#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SamMul.Scenes;

namespace Demo.Editor
{
    /// <summary>
    /// 데모 씬(Loading/Lobby/Stage)과 필수 공용 프리팹을 코드로 생성한다.
    /// 각 씬은 게임 코드가 기대하는 루트 구조를 갖는다:
    ///   @MainCameraController / @MainCamera(Camera) , @Scene(BaseScene 파생) , @UIRoot(Canvas + BaseSceneUIRoot 파생)
    /// UIRoot 이하의 [SerializeField] 참조는 필드 타입에 맞는 플레이스홀더 오브젝트를 만들어 자동으로 연결한다.
    /// 메뉴: Demo > Build Scenes  /  배치: -executeMethod Demo.Editor.DemoSceneBuilder.BuildAll
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string SCENES_DIR = "Assets/Scenes";
        private const string RESOURCES_DIR = "Assets/Resources";
        private const int MAX_DEPTH = 6;

        [MenuItem("Demo/Build Scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(SCENES_DIR);
            Directory.CreateDirectory(RESOURCES_DIR + "/Commons");
            Directory.CreateDirectory(RESOURCES_DIR + "/StaticData");
            AssetDatabase.Refresh();

            CreateEventSystemPrefab();

            var scenePaths = new List<string>
            {
                BuildScene(SceneType.Lobby, typeof(LobbyScene), typeof(LobbySceneUIRoot)),
                BuildScene(SceneType.Stage, typeof(StageScene), typeof(StageSceneUIRoot)),
                BuildScene(SceneType.Loading, typeof(LoadingScene), typeof(LoadingSceneUIRoot)),
            };

            var buildScenes = new List<EditorBuildSettingsScene>();
            foreach (var path in scenePaths)
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"[DemoSceneBuilder] built {scenePaths.Count} scenes: {string.Join(", ", scenePaths)}");
        }

        private static void CreateEventSystemPrefab()
        {
            string path = RESOURCES_DIR + "/Commons/@EventSystem.prefab";
            if (File.Exists(path))
            {
                return;
            }
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static string BuildScene(SceneType sceneType, Type sceneComponentType, Type uiRootType)
        {
            string path = $"{SCENES_DIR}/{sceneType}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라
            var cameraController = new GameObject("@MainCameraController");
            var cameraObject = new GameObject("@MainCamera");
            cameraObject.transform.SetParent(cameraController.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";

            // UI 루트
            var uiRoot = new GameObject("@UIRoot", typeof(RectTransform));
            var canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            uiRoot.AddComponent<GraphicRaycaster>();
            var uiRootComponent = (Component)uiRoot.AddComponent(uiRootType);
            AutoWire(uiRootComponent, 0, new HashSet<Type>());

            // 씬 컴포넌트 (Awake에서 @UIRoot를 찾으므로 마지막에 만든다)
            var sceneObject = new GameObject("@Scene");
            var sceneComponent = (Component)sceneObject.AddComponent(sceneComponentType);
            AutoWire(sceneComponent, 0, new HashSet<Type>());

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        /// <summary>
        /// 컴포넌트의 직렬화된 오브젝트 참조 필드가 비어 있으면 필드 타입에 맞는 자식 오브젝트를 만들어 연결한다.
        /// 커스텀 MonoBehaviour 필드는 재귀적으로 같은 처리를 한다.
        /// </summary>
        private static void AutoWire(Component target, int depth, HashSet<Type> ancestry)
        {
            if (depth > MAX_DEPTH)
            {
                return;
            }
            var so = new SerializedObject(target);
            var prop = so.GetIterator();
            bool enterChildren = true;
            var created = new List<Component>();
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script")
                {
                    continue;
                }
                var field = FindField(target.GetType(), prop.name);
                if (field == null)
                {
                    continue;
                }

                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null)
                    {
                        var value = CreateReference(target, field.FieldType, prop.name, depth, ancestry, created);
                        if (value != null)
                        {
                            prop.objectReferenceValue = value;
                        }
                    }
                }
                else if (prop.isArray && prop.propertyType != SerializedPropertyType.String && prop.arraySize == 0)
                {
                    var elementType = field.FieldType.IsArray ? field.FieldType.GetElementType() : (field.FieldType.IsGenericType ? field.FieldType.GetGenericArguments()[0] : null);
                    if (elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                    {
                        prop.arraySize = 1;
                        var element = prop.GetArrayElementAtIndex(0);
                        var value = CreateReference(target, elementType, prop.name + "_0", depth, ancestry, created);
                        if (value != null)
                        {
                            element.objectReferenceValue = value;
                        }
                    }
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var child in created)
            {
                var childAncestry = new HashSet<Type>(ancestry) { target.GetType() };
                AutoWire(child, depth + 1, childAncestry);
            }
        }

        private static UnityEngine.Object? CreateReference(Component owner, Type fieldType, string name, int depth, HashSet<Type> ancestry, List<Component> created)
        {
            // 캔버스 계열은 상위에서 찾아 공유한다.
            if (fieldType == typeof(Canvas) || fieldType == typeof(CanvasScaler) || fieldType == typeof(GraphicRaycaster) || fieldType == typeof(Camera))
            {
                var found = owner.GetComponentInParent(fieldType);
                if (found != null)
                {
                    return found;
                }
            }

            if (fieldType == typeof(GameObject))
            {
                return CreateChild(owner.transform, name).gameObject;
            }
            if (fieldType == typeof(Transform) || fieldType == typeof(RectTransform))
            {
                return CreateChild(owner.transform, name);
            }
            if (!typeof(Component).IsAssignableFrom(fieldType) || fieldType.IsAbstract || fieldType.IsInterface)
            {
                return null; // Sprite, AudioClip, ScriptableObject 등은 비워둔다.
            }
            if (ancestry.Contains(fieldType) || fieldType == owner.GetType())
            {
                return null; // 순환 참조 방지
            }

            var child = CreateChild(owner.transform, name);
            Component component;
            if (fieldType == typeof(TextMeshProUGUI) || fieldType == typeof(TMP_Text))
            {
                var text = child.gameObject.AddComponent<TextMeshProUGUI>();
                text.text = name.TrimStart('_');
                text.fontSize = 28f;
                text.alignment = TextAlignmentOptions.Center;
                component = text;
            }
            else if (fieldType == typeof(Image))
            {
                var image = child.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.25f);
                component = image;
            }
            else if (fieldType == typeof(Button) || typeof(Button).IsAssignableFrom(fieldType))
            {
                var image = child.gameObject.AddComponent<Image>();
                image.color = new Color(0.3f, 0.5f, 0.9f, 0.9f);
                component = child.gameObject.AddComponent(fieldType);
                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                label.transform.SetParent(child, false);
                label.text = name.TrimStart('_');
                label.fontSize = 24f;
                label.alignment = TextAlignmentOptions.Center;
                Stretch(label.rectTransform);
            }
            else if (fieldType == typeof(Slider))
            {
                component = child.gameObject.AddComponent<Slider>();
            }
            else
            {
                if (typeof(Graphic).IsAssignableFrom(fieldType))
                {
                    child.gameObject.AddComponent<CanvasRenderer>();
                }
                component = child.gameObject.AddComponent(fieldType);
            }

            if (component is MonoBehaviour && fieldType.Assembly == typeof(BaseScene).Assembly)
            {
                created.Add(component);
            }
            return component;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            bool isUI = parent is RectTransform;
            var go = isUI ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            go.transform.SetParent(parent, false);
            if (isUI)
            {
                var rect = (RectTransform)go.transform;
                rect.sizeDelta = new Vector2(300f, 80f);
            }
            return go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static FieldInfo? FindField(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null)
                {
                    return f;
                }
            }
            return null;
        }
    }
}

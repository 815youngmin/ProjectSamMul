#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.Animations.Placeholder;

namespace SamMul.ResourcePools
{
    /// <summary>
    /// 리소스가 없을 때 대신 쓸 플레이스홀더를 런타임에 만든다.
    /// 프리팹 대신 요청된 컴포넌트를 붙인 빈 오브젝트를 만들고, 그 컴포넌트의 직렬화 참조 필드를
    /// 필드 타입에 맞는 자식 오브젝트로 채워 NullReference 없이 동작하게 한다.
    /// 실제 프리팹/스프라이트가 Resources 에 추가되면 자연히 이 경로는 쓰이지 않는다.
    /// </summary>
    public static class PlaceholderFactory
    {
        private const int MAX_DEPTH = 6;
        private static Sprite? s_whiteSprite;
        private static readonly HashSet<string> s_reported = new HashSet<string>();

        public static void ReportOnce(string what, string path)
        {
            if (s_reported.Add(path))
            {
                Debug.LogWarning($"[Placeholder] {what} 없음 → 플레이스홀더 사용. Path[{path}]");
            }
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (s_whiteSprite == null)
                {
                    var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var pixels = new Color32[16];
                    for (int i = 0; i < pixels.Length; ++i)
                    {
                        pixels[i] = new Color32(255, 255, 255, 255);
                    }
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    s_whiteSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                    s_whiteSprite.name = "PlaceholderWhite";
                }
                return s_whiteSprite;
            }
        }

        /// <summary>요청 타입의 컴포넌트가 붙은 플레이스홀더 오브젝트를 만든다.</summary>
        public static GameObject CreateObject(string path, Type? rootComponentType)
        {
            string name = "[Placeholder] " + path;
            bool isUI = (rootComponentType != null && IsUIType(rootComponentType)) || IsUIPath(path);
            var go = isUI ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);

            if (rootComponentType != null && !rootComponentType.IsAbstract)
            {
                var root = go.AddComponent(rootComponentType);
                if (root != null)
                {
                    if (isUI)
                    {
                        var rect = (RectTransform)go.transform;
                        rect.sizeDelta = new Vector2(600f, 400f);
                        if (typeof(Graphic).IsAssignableFrom(rootComponentType) == false && go.GetComponent<Graphic>() == null)
                        {
                            var background = go.AddComponent<Image>();
                            background.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
                        }
                    }
                    else if (go.GetComponent<Renderer>() == null && rootComponentType != typeof(SkeletonAnimation))
                    {
                        var renderer = go.AddComponent<SpriteRenderer>();
                        renderer.sprite = WhiteSprite;
                    }
                    Wire(root, 0, new HashSet<Type>());
                }
            }
            else if (rootComponentType == null)
            {
                if (isUI)
                {
                    var image = go.AddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0.15f);
                    ((RectTransform)go.transform).sizeDelta = new Vector2(200f, 60f);
                }
                else
                {
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = WhiteSprite;
                }
            }
            return go;
        }

        private static bool IsUIPath(string path)
        {
            return path.Contains("/UIs/") || path.StartsWith("Commons/") || path.StartsWith("Lobbys/") || path.StartsWith("Loadings/");
        }

        private static bool IsUIType(Type type)
        {
            if (typeof(Graphic).IsAssignableFrom(type) || typeof(Selectable).IsAssignableFrom(type) || typeof(LayoutGroup).IsAssignableFrom(type))
            {
                return true;
            }
            return type.FullName != null && type.FullName.Contains(".UIs.");
        }

        /// <summary>컴포넌트의 비어 있는 직렬화 참조 필드를 채운다.</summary>
        public static void Wire(Component target, int depth, HashSet<Type> ancestry)
        {
            if (depth > MAX_DEPTH)
            {
                return;
            }
            var created = new List<Component>();
            for (var type = target.GetType(); type != null && type != typeof(MonoBehaviour) && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                    {
                        continue;
                    }
                    if (field.GetCustomAttribute<NonSerializedAttribute>() != null || field.IsInitOnly)
                    {
                        continue;
                    }

                    var fieldType = field.FieldType;
                    if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                    {
                        if (field.GetValue(target) is UnityEngine.Object existing && existing != null)
                        {
                            continue;
                        }
                        var value = CreateReference(target, fieldType, field.Name, ancestry, created);
                        if (value != null)
                        {
                            field.SetValue(target, value);
                        }
                    }
                    else if (fieldType.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(fieldType.GetElementType()))
                    {
                        var current = field.GetValue(target) as Array;
                        if (current != null && current.Length > 0)
                        {
                            continue;
                        }
                        var elementType = fieldType.GetElementType()!;
                        var value = CreateReference(target, elementType, field.Name + "_0", ancestry, created);
                        var array = Array.CreateInstance(elementType, value != null ? 1 : 0);
                        if (value != null)
                        {
                            array.SetValue(value, 0);
                        }
                        field.SetValue(target, array);
                    }
                    else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>) && typeof(UnityEngine.Object).IsAssignableFrom(fieldType.GetGenericArguments()[0]))
                    {
                        if (field.GetValue(target) is System.Collections.IList existing && existing.Count > 0)
                        {
                            continue;
                        }
                        var elementType = fieldType.GetGenericArguments()[0];
                        var list = (System.Collections.IList)Activator.CreateInstance(fieldType)!;
                        var value = CreateReference(target, elementType, field.Name + "_0", ancestry, created);
                        if (value != null)
                        {
                            list.Add(value);
                        }
                        field.SetValue(target, list);
                    }
                }
            }

            foreach (var child in created)
            {
                var childAncestry = new HashSet<Type>(ancestry) { target.GetType() };
                Wire(child, depth + 1, childAncestry);
            }
        }

        private static UnityEngine.Object? CreateReference(Component owner, Type fieldType, string name, HashSet<Type> ancestry, List<Component> created)
        {
            if (fieldType == typeof(Sprite))
            {
                return WhiteSprite;
            }
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
            if (fieldType == typeof(SpriteRenderer))
            {
                var renderer = CreateChild(owner.transform, name).gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = WhiteSprite;
                return renderer;
            }
            if (!typeof(Component).IsAssignableFrom(fieldType) || fieldType.IsAbstract || fieldType.IsInterface)
            {
                return null;
            }
            if (ancestry.Contains(fieldType) || fieldType == owner.GetType())
            {
                return null;
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
            else if (fieldType == typeof(TextMeshPro))
            {
                var text = child.gameObject.AddComponent<TextMeshPro>();
                text.text = name.TrimStart('_');
                text.fontSize = 4f;
                text.alignment = TextAlignmentOptions.Center;
                component = text;
            }
            else if (fieldType == typeof(Image))
            {
                var image = child.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.25f);
                component = image;
            }
            else if (typeof(Button).IsAssignableFrom(fieldType))
            {
                var image = child.gameObject.AddComponent<Image>();
                image.color = new Color(0.3f, 0.5f, 0.9f, 0.9f);
                component = child.gameObject.AddComponent(fieldType);
                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                label.transform.SetParent(child, false);
                label.text = name.TrimStart('_');
                label.fontSize = 24f;
                label.alignment = TextAlignmentOptions.Center;
                var rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                component = child.gameObject.AddComponent(fieldType);
            }

            if (component is MonoBehaviour && fieldType.Assembly == typeof(PlaceholderFactory).Assembly)
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
                ((RectTransform)go.transform).sizeDelta = new Vector2(300f, 80f);
            }
            return go.transform;
        }
    }
}

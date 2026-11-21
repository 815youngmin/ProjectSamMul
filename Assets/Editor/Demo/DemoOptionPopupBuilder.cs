#nullable enable
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SamMul.Localizations;
using SamMul.UIs.Lobbies;
using SamMul.UIs.Lobbies.Popups;

namespace Demo.Editor
{
    /// <summary>
    /// 로비 옵션 팝업 프리팹을 만들고 OptionPopup 의 필드를 연결한다.
    /// 패널·버튼 스프라이트와 폰트는 CommonPopup 프리팹에서 가져와 같은 모양으로 맞춘다.
    /// 메뉴: Demo > Build Option Popup Prefab  /  배치: -executeMethod Demo.Editor.DemoOptionPopupBuilder.Build
    /// </summary>
    public static class DemoOptionPopupBuilder
    {
        private const string PREFAB_PATH = "Assets/Resources/Lobbys/UIs/OptionPopup/OptionPopup.prefab";
        private const string STYLE_SOURCE_PATH = "Assets/Resources/Stage/UIs/CommonPopup.prefab";

        private static readonly Vector2 PANEL_SIZE = new Vector2(1082f, 1000f);
        private const float TITLE_HEIGHT = 133f;
        private const float LABEL_X = -330f;
        private const float CONTROL_X = 170f;
        private static readonly Color SLIDER_FILL_COLOR = new Color(0.992156f, 0.768627f, 0.062745f);

        private static Image? s_panelStyle;
        private static Image? s_titleStyle;
        private static Image? s_buttonStyle;
        private static TextMeshProUGUI? s_labelStyle;
        private static TextMeshProUGUI? s_buttonTextStyle;

        [MenuItem("Demo/Build Option Popup Prefab")]
        public static void Build()
        {
            if (File.Exists(PREFAB_PATH) &&
                !EditorUtility.DisplayDialog("Option Popup", $"{PREFAB_PATH}\n이미 있습니다. 덮어쓸까요? (직접 수정한 내용은 사라집니다)", "덮어쓰기", "취소"))
            {
                return;
            }

            var style = AssetDatabase.LoadAssetAtPath<GameObject>(STYLE_SOURCE_PATH);
            s_panelStyle = FindStyle<Image>(style, "Background");
            s_titleStyle = FindStyle<Image>(style, "Title");
            s_buttonStyle = FindStyle<Image>(style, "LeftButton");
            s_labelStyle = FindStyle<TextMeshProUGUI>(style, "MessageText");
            s_buttonTextStyle = s_buttonStyle != null ? s_buttonStyle.GetComponentInChildren<TextMeshProUGUI>(true) : null;

            var root = new GameObject("OptionPopup", typeof(RectTransform));
            var popup = root.AddComponent<OptionPopup>();

            var panel = CreateImage("Background", root.transform, Vector2.zero, PANEL_SIZE, s_panelStyle, new Color(0.15f, 0.17f, 0.22f, 1f)).rectTransform;

            float titleY = (PANEL_SIZE.y - TITLE_HEIGHT) * 0.5f;
            CreateImage("Title", panel, new Vector2(0f, titleY), new Vector2(PANEL_SIZE.x, TITLE_HEIGHT), s_titleStyle, new Color(0f, 0f, 0f, 0.5f)).raycastTarget = false;
            var titleText = CreateText("TitleText", panel, new Vector2(0f, titleY), new Vector2(PANEL_SIZE.x - 260f, TITLE_HEIGHT), "옵션", 60f, TextAlignmentOptions.Center, s_labelStyle);
            SetTextKey(titleText, "UI_OPTION_TITLE");

            var closeButton = CreateButton("CloseButton", panel, new Vector2(PANEL_SIZE.x * 0.5f - 75f, titleY), new Vector2(100f, 100f), "X", out _);

            // 언어
            CreateLabel(panel, 230f, "언어", "UI_OPTION_LANGUAGE");
            var languageRect = CreateRect("LanguageGroup", panel, new Vector2(CONTROL_X, 230f), new Vector2(600f, 120f));
            var languageGroup = languageRect.gameObject.AddComponent<LobbyRadioGroup>();
            // 자식 순서가 OptionPopup.LANGUAGES 의 순서(한국어, 영어)와 같아야 한다.
            CreateButton("KoreanButton", languageRect, new Vector2(-150f, 0f), new Vector2(270f, 110f), "한국어", out _);
            CreateButton("EnglishButton", languageRect, new Vector2(150f, 0f), new Vector2(270f, 110f), "English", out _);

            // 사운드
            CreateLabel(panel, 60f, "사운드", "UI_OPTION_SOUND");
            var muteButton = CreateButton("MuteButton", panel, new Vector2(CONTROL_X, 60f), new Vector2(420f, 110f), "사운드 켜짐", out var muteText);

            CreateLabel(panel, -120f, "배경음", "UI_OPTION_BGM_VOLUME");
            var bgmSlider = CreateSlider("BgmSlider", panel, new Vector2(CONTROL_X, -120f));

            CreateLabel(panel, -290f, "효과음", "UI_OPTION_SFX_VOLUME");
            var effectSlider = CreateSlider("EffectSlider", panel, new Vector2(CONTROL_X, -290f));

            var serializedPopup = new SerializedObject(popup);
            SetReference(serializedPopup, "_closeButton", closeButton);
            SetReference(serializedPopup, "_languageGroup", languageGroup);
            SetReference(serializedPopup, "_muteButton", muteButton);
            SetReference(serializedPopup, "_muteText", muteText);
            SetReference(serializedPopup, "_bgmSlider", bgmSlider);
            SetReference(serializedPopup, "_effectSlider", effectSlider);
            serializedPopup.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PREFAB_PATH)!);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DemoOptionPopupBuilder] created {PREFAB_PATH}");
        }

        private static T? FindStyle<T>(GameObject? source, string objectName) where T : Component
        {
            if (source == null)
            {
                return null;
            }
            foreach (var child in source.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child.GetComponent<T>();
                }
            }
            return null;
        }

        private static void SetReference(SerializedObject serializedObject, string fieldName, Object value)
        {
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.InvalidOperationException($"OptionPopup 에 {fieldName} 필드가 없습니다.");
            }
            property.objectReferenceValue = value;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <param name="style">스프라이트·색을 가져올 이미지. 없으면 fallbackColor 의 단색 사각형이 된다.</param>
        private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Image? style, Color fallbackColor)
        {
            var image = CreateRect(name, parent, position, size).gameObject.AddComponent<Image>();
            if (style != null)
            {
                image.sprite = style.sprite;
                image.type = style.type;
                image.color = style.color;
            }
            else
            {
                image.color = fallbackColor;
            }
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize, TextAlignmentOptions alignment, TextMeshProUGUI? style)
        {
            var label = CreateRect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            if (style != null)
            {
                label.font = style.font;
                label.fontSharedMaterial = style.fontSharedMaterial;
                label.color = style.color;
            }
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }

        private static void CreateLabel(Transform parent, float y, string text, string textKey)
        {
            var label = CreateText(text + "Label", parent, new Vector2(LABEL_X, y), new Vector2(360f, 100f), text, 50f, TextAlignmentOptions.Left, s_labelStyle);
            SetTextKey(label, textKey);
        }

        private static void SetTextKey(TextMeshProUGUI text, string textKey)
        {
            var serializedText = new SerializedObject(text.gameObject.AddComponent<LocalizedText>());
            serializedText.FindProperty("textKey").stringValue = textKey;
            serializedText.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ZButton CreateButton(string name, Transform parent, Vector2 position, Vector2 size, string text, out TextMeshProUGUI buttonText)
        {
            var image = CreateImage(name, parent, position, size, s_buttonStyle, new Color(0.3f, 0.45f, 0.75f, 1f));
            buttonText = CreateText("Text", image.transform, Vector2.zero, size, text, 46f, TextAlignmentOptions.Center, s_buttonTextStyle != null ? s_buttonTextStyle : s_labelStyle);
            return image.gameObject.AddComponent<ZButton>();
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 position)
        {
            var resources = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            };
            var go = DefaultControls.CreateSlider(resources);
            go.name = name;

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(560f, 40f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect.GetComponent<Image>().color = SLIDER_FILL_COLOR;
            // 손잡이를 터치하기 쉽게 키운다. 세로는 슬라이더 높이보다 30 크게.
            slider.handleRect.sizeDelta = new Vector2(70f, 30f);
            return slider;
        }
    }
}

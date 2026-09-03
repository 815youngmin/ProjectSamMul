#nullable enable
using System;
using Shared.Localizers;
using TMPro;
using UnityEngine;

namespace Z.Localizations
{
    /// <summary>
    /// 텍스트 키를 현재 언어의 문자열로 바꿔 TextMeshProUGUI 에 넣어주는 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        public static readonly string LANGUAGE_KEY = "KLanguage";

        // 프리팹 직렬화 호환을 위해 필드 이름을 유지한다.
        [SerializeField] private string textKey = string.Empty;

        private TextMeshProUGUI? _text;

        public static LanguageType GetCurrentLanguageSetting()
        {
            var defaultLanguage = Application.systemLanguage == SystemLanguage.Korean ? LanguageType.Korean : LanguageType.English;
            string saved = PlayerPrefs.GetString(LANGUAGE_KEY, defaultLanguage.ToString());
            return Enum.TryParse<LanguageType>(saved, out var language) ? language : LanguageType.English;
        }

        private void Start()
        {
            this.UpdateText();
        }

        public void UpdateText()
        {
            this.GetText().text = Localizer.Instance.GetText(textKey);
        }

        public TextMeshProUGUI GetText()
        {
            if (_text == null)
            {
                _text = this.GetComponent<TextMeshProUGUI>();
            }
            return _text;
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.StaticDatas;

namespace Shared.Localizers
{
    public class Language
    {
        public LanguageType Type { get; }
        public Dictionary<string, string> Texts { get; }

        public Language(LanguageType type, Dictionary<string, string> texts)
        {
            Type = type;
            Texts = texts;
        }
    }

    /// <summary>
    /// Text lookup by key. Table "Localization": a JSON object keyed by <see cref="LanguageType"/> name,
    /// each holding an object of text key to text. A missing key resolves to the key itself.
    /// </summary>
    public class Localizer
    {
        private static Localizer? s_instance;

        public static Localizer Instance => s_instance ?? throw new InvalidOperationException("Localizer is not initialized.");
        public static bool IsInitialized => s_instance != null;

        private readonly Dictionary<LanguageType, Language> _languages = new Dictionary<LanguageType, Language>();

        public Language CurrentLanguage { get; private set; }

        public static void Initialize(StaticDataTableReader reader, LanguageType initialLanguageType)
        {
            if (s_instance != null)
            {
                return;
            }

            s_instance = new Localizer(reader, initialLanguageType);
        }

        public static void ForceReInitialize(StaticDataTableReader reader, LanguageType initialLanguageType)
        {
            s_instance = new Localizer(reader, initialLanguageType);
        }

        private Localizer(StaticDataTableReader reader, LanguageType initialLanguageType)
        {
            var json = reader("Localization");
            var texts = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonConvert.DeserializeObject<Dictionary<LanguageType, Dictionary<string, string>>>(json);
            foreach (LanguageType type in Enum.GetValues(typeof(LanguageType)))
            {
                _languages[type] = new Language(type, texts != null && texts.TryGetValue(type, out var table) ? table : new Dictionary<string, string>());
            }

            CurrentLanguage = _languages[initialLanguageType];
        }

        public void SetLanguage(LanguageType language)
        {
            CurrentLanguage = _languages[language];
        }

        public string GetText(string textKey)
        {
            return CurrentLanguage.Texts.TryGetValue(textKey, out var text) ? text : textKey;
        }
    }
}

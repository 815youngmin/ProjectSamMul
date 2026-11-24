#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SamMul.Localizations;
using Shared;
using Shared.Localizers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SamMul.Tests.EditMode
{
    /// <summary>
    /// 로비·스테이지에서 실제로 쓰는 텍스트 키를 코드, 프리팹·씬, 정적 데이터에서 모아
    /// 한국어와 영어 양쪽에 빠짐없이 들어 있는지 검증한다.
    /// 키가 없으면 <see cref="Localizer.GetText"/> 가 키 문자열을 그대로 돌려주므로 화면에 키가 노출된다.
    /// </summary>
    public class LocalizationTests
    {
        private static readonly LanguageType[] LANGUAGES = { LanguageType.Korean, LanguageType.English };

        private static readonly Regex KEY_LITERAL = new Regex("\"([A-Z][A-Z0-9_]+)\"");
        private static readonly Regex DATA_KEY_FIELD = new Regex("\"\\w*Key\"\\s*:\\s*\"([^\"]*)\"");
        private static readonly Regex PLACEHOLDER = new Regex(@"\{(\d+)");

        private readonly Dictionary<LanguageType, Dictionary<string, string>> _texts = new Dictionary<LanguageType, Dictionary<string, string>>();

        [OneTimeSetUp]
        public void LoadTexts()
        {
            SharedInitializer.ForceReInitialize(LanguageType.Korean);
            foreach (var language in LANGUAGES)
            {
                Localizer.Instance.SetLanguage(language);
                _texts[language] = new Dictionary<string, string>(Localizer.Instance.CurrentLanguage.Texts);
            }
            Localizer.Instance.SetLanguage(LanguageType.Korean);
        }

        private static IEnumerable<string> FilesOf(string folder, string pattern)
        {
            return Directory.GetFiles(Path.Combine(Application.dataPath, folder), pattern, SearchOption.AllDirectories);
        }

        /// <summary>키 → 처음 발견한 위치. 실패 메시지에서 어디를 고쳐야 하는지 알려 주는 용도다.</summary>
        private static void Collect(Dictionary<string, string> keys, IEnumerable<string> files, Regex regex, System.Func<string, string>? preprocess = null)
        {
            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                foreach (Match match in regex.Matches(preprocess != null ? preprocess(content) : content))
                {
                    string key = match.Groups[1].Value;
                    if (!keys.ContainsKey(key))
                    {
                        keys.Add(key, Path.GetFileName(file));
                    }
                }
            }
        }

        // ponytail: GetText( 와 키 리터럴이 같은 줄에 있다고 가정한다. 여러 줄 호출이나 변수로 넘긴 키는 못 잡는다. 필요해지면 Roslyn 으로 교체.
        private static string OnlyGetTextLines(string source)
        {
            return string.Join("\n", source.Split('\n').Select(line => line.Split(new[] { "//" }, 2, System.StringSplitOptions.None)[0]).Where(line => line.Contains("GetText(")));
        }

        private void AssertAllLocalized(Dictionary<string, string> keys)
        {
            Assert.IsNotEmpty(keys, "수집된 키가 없습니다. 수집 규칙이 깨졌습니다.");

            var problems = new List<string>();
            foreach (var pair in keys)
            {
                foreach (var language in LANGUAGES)
                {
                    if (!_texts[language].TryGetValue(pair.Key, out var text) || string.IsNullOrWhiteSpace(text))
                    {
                        problems.Add($"{language}: '{pair.Key}' ({pair.Value})");
                    }
                }
            }
            CollectionAssert.IsEmpty(problems, "텍스트가 없는 키");
        }

        [Test]
        public void KeysUsedInCode_ExistInKoreanAndEnglish()
        {
            var keys = new Dictionary<string, string>();
            Collect(keys, FilesOf("Scripts", "*.cs"), KEY_LITERAL, OnlyGetTextLines);
            this.AssertAllLocalized(keys);
        }

        private static string HierarchyPathOf(Transform transform)
        {
            return transform.parent == null ? transform.name : HierarchyPathOf(transform.parent) + "/" + transform.name;
        }

        /// <summary>오브젝트들(비활성 포함)에서 LocalizedText 를 찾아 직렬화된 textKey 를 모은다. 키가 비어 있으면 바로 문제로 기록한다.</summary>
        private static void CollectLocalizedTexts(Dictionary<string, string> keys, List<string> emptyKeys, string assetPath, IEnumerable<GameObject> roots)
        {
            foreach (var localizedText in roots.SelectMany(root => root.GetComponentsInChildren<LocalizedText>(true)))
            {
                string where = $"{assetPath}: {HierarchyPathOf(localizedText.transform)}";
                string key = new SerializedObject(localizedText).FindProperty("textKey").stringValue;
                if (string.IsNullOrWhiteSpace(key))
                {
                    emptyKeys.Add(where);
                }
                else if (!keys.ContainsKey(key))
                {
                    keys.Add(key, where);
                }
            }
        }

        [Test]
        public void LocalizedTextsInScenes_HaveKeysInKoreanAndEnglish()
        {
            var keys = new Dictionary<string, string>();
            var emptyKeys = new List<string>();
            foreach (string scenePath in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                // 이미 열려 있는 씬은 그대로 쓰고, 테스트가 연 씬만 닫는다.
                bool wasLoaded = SceneManager.GetSceneByPath(scenePath).isLoaded;
                var scene = wasLoaded ? SceneManager.GetSceneByPath(scenePath) : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                CollectLocalizedTexts(keys, emptyKeys, scenePath, scene.GetRootGameObjects());
                if (!wasLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            CollectionAssert.IsEmpty(emptyKeys, "textKey 가 비어 있는 LocalizedText");
            this.AssertAllLocalized(keys);
        }

        [Test]
        public void LocalizedTextsInPrefabs_HaveKeysInKoreanAndEnglish()
        {
            var keys = new Dictionary<string, string>();
            var emptyKeys = new List<string>();
            foreach (string prefabPath in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                CollectLocalizedTexts(keys, emptyKeys, prefabPath, new[] { AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) });
            }

            CollectionAssert.IsEmpty(emptyKeys, "textKey 가 비어 있는 LocalizedText");
            this.AssertAllLocalized(keys);
        }

        [Test]
        public void KeysUsedInStaticData_ExistInKoreanAndEnglish()
        {
            var keys = new Dictionary<string, string>();
            Collect(keys, FilesOf("Resources/StaticData", "*.json").Where(file => Path.GetFileName(file) != "Localization.json"), DATA_KEY_FIELD);
            this.AssertAllLocalized(keys);
        }

        [Test]
        public void GetText_WithMissingKey_ReturnsKeyItself_WithoutThrowing()
        {
            const string MISSING_KEY = "UI_TEST_KEY_THAT_DOES_NOT_EXIST";

            try
            {
                // 한국어·영어, 그리고 테이블 자체가 없는 언어(일본어)까지 확인한다.
                foreach (var language in LANGUAGES.Append(LanguageType.Japanese))
                {
                    Localizer.Instance.SetLanguage(language);
                    Assert.IsFalse(Localizer.Instance.CurrentLanguage.Texts.ContainsKey(MISSING_KEY), $"{language}: 테스트용 키가 실제로 존재합니다.");
                    Assert.AreEqual(MISSING_KEY, Localizer.Instance.GetText(MISSING_KEY), $"{language}: 없는 키는 키 문자열을 그대로 돌려줘야 합니다.");
                    Assert.AreEqual(string.Empty, Localizer.Instance.GetText(string.Empty), $"{language}: 빈 키");
                }
            }
            finally
            {
                Localizer.Instance.SetLanguage(LanguageType.Korean);
            }
        }

        [Test]
        public void FormatPlaceholders_AreSameInKoreanAndEnglish()
        {
            var problems = new List<string>();
            foreach (var pair in _texts[LanguageType.Korean])
            {
                if (!_texts[LanguageType.English].TryGetValue(pair.Key, out var english))
                {
                    continue;
                }

                var koreanIndexes = PLACEHOLDER.Matches(pair.Value).Cast<Match>().Select(match => match.Groups[1].Value).Distinct().OrderBy(index => index);
                var englishIndexes = PLACEHOLDER.Matches(english).Cast<Match>().Select(match => match.Groups[1].Value).Distinct().OrderBy(index => index);
                if (!koreanIndexes.SequenceEqual(englishIndexes))
                {
                    problems.Add($"'{pair.Key}': \"{pair.Value}\" / \"{english}\"");
                }
            }
            CollectionAssert.IsEmpty(problems, "{0} 같은 자리표시자가 언어마다 다른 키");
        }
    }
}

#nullable enable
using Shared;
using Shared.Localizers;
using Shared.StaticDatas;
using UnityEditor;
using UnityEngine;

namespace Demo.Editor
{
    /// <summary>
    /// Resources/StaticData 의 JSON 테이블을 실제 로더로 읽어 검증한다.
    /// 메뉴: Demo > Validate Static Data  /  배치: -executeMethod Demo.Editor.DemoStaticDataValidator.Validate
    /// </summary>
    public static class DemoStaticDataValidator
    {
        [MenuItem("Demo/Validate Static Data")]
        public static void Validate()
        {
            try
            {
                SharedInitializer.ForceReInitialize(LanguageType.Korean);
                var repo = StaticDataRepository.Instance;
                int chapters = 0;
                while (repo.Chapters.FindChapter(chapters + 1) != null)
                {
                    ++chapters;
                }
                Debug.Log($"[StaticData] OK. chapters={chapters} localizedTexts={Localizer.Instance.CurrentLanguage.Texts.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StaticData] FAILED: {e}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(2);
                }
            }
        }
    }
}

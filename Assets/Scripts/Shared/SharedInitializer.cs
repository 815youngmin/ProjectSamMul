#nullable enable
using Shared.Localizers;
using Shared.StaticDatas;

namespace Shared
{
    /// <summary>
    /// Boots the localizer and the static-data tables. Tables are JSON files under Resources/StaticData/&lt;table&gt;.json;
    /// a missing file is treated as an empty table so data can be authored incrementally.
    /// </summary>
    public static class SharedInitializer
    {
        public const string RESOURCE_ROOT = "StaticData";

        public static bool IsInitialized { get; private set; }

        public static bool Initialize(LanguageType initialLanguageType)
        {
            if (IsInitialized)
            {
                return false;
            }

            Localizer.Initialize(ReadTable, initialLanguageType);
            StaticDataRepository.Initialize(ReadTable);
            IsInitialized = true;
            return true;
        }

        public static void ForceReInitialize(LanguageType initialLanguageType)
        {
            Localizer.ForceReInitialize(ReadTable, initialLanguageType);
            StaticDataRepository.ForceReInitialize(ReadTable);
            IsInitialized = true;
        }

        /// <summary>Reads Resources/StaticData/&lt;tableName&gt;.json; null when the asset does not exist.</summary>
        public static string? ReadTable(string tableName)
        {
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>($"{RESOURCE_ROOT}/{tableName}");
            return asset == null ? null : asset.text;
        }
    }
}

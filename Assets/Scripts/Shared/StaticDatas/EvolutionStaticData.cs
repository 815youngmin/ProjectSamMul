#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.GameDataTypes;
using Shared.Localizers;

namespace Shared.StaticDatas
{
    /// <summary>Basic evolution research step. Table "BasicEvolutions" (ids start at 1 and are researched in order).</summary>
    public class BasicEvolutionStaticData
    {
        public int basicEvolutionID { get; set; }
        public int learnLevel { get; set; }
        public EvolutionType basicEvolutionType { get; set; }
        public float param1 { get; set; }
        public int requiredGold { get; set; }
        public string NameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        [JsonIgnore] public string BasicEvolutionName => Localizer.Instance.GetText(NameKey);
        [JsonIgnore] public string BasicEvolutionDescription => Localizer.Instance.GetText(DescriptionKey);
        public string EnableIconResourcePath { get; set; } = "";
        public string DisableIconResourcePath { get; set; } = "";
    }

    /// <summary>Special evolution research step. Table "SpecialEvolutions" (ids start at 1 and are researched in order).</summary>
    public class SpecialEvolutionStaticData
    {
        public int specialEvolutionID { get; set; }
        public int learnLevel { get; set; }
        public EvolutionType specialEvolutionType { get; set; }
        public float param1 { get; set; }
        public float param2 { get; set; }
        public int requiredGold { get; set; }
        public int requiredSpecialDNA { get; set; }
        public string NameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        [JsonIgnore] public string SpecialEvolutionName => Localizer.Instance.GetText(NameKey);
        [JsonIgnore] public string SpecialEvolutionDescription => Localizer.Instance.GetText(DescriptionKey);
        public string EnableIconResourcePath { get; set; } = "";
        public string DisableIconResourcePath { get; set; } = "";
    }

    public class BasicEvolutionStaticDataRepository
    {
        private readonly Dictionary<int, BasicEvolutionStaticData> _evolutions = new Dictionary<int, BasicEvolutionStaticData>();

        public IReadOnlyDictionary<int, BasicEvolutionStaticData> BasicEvolutionStaticDatas => _evolutions;

        public BasicEvolutionStaticDataRepository(IReadOnlyList<BasicEvolutionStaticData> rows)
        {
            foreach (var row in rows)
            {
                _evolutions[row.basicEvolutionID] = row;
            }
        }

        public BasicEvolutionStaticData? FindBasicEvolutionStaticData(int basicEvolutionID)
            => _evolutions.TryGetValue(basicEvolutionID, out var evolution) ? evolution : null;
    }

    public class SpecialEvolutionStaticDataRepository
    {
        private readonly Dictionary<int, SpecialEvolutionStaticData> _evolutions = new Dictionary<int, SpecialEvolutionStaticData>();

        public IReadOnlyDictionary<int, SpecialEvolutionStaticData> SpecialEvolutionStaticDatas => _evolutions;

        public SpecialEvolutionStaticDataRepository(IReadOnlyList<SpecialEvolutionStaticData> rows)
        {
            foreach (var row in rows)
            {
                _evolutions[row.specialEvolutionID] = row;
            }
        }
    }
}

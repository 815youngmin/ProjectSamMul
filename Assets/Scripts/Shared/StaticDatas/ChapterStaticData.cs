#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.GameDataTypes;
using Shared.Localizers;

namespace Shared.StaticDatas
{
    /// <summary>Main chapter. Table "Chapters" (chapter numbers start at 1 and increase by 1).</summary>
    public class ChapterStaticData
    {
        public int ChapterNumber { get; set; }
        public string ChapterNameKey { get; set; } = "";
        [JsonIgnore] public string ChapterName => Localizer.Instance.GetText(ChapterNameKey);
        public ElementType ElementType { get; set; }

        public int StageNumber { get; set; }
        [JsonIgnore] public StageStaticData StageStaticData { get; private set; } = null!;


        public long TotalDropGold { get; set; }
        public long TotalLevelUpBonusGold { get; set; }
        public long TotalDropGem { get; set; }
        public long TotalRandomEquipmentTicket { get; set; }
        public long TotalRandomCharacterTicket { get; set; }
        public float RecommendedHP { get; set; }
        public float RecommendedAttackPower { get; set; }

        internal void Link(StageStaticDataRepository stages) => StageStaticData = stages.Get(StageNumber);
    }

    public class ChapterStaticDataRepository
    {
        private readonly Dictionary<int, ChapterStaticData> _chapters = new Dictionary<int, ChapterStaticData>();

        /// <summary>챕터 번호 순서.</summary>
        public IReadOnlyList<ChapterStaticData> All { get; }

        public ChapterStaticDataRepository(IReadOnlyList<ChapterStaticData> rows, StageStaticDataRepository stages)
        {
            int expected = 1;
            foreach (var row in rows)
            {
                if (row.ChapterNumber != expected)
                {
                    throw new StaticDataValidationError($"Chapter {expected} was expected but chapter {row.ChapterNumber} was found.");
                }
                row.Link(stages);
                _chapters.Add(row.ChapterNumber, row);
                ++expected;
            }
            All = rows;
        }

        public ChapterStaticData? FindChapter(int chapterNumber)
            => _chapters.TryGetValue(chapterNumber, out var chapter) ? chapter : null;
    }
}

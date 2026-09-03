#nullable enable
using System.Collections.Generic;
using Shared.GameDataTypes;
using Shared.UserDatas;

namespace Shared.CSProtocols.ReqResData.Chapters
{
    public class FinishChapterRequest : SessionRequestBase
    {
        public int ChapterNumber { get; set; }
        public StagePlayResult PlayResult { get; set; }
        public float PlayedStageTime { get; set; }
        public long GainedGolds { get; set; }
        public long GainedGems { get; set; }
        public long GainedRandomEquipmentElement { get; set; }
        public long EliminatedBosses { get; set; }
        public long EliminatedElites { get; set; }
        public long EliminatedMonsters { get; set; }

        public FinishChapterRequest(
            int chapterNumber,
            StagePlayResult playResult,
            float playedStageTime,
            long gainedGolds,
            long gainedGems,
            long gainedRandomEquipmentElement,
            long eliminatedBosses,
            long eliminatedElites,
            long eliminatedMonsters)
        {
            ChapterNumber = chapterNumber;
            PlayResult = playResult;
            PlayedStageTime = playedStageTime;
            GainedGolds = gainedGolds;
            GainedGems = gainedGems;
            GainedRandomEquipmentElement = gainedRandomEquipmentElement;
            EliminatedBosses = eliminatedBosses;
            EliminatedElites = eliminatedElites;
            EliminatedMonsters = eliminatedMonsters;
        }
    }

    public class FinishChapterResponse
    {
        public FinishChapterResultCode ResultCode { get; set; }
        public int? ClearedHighestChapter { get; set; }
        public long? IncrementAccountExp { get; set; }
        public long? ResultGold { get; set; }
        public long? RewardGold { get; set; }
        public long? IncrementGold { get; set; }
        public long? GainedGem { get; set; }
        public long? HighestStageTimeInSeconds { get; set; }
        public Dictionary<EquipmentSlot, long>? RandomEquipmentTicketResults { get; set; }
        public long? RandomCharacterTicketResults { get; set; }
        public FirstClearRewardData? FirstClearRewardData { get; set; }

        public FinishChapterResponse(FinishChapterResultCode resultCode)
        {
            ResultCode = resultCode;
        }

        public static FinishChapterResponse CreateWithErrorResult(FinishChapterResultCode resultCode) => new FinishChapterResponse(resultCode);
    }

    /// <summary>Items granted the first time a chapter is cleared.</summary>
    public class FirstClearRewardData
    {
        public HeroData? FirstClearRewardHeroData { get; set; }
        public List<EquipmentData> FirstClearRewardEquipments { get; set; } = new List<EquipmentData>();
        public long NormalSupplyBoxIncrement { get; set; }
        public long RareSupplyBoxIncrement { get; set; }
    }

}

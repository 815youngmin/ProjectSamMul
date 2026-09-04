#nullable enable
using Shared.GameDataTypes;

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
        public long? ResultGold { get; set; }
        public long? IncrementGold { get; set; }
        public long? GainedGem { get; set; }
        public long? HighestStageTimeInSeconds { get; set; }

        public FinishChapterResponse(FinishChapterResultCode resultCode)
        {
            ResultCode = resultCode;
        }

        public static FinishChapterResponse CreateWithErrorResult(FinishChapterResultCode resultCode) => new FinishChapterResponse(resultCode);
    }
}

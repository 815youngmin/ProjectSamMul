#nullable enable

namespace Shared.CSProtocols.ReqResData.Chapters
{
    public enum StagePlayResult
    {
        Cleared = 0,
        Failed = 1,
    }

    public enum EnterChapterResultCode
    {
        Success = 0,
        InvalidSession = 1,
        NotClearedPreviousChapter = 3,
        InvalidChapter = 4,
        AlreadyChapterPlaying = 5,
    }

    public enum FinishChapterResultCode
    {
        Success = 0,
        InvalidSession = 1,
        NotInStage = 3,
        NotClearedPreviousChapter = 4,
        InvalidChapter = 5,
    }

    public enum GiveUpChapterResultCode
    {
        Success = 0,
    }
}

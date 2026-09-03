#nullable enable

namespace Shared.CSProtocols.ReqResData.Chapters
{
    public class EnterChapterRequest : SessionRequestBase
    {
        public int ChapterNumber { get; set; }

        public EnterChapterRequest(int chapterNumber)
        {
            ChapterNumber = chapterNumber;
        }
    }

    public class EnterChapterResponse
    {
        public EnterChapterResultCode ResultCode { get; set; }

        public EnterChapterResponse(EnterChapterResultCode resultCode)
        {
            ResultCode = resultCode;
        }

        public static EnterChapterResponse FromError(EnterChapterResultCode resultCode) => new EnterChapterResponse(resultCode);
    }
}

#nullable enable

namespace Shared.CSProtocols.ReqResData.Chapters
{
    public class GiveUpChapterRequest : SessionRequestBase
    {
    }

    public class GiveUpChapterResponse
    {
        public GiveUpChapterResultCode ResultCode { get; }

        public GiveUpChapterResponse(GiveUpChapterResultCode resultCode)
        {
            ResultCode = resultCode;
        }
    }
}

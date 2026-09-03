#nullable enable

namespace Shared.CSProtocols.ReqResData
{
    public class ResurrectRequest : SessionRequestBase
    {
        public int StageNumber { get; set; }

        public ResurrectRequest(int stageNumber)
        {
            StageNumber = stageNumber;
        }
    }

    public class ResurrectResponse
    {
        public ResurrectResultCode ResultCode { get; set; }
        public int StageNumber { get; set; }
        public int? StageResurrectCount { get; set; }
        public long? ResultGems { get; set; }
        public long? ResultResurrectionCoins { get; set; }

        public ResurrectResponse(ResurrectResultCode resultCode, int stageNumber)
        {
            ResultCode = resultCode;
            StageNumber = stageNumber;
        }

        public static ResurrectResponse FromError(ResurrectResultCode errorCode, int stageNumber) => new ResurrectResponse(errorCode, stageNumber);
    }
}

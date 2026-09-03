#nullable enable

namespace Shared.CSProtocols.ReqResData
{
    /// <summary>Base of every request that used to carry the login session. Kept so the stage flow keeps its request/response shape.</summary>
    public abstract class SessionRequestBase
    {
        public long AccountId { get; set; } = -1;
        public string AccessToken { get; set; } = "";
    }
}

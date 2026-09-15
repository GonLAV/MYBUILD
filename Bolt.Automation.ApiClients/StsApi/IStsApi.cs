using Refit;

namespace Bolt.Automation.ApiClients.StsApi
{
    public interface IStsApi
    {
        [Get("")]
        Task<string> GetLoginPage();

        [Post("")]
        [Headers("Content-Type: application/x-www-form-urlencoded")]
        Task<IApiResponse> GetToken([Body] string body);
    }
}

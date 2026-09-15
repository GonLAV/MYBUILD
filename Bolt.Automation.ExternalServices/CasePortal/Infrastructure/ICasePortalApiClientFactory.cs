namespace Bolt.Automation.ExternalServices.CasePortal.Infrastructure
{
    public interface ICasePortalApiClientFactory
    {
        Task<ICasePortalApi> CreateApiClient();
    }
}

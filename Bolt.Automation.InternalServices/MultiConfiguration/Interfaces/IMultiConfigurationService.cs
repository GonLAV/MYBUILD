namespace Bolt.Automation.InternalServices.MultiConfiguration.Interfaces
{
    internal interface IMultiConfigurationService
    {
        Task Refresh();
        Task<T?> GetTenantSectionAsync<T>(string? tenant, string? subtenant) where T : class;
        Task<Dictionary<string, T>> GetSectionsAsync<T>() where T : class;
    }
}

using Bolt.Automation.InternalServices.Common.Interfaces;
using Bolt.Automation.InternalServices.MultiConfiguration.Interfaces;
using IScopeContext = Bolt.Automation.Common.Context.IScopeContext;

namespace Bolt.Automation.InternalServices.MultiConfiguration
{
    internal class ConfigSection<T>(IScopeContext scopeContext, IMultiConfigurationService configurationService)
        : IConfigSection<T>
        where T : class
    {
        public async Task<T?> GetAsync()
        {
            return await configurationService.GetTenantSectionAsync<T>(scopeContext.Data.Tenant.ToString(),
                scopeContext.TryGetTestValue("Subtenant", out var subtenant) ? subtenant : null);
        }
    }
}

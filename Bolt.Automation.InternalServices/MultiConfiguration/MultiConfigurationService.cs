using System.Collections.Concurrent;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.InternalServices.Common;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Bolt.Automation.InternalServices.Context;
using Bolt.Automation.InternalServices.MultiConfiguration.Interfaces;

namespace Bolt.Automation.InternalServices.MultiConfiguration
{
    internal class MultiConfigurationService : IMultiConfigurationService
    {
        private const string _microserviceType = "Bolt.Microservices.Entities.MultiConfiguration.IMultiConfigurationService";

        private readonly IBoltConfigurationRegistry _configurationRegistry;
        private readonly IServiceRegistrationService _serviceRegistrationService;

        private readonly IGenericMicroserviceClient _microserviceClient;

        private ConfigSectionsCache _sectionCache;

        public MultiConfigurationService(
            IBoltConfigurationRegistry configurationRegistry,
            IServiceRegistrationService serviceRegistrationService,
            IGenericMicroserviceClientFactory microserviceClientFactory)
        {
            _configurationRegistry = configurationRegistry;
            _serviceRegistrationService = serviceRegistrationService;

            _microserviceClient = microserviceClientFactory.CreateMicroserviceClient(
                new MicroserviceAddressResolver(_serviceRegistrationService, _microserviceType), StaticScopeContext.GetEmpty());

            _sectionCache = new ConfigSectionsCache
            {
                FreshnessId = null,
                Cache = new ConcurrentDictionary<string, Dictionary<string, ConfigSectionState>>()
            };

            Refresh().GetAwaiter().GetResult();
        }

        public async Task Refresh()
        {
            if (await IsSectionCacheFreshAsync())
            {
                return;
            }

            var allRegisteredSections = _configurationRegistry.GetAllSections();
            if (allRegisteredSections.Count == 0)
            {
                return;
            }

            var sectionKeys = new HashSet<string>(allRegisteredSections.Keys);
            var (freshnessId, sections) = await GetAllManySourcesAsync(sectionKeys);

            if (sections is null || sections.Count != sectionKeys.Count)
            {
                throw new ArgumentException("Some Sections not found");
            }

            var newSectionCache = new ConcurrentDictionary<string, Dictionary<string, ConfigSectionState>>();

            foreach (var section in sections)
            {
                var sources = new Dictionary<string, ConfigSectionState>();
                newSectionCache.TryAdd(section.Key, sources);
                foreach (var source in section.Value)
                {
                    try
                    {
                        var config = JsonHelper.Deserialize(source.Value, allRegisteredSections[section.Key]);
                        sources.Add(source.Key, new ConfigSectionState
                        {
                            ConfigSectionData = config
                        });
                    }
                    catch (Exception ex)
                    {
                        sources.Add(source.Key, new ConfigSectionState
                        {
                            ConfigSectionData = null,
                            Error = ex
                        });
                    }
                }
            }

            _sectionCache = new ConfigSectionsCache
            {
                FreshnessId = freshnessId,
                Cache = newSectionCache
            };
        }

        public async Task<T?> GetTenantSectionAsync<T>(string? tenant, string? subtenant) where T : class
        {
            if (string.IsNullOrWhiteSpace(tenant))
                throw new ArgumentNullException(nameof(tenant));

            var sectionName = _configurationRegistry.GetOrAddSection<T>();
            var sectionCache = _sectionCache.Cache ?? throw new Exception("Section cache is not initialized");
            if (!sectionCache.TryGetValue(sectionName, out var tenantsSectionSate))
            {
                tenantsSectionSate = await GetSectionForAllTenantsAsync<T>(sectionName);
                tenantsSectionSate = sectionCache.GetOrAdd(sectionName, tenantsSectionSate);
            }


            if (!string.IsNullOrWhiteSpace(subtenant)
                && tenantsSectionSate.TryGetValue($"{tenant}_{subtenant.ToUpper()}", out ConfigSectionState? sectionState))
            {
                if (sectionState.HasError)
                    throw new Exception($"Section {sectionName} for tenant {tenant} subtenant {subtenant} has error", sectionState.Error);

                return sectionState.ConfigSectionData as T;
            }

            if (tenantsSectionSate.TryGetValue(tenant, out sectionState))
            {
                if (sectionState.HasError)
                    throw new Exception($"Section {sectionName} for tenant {tenant} has error", sectionState.Error);

                return sectionState.ConfigSectionData as T;
            }

            throw new Exception($"Section {sectionName} for tenant {tenant} subtenant {subtenant ?? "N/A"} not found");
        }

        public async Task<Dictionary<string, T>> GetSectionsAsync<T>() where T : class
        {
            var sectionName = _configurationRegistry.GetOrAddSection<T>();
            var sectionCache = _sectionCache.Cache ?? throw new Exception("Section cache is not initialized");
            if (!sectionCache.TryGetValue(sectionName, out var tenantsSectionState))
            {
                tenantsSectionState = await GetSectionForAllTenantsAsync<T>(sectionName);
                tenantsSectionState = sectionCache.GetOrAdd(sectionName, tenantsSectionState);
            }

            return tenantsSectionState
                .Where(e => !e.Value.HasError)
                .Select(e => new KeyValuePair<string, T?>(e.Key, e.Value.ConfigSectionData as T))
                .Where(e => e.Value != null)
                .ToDictionary(e => e.Key, e => e.Value!);
        }

        private async Task<Dictionary<string, ConfigSectionState>> GetSectionForAllTenantsAsync<T>(string sectionName) where T : class
        {
            var sources = await GetAllSourcesAsync(sectionName)
                ?? throw new ArgumentException($"Section for {sectionName} not found");

            var result = new Dictionary<string, ConfigSectionState>();
            foreach (var tenant in sources.Keys)
            {
                try
                {
                    var section = JsonHelper.Deserialize<T>(sources[tenant]);
                    result.Add(tenant, new ConfigSectionState
                    {
                        ConfigSectionData = section
                    });
                }
                catch (Exception ex)
                {
                    result.Add(tenant, new ConfigSectionState
                    {
                        ConfigSectionData = null,
                        Error = ex
                    });
                }
            }

            return result;
        }

        private async Task<Dictionary<string, string>?> GetAllSourcesAsync(string sectionName)
        {
            var response = await _microserviceClient.PostAsync<AllTenantsSection>("GetAll", new { Key = sectionName });

            return response?.Value?.Sources;
        }

        private async Task<(string?, Dictionary<string, Dictionary<string, string>>?)> GetAllManySourcesAsync(HashSet<string> sectionName)
        {
            var response = await _microserviceClient.PostAsync<AllTenantsManySections>("GetManyAll", new { Keys = sectionName });

            return
            (
                response?.FreshnessId,
                response?.Value?.ToDictionary(o => o.Key, o => o.Value?.Sources ?? [])
            );
        }

        private async Task<bool> IsSectionCacheFreshAsync()
        {
            if (string.IsNullOrWhiteSpace(_sectionCache.FreshnessId))
                return false;

            try
            {
                var response = await _microserviceClient.PostAsync<GetFreshnessIdResponse>("GetFreshnessId", new { });
                var freshnessId = response?.FreshnessId;

                if (string.IsNullOrWhiteSpace(freshnessId))
                    return false;

                return _sectionCache.FreshnessId == freshnessId;
            }
            catch
            {
                return false;
            }
        }
    }
}

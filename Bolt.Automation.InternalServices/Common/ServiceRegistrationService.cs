using System.Collections.Concurrent;
using System.Text.Json;
using Automation.Configuration.InternalServices.BoltServices;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Bolt.Automation.InternalServices.Context;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.InternalServices.Common
{
    internal class ServiceRegistrationService : IServiceRegistrationService
    {
        private readonly IGenericMicroserviceClient _microserviceClient;

        private readonly ConcurrentDictionary<string, string> _servicesRegistry;

        public ServiceRegistrationService(
            IOptions<ServiceRegistrationOptions> options,
            IGenericMicroserviceClientFactory genericMicroserviceClientFactory)
        {
            if (string.IsNullOrWhiteSpace(options.Value.Address))
                throw new Exception("ServiceRegistrationService address is empty");

            _microserviceClient = genericMicroserviceClientFactory.CreateMicroserviceClient(
                new StaticAddressResolver(options.Value.Address), StaticScopeContext.GetEmpty());

            _servicesRegistry = new ConcurrentDictionary<string, string>();
        }

        public async Task<string> GetMicroserviceAddressAsync(string? microserviceType)
        {
            ArgumentNullException.ThrowIfNull(microserviceType);

            if (_servicesRegistry.TryGetValue(microserviceType, out string? address))
                return address;

            var response = await _microserviceClient.PostJsonAsync("getservice", new { Type = microserviceType });
            address = ExtractAddress(response);

            return _servicesRegistry.GetOrAdd(microserviceType, address);
        }

        private static string ExtractAddress(JsonDocument response)
        {
            var address = string.Empty;
            if (response.RootElement.TryGetProperty("Value", out var valueElement))
                if (valueElement.TryGetProperty("Address", out var addressElement))
                    address = addressElement.GetString();

            if (string.IsNullOrWhiteSpace(address))
                throw new Exception("Microservice Address is empty");

            return address;
        }
    }
}

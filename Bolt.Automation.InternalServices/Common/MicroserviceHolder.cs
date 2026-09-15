using Bolt.Automation.Common.Context;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Castle.DynamicProxy;

namespace Bolt.Automation.InternalServices.Common
{
    internal class MicroserviceHolder<T>(
        IScopeContext scopeContext,
        IServiceRegistrationService serviceRegistrationService,
        IGenericMicroserviceClientFactory clientFactory)
        : IMicroservice<T>
        where T : class
    {
        private static readonly ProxyGenerator _proxyGenerator;

        private readonly object _lockObject = new();

        private readonly string? _microserviceType = typeof(T).FullName;
        private T? _proxy;
        private IGenericMicroserviceClient? _genericClient;

        static MicroserviceHolder()
        {
            _proxyGenerator = new ProxyGenerator();
        }

        public T GetClient()
        {
            lock (_lockObject)
            {
                _proxy ??= CreateProxy();
            }

            return _proxy;
        }

        public async Task<string> GetAddressAsync()
        {
            return await serviceRegistrationService.GetMicroserviceAddressAsync(_microserviceType);
        }

        IGenericMicroserviceClient IMicroservice<T>.CreateLowLevelClient()
        {
            return GenericClient;
        }

        private IGenericMicroserviceClient GenericClient
        {
            get
            {
                lock (_lockObject)
                {
                    _genericClient ??= clientFactory.CreateMicroserviceClient(
                        new MicroserviceAddressResolver(serviceRegistrationService, _microserviceType), scopeContext);
                }

                return _genericClient;
            }
        }

        private T CreateProxy()
        {
            var interceptor = new MicroserviceHttpInterceptor(GenericClient);

            var proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(interceptor);

            return proxy;
        }
    }
}

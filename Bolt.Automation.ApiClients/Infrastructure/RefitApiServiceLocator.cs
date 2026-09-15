using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.ApiClients.Infrastructure
{
    public class RefitApiServiceLocator(IServiceProvider serviceProvider)
    {
        public T? GetService<T>() where T : class
        {
            return serviceProvider.GetService<T>(); // GetService returns null if not found
        }

        public T GetRequiredService<T>() where T : class
            => serviceProvider.GetService<T>()
               ?? throw new InvalidOperationException($"{typeof(T).Name} is not registered");
    }

}

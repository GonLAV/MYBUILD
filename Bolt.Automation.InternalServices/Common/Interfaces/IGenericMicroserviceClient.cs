using System.Security.Claims;
using System.Text.Json;

namespace Bolt.Automation.InternalServices.Common.Interfaces
{
    internal interface IGenericMicroserviceClient
    {
        Task<string> GetAddressAsync();
        Task<TResult?> GetAsync<TResult>(string action, Dictionary<string, string?>? queryParams, IEnumerable<Claim>? claims = null)
            where TResult : class;
        Task<JsonDocument> PostJsonAsync(string action, object content, IEnumerable<Claim>? claims = null);
        Task<TResult?> PostAsync<TResult>(string action, object content, IEnumerable<Claim>? claims = null)
            where TResult : class;
        Task<HttpClient> CreateHttpClientAsync(IEnumerable<Claim>? claims = null);
    }
}

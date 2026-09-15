using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace Bolt.Automation.InternalServices.Common
{
    internal class GenericMicroserviceClient(
        IAddressResolver addressResolver,
        string? selfAddress,
        IHttpClientFactory clientFactory,
        IScopeContext scopeContext,
        ITokenManager tokenManager,
        IAutomationLogger logger)
        : IGenericMicroserviceClient
    {
        public async Task<string> GetAddressAsync()
        {
            return await addressResolver.ResolveAddressAsync();
        }

        public async Task<TResult?> GetAsync<TResult>(string action,
            Dictionary<string, string?>? queryParams, IEnumerable<Claim>? claims = null)
            where TResult : class
        {
            var address = await GetAddressAsync();

            using var client = CreateHttpClientForAddress(address, claims);

            action = queryParams is null || queryParams.Count == 0
                ? action
                : QueryHelpers.AddQueryString(action?.Trim() ?? string.Empty, queryParams);

            using var request = new HttpRequestMessage(HttpMethod.Get, action);
            using var response = await SendAsync(client, request);

            return await Extract<TResult>(response, address, action);
        }

        public async Task<JsonDocument> PostJsonAsync(string action, object content, IEnumerable<Claim>? claims = null)
        {
            var address = await GetAddressAsync();

            using var client = CreateHttpClientForAddress(address, claims);

            using var request = new HttpRequestMessage(HttpMethod.Post, action)
            {
                Content = GetHttpContent(content)
            };

            using var response = await SendAsync(client, request);

            return await ExtractJsonDocument(response, address, action);
        }

        public async Task<TResult?> PostAsync<TResult>(string action, object content, IEnumerable<Claim>? claims = null)
            where TResult : class
        {
            var address = await GetAddressAsync();

            using var client = CreateHttpClientForAddress(address, claims);

            using var request = new HttpRequestMessage(HttpMethod.Post, action)
            {
                Content = GetHttpContent(content)
            };

            using var response = await SendAsync(client, request);

            return await Extract<TResult>(response, address, action);
        }

        public async Task<HttpClient> CreateHttpClientAsync(IEnumerable<Claim>? claims = null)
        {
            var address = await addressResolver.ResolveAddressAsync();
            return CreateHttpClientForAddress(address, claims);
        }

        private HttpClient CreateHttpClientForAddress(string address, IEnumerable<Claim>? claims)
        {
            var client = clientFactory.CreateClient("BoltMicroservice");

            var token = tokenManager.GenerateMicroserviceToken(claims ?? []);

            if (!address.EndsWith('/'))
                address += '/';

            client.BaseAddress = new Uri(address);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            MicroserviceHeaderHandler.Apply(client.DefaultRequestHeaders, scopeContext, selfAddress);

            return client;
        }

        private async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await client.SendAsync(request);
            stopwatch.Stop();

            if (response.Content is not null)
            {
                await response.Content.LoadIntoBufferAsync();
            }

            await logger.LogApiCallAsync(request, response, stopwatch.ElapsedMilliseconds);

            return response;
        }

        private static HttpContent GetHttpContent(object content)
        {
            var jsonContent = JsonHelper.Serialize(content);

            return new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        private static async Task<JsonDocument> ExtractJsonDocument(HttpResponseMessage response, string address, string action)
        {
            ArgumentNullException.ThrowIfNull(response);

            await EnsureSuccess(response, address, action);

            using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(responseStream);
        }

        private static async Task<T?> Extract<T>(HttpResponseMessage response, string address, string action) where T : class
        {
            var contentString = await ExtractString(response, address, action);
            return JsonHelper.Deserialize<T>(contentString);
        }

        private static async Task<string> ExtractString(HttpResponseMessage response, string address, string action)
        {
            ArgumentNullException.ThrowIfNull(response);

            await EnsureSuccess(response, address, action);

            if (response.Content.Headers.ContentType?.MediaType != "application/json")
            {
                throw new ArgumentException("Response content is not json", nameof(response));
            }

            return await response.Content.ReadAsStringAsync();
        }

        private static async Task EnsureSuccess(HttpResponseMessage response, string address, string action)
        {
            if (response.IsSuccessStatusCode)
                return;

            var responseContent = await response.Content.ReadAsStringAsync();

            var message =
                $"GenericMicroserviceClient Error. StatusCode {response.StatusCode}. Address: {address}. Action: {action}. Reason: {response.ReasonPhrase}. Response content: {responseContent}";
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }
}

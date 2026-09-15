# Bolt.Automation.ApiClients Documentation

This document provides an overview of the API Clients project in the Bolt Automation solution. It explains the purpose of each major class and interface, the workflow for using and extending API clients, and step-by-step guidance for adding a new API client. This guide is intended for developers new to the codebase.

---

## What is the API Clients Project?

The `Bolt.Automation.ApiClients` project is a collection of strongly-typed, reusable HTTP clients for communicating with external and internal APIs. Each API client is organized by domain (e.g., GetQuote, CaseManager, Platform, PartnerPortal, Adbx) and exposes methods for interacting with the corresponding service endpoints. The project uses [Refit](https://github.com/reactiveui/refit) for type-safe REST API calls and is designed for easy dependency injection and configuration.

> **Note:** SSO and STS APIs are documented separately and are excluded from this overview.

---

## Key Components and Classes

### 1. **ApiClientServiceExtensions**
- **Purpose:** Central registration point for all API clients. Adds each API client to the DI container and binds configuration.
- **How it works:** Call `services.AddApiClients(configuration)` in your startup to register all available API clients.

### 2. **GetQuoteApi**
- **Namespace:** `Bolt.Automation.ApiClients.GetQuoteApi`
- **Key Files:**
  - `IGetQuoteApi.cs`: Interface for all quote-related endpoints (e.g., get quote, submit application).
  - `GetQuoteApiServiceExtensions.cs`: Registers the GetQuote API client and its handlers.
  - `Handlers/GetQuoteApiKeyHandler.cs`: Adds authentication headers for requests.
  - `Models/`: Contains all request/response models for quote operations.
- **Usage:** Used to retrieve and submit insurance quotes, manage applicants, and handle quote-related workflows.

### 3. **CaseManagerApi**
- **Namespace:** `Bolt.Automation.ApiClients.CaseManagerApi`
- **Key Files:**
  - `ICaseManagerApi.cs`: Interface for case management endpoints (create/update cases, messages, users, organizations, etc.).
  - `CaseManagerApiServiceExtensions.cs`: Registers the CaseManager API client and its handlers.
  - `Handlers/CaseManagerApiKeyHandler.cs`: Handles authentication for CaseManager requests.
  - `Entities/`: Contains all request/response models for case, user, organization, and policy management.
- **Usage:** Used for managing insurance cases, users, organizations, and related workflows.

### 4. **PlatformApi**
- **Namespace:** `Bolt.Automation.ApiClients.PlatformApi`
- **Key Files:**
  - `IPlatformApi.cs`: Interface for platform-level endpoints (e.g., user management, quote status, deep links).
  - `PlatformApiServiceExtensions.cs`: Registers the Platform API client and its handlers.
  - `Entities/`: Contains models for platform operations.
- **Usage:** Used for platform-wide operations such as user provisioning, quote status, and integrations.

### 5. **PartnerPortalApi**
- **Namespace:** `Bolt.Automation.ApiClients.PartnerPortalApi`
- **Key Files:**
  - `Interfaces/IPartnerPortalApi.cs`: Interface for partner portal endpoints (e.g., invitations, login, progress tracking).
  - `PartnerPortalApiServiceExtensions.cs`: Registers the PartnerPortal API client and its handlers.
  - `Entities/`: Contains models for partner portal operations.
- **Usage:** Used for partner portal workflows, such as sending invitations and tracking progress.

### 6. **AdbxApi**
- **Namespace:** `Bolt.Automation.ApiClients.AdbxApi`
- **Key Files:**
  - `IAdbxApi.cs`: Interface for Adbx endpoints (e.g., account, lead, quote, policy management).
  - `AdbxApiServiceExtensions.cs`: Registers the Adbx API client and its handlers.
  - `Entities/`: Contains models for Adbx operations.
- **Usage:** Used for Adbx-specific integrations, such as account and lead management.

### 7. **IntegrationHubApi**
- **Namespace:** `Bolt.Automation.ApiClients.IntegrationHubApi`
- **Key Files:**
  - `Interfaces/ITwilioWebhookApi.cs`: Twilio webhook endpoints.
  - `TwilioSignatureGenerator.cs`: Generates the Twilio request signature for webhook calls.
  - `IntegrationHubApiServiceExtensions.cs`: Registers the client and its handlers.
- **Usage:** Integration-hub interactions, notably Twilio webhook simulation with correct signatures.

### 8. **Infrastructure and Utilities**
- **RefitApiServiceLocator.cs:** Utility for resolving Refit clients by type.
- **RetryHelper.cs:** Provides retry logic for transient HTTP errors.
- **IScopedRequestHeaderCache/ScopedRequestHeaderCache.cs:** Caches request headers for the duration of a request scope.
- **ApiResponseExtensions.cs:** Extension methods for working with API responses.
- **TokenCacheHelper.cs:** Utility for caching authentication tokens.

---

## How to Add a New API Client

1. **Define the API Interface:**
   - Create an interface (e.g., `INewApi.cs`) using Refit attributes to describe the endpoints.
2. **Create Models:**
   - Add request and response models in a dedicated folder (e.g., `Models/` or `Entities/`).
3. **Add Service Registration:**
   - Create a service extension (e.g., `NewApiServiceExtensions.cs`) to register the client and any handlers.
   - Add your registration call to `ApiClientServiceExtensions.cs`.
4. **Implement Handlers (Optional):**
   - If your API requires authentication or custom headers, implement a DelegatingHandler.
5. **Configure Settings:**
   - Add configuration for the new API (base URL, keys, etc.) in `appsettings.json` for each environment.
   - Add API specific configuration schema under Configuration project inside ApiClients folder.
6. **Register in Startup:**
   - Ensure `AddApiClients` is called in your DI setup.
7. **Consume the Client:**
   - Inject your API interface where needed and call its methods.

---

## Typical Workflow

1. **Registration:** All API clients are registered via DI using `AddApiClients`.
2. **Configuration:** Each client reads its settings (base URL, keys) from configuration files.
3. **Consumption:** Inject the required API interface (e.g., `IGetQuoteApi`) into your service or test.
4. **Calling Endpoints:** Use the interface methods to call API endpoints. All calls are strongly typed and return Refit `ApiResponse<T>` objects.
5. **Error Handling:** Use provided helpers (e.g., `RetryHelper`, `ApiResponseExtensions`) for error handling and retries.
6. **Extensibility:** Add new endpoints, models, or clients as needed by following the established patterns.

---

## Example: Using an API Client

```csharp
public class QuoteService
{
    private readonly IGetQuoteApi _getQuoteApi;
    public QuoteService(IGetQuoteApi getQuoteApi)
    {
        _getQuoteApi = getQuoteApi;
    }

    public async Task<QuoteResponseModel> GetQuoteAsync(QuoteRequestModel request)
    {
        var response = await _getQuoteApi.GetQuoteAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"API error: {response.Error?.Message}");
        return response.Content;
    }
}
```

---

## Best Practices
- Use strongly-typed models for all requests and responses.
- Keep each API client focused on a single domain.
- Use extension methods for service registration.
- Store secrets and URLs in configuration, not code.
- Add XML comments to interfaces and models for documentation.

---

For more details, see the source code and XML documentation in each class.

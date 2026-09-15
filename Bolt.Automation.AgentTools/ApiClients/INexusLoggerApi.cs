using Refit;

namespace Bolt.Automation.AgentTools.ApiClients;

/// <summary>
/// Refit contract for the nexus-logger Azure DevOps test-case API. Auth is a
/// user-supplied Azure AD bearer token set on the HttpClient by
/// <see cref="NexusLoggerClient"/>. Returns <see cref="ApiResponse{T}"/> so the
/// caller can inspect 401/404/5xx without exception flow.
/// </summary>
[Headers("Accept: application/json")]
public interface INexusLoggerApi
{
    [Get("/api/azure-devops/test-cases/{id}")]
    Task<ApiResponse<NexusLoggerResponse<TestCase>>> GetTestCaseAsync(int id);
}

namespace Bolt.Automation.AgentTools.ApiClients;

/// <summary>Envelope: the nexus-logger API wraps every payload in <c>{ "data": ... }</c>.</summary>
public sealed class NexusLoggerResponse<T>
{
    public T? Data { get; set; }
}

/// <summary>
/// A test case from <c>GET /api/azure-devops/test-cases/{id}</c>. Property names
/// bind from the API's camelCase via Refit's Web JSON defaults; they serialize
/// back out as snake_case (CLI output + disk cache) via the command/cache
/// serializer naming policy.
/// </summary>
public sealed class TestCase
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? State { get; set; }
    public int? Priority { get; set; }
    public string? AreaPath { get; set; }
    public string? IterationPath { get; set; }
    public string? Tags { get; set; }
    public string? AssignedTo { get; set; }
    public string? ChangedBy { get; set; }
    public string? CreatedDate { get; set; }
    public string? ChangedDate { get; set; }

    // Custom Bolt fields
    public string? Partner { get; set; }
    public string? Feature { get; set; }
    public string? TcType { get; set; }
    public string? SanityRegression { get; set; }
    public string? TcSource { get; set; }
    public string? ExecutionEnv { get; set; }
    public string? FeatureFlag { get; set; }

    public string? Description { get; set; }
    public List<TestStep>? Steps { get; set; }
    public List<TestLink>? Links { get; set; }
}

public sealed class TestStep
{
    public string? Action { get; set; }
    public string? ExpectedResult { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
}

public sealed class TestLink
{
    public string? Rel { get; set; }
    public string? Url { get; set; }
    public int? Id { get; set; }
    public string? Title { get; set; }
    public string? State { get; set; }
    public string? WorkItemType { get; set; }
}

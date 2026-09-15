using MongoDB.Bson.Serialization.Attributes;

namespace Bolt.Automation.Common.Logging.Mongo;

public class ExecutionContext
{
    [BsonElement("machineName")]
    public string MachineName { get; set; } = string.Empty;

    [BsonElement("userName")]
    public string? UserName { get; set; }

    [BsonElement("os")]
    public string? Os { get; set; }

    [BsonElement("framework")]
    public string? Framework { get; set; }

    [BsonElement("testFramework")]
    public string? TestFramework { get; set; }

    [BsonElement("isLocal")]
    public bool IsLocal { get; set; }

    public static ExecutionContext Capture()
    {
        var nunitAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "nunit.framework");

        return new ExecutionContext
        {
            MachineName = System.Environment.MachineName,
            UserName = System.Environment.UserName,
            Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            TestFramework = nunitAssembly != null ? $"NUnit {nunitAssembly.GetName().Version}" : null,
            IsLocal = string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("BUILD_BUILDID"))
        };
    }
}

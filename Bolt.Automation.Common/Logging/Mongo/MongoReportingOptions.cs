namespace Bolt.Automation.Common.Logging.Mongo;

public class MongoReportingOptions
{
    public const string SectionName = "MongoReporting";

    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "nexusAutomation";
    public CollectionNames Collections { get; set; } = new();
    public int LogBufferSize { get; set; } = 100;
    public int LogFlushIntervalMs { get; set; } = 5000;
}

public class CollectionNames
{
    public string RunSummaries { get; set; } = "run_summaries";
    public string TestRunDetails { get; set; } = "test_run_details";
    public string Logs { get; set; } = "logs";
}

using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Bolt.Automation.Common.Logging.Mongo;

public interface IMongoContext
{
    IMongoCollection<RunSummaryDocument> RunSummaries { get; }
    IMongoCollection<TestRunDetailDocument> TestRunDetails { get; }
    IMongoCollection<LogEntryDocument> Logs { get; }
    bool IsEnabled { get; }
}

public class MongoContext : IMongoContext
{
    private readonly IMongoDatabase? _database;
    private readonly MongoReportingOptions _options;

    public MongoContext(IOptions<MongoReportingOptions> options)
    {
        _options = options.Value;

        if (!_options.Enabled || string.IsNullOrEmpty(_options.ConnectionString))
            return;

        var settings = MongoClientSettings.FromConnectionString(_options.ConnectionString);
        settings.ConnectTimeout = TimeSpan.FromSeconds(5);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(10);

        var client = new MongoClient(settings);
        _database = client.GetDatabase(_options.DatabaseName);
    }

    public bool IsEnabled => _options.Enabled && _database != null;

    public IMongoCollection<RunSummaryDocument> RunSummaries =>
        _database!.GetCollection<RunSummaryDocument>(_options.Collections.RunSummaries);

    public IMongoCollection<TestRunDetailDocument> TestRunDetails =>
        _database!.GetCollection<TestRunDetailDocument>(_options.Collections.TestRunDetails);

    public IMongoCollection<LogEntryDocument> Logs =>
        _database!.GetCollection<LogEntryDocument>(_options.Collections.Logs);
}

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Bolt.Automation.Common.Logging.Mongo;

public class AwsOptions
{
    public const string SectionName = "Aws";

    public string? ProfileName { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public S3Options S3 { get; set; } = new();
}

public class S3Options
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string KeyPrefix { get; set; } = "test-runs/";
}

public interface IS3ArtifactUploader
{
    Task<string?> UploadFileAsync(string localPath, string runId, string testId, CancellationToken ct = default);
    Task<string?> UploadBytesAsync(byte[] data, string runId, string testId, string fileName, string contentType, CancellationToken ct = default);
    bool IsEnabled { get; }
}

public class S3ArtifactUploader : IS3ArtifactUploader
{
    private readonly IAmazonS3? _s3;
    private readonly S3Options _options;

    public S3ArtifactUploader(AwsOptions awsOptions)
    {
        _options = awsOptions.S3;

        if (string.IsNullOrEmpty(_options.BucketName) ||
            string.IsNullOrEmpty(awsOptions.AccessKey) ||
            string.IsNullOrEmpty(awsOptions.SecretKey))
        {
            _s3 = null;
            return;
        }

        var credentials = new BasicAWSCredentials(awsOptions.AccessKey, awsOptions.SecretKey);
        _s3 = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(_options.Region));
    }

    public bool IsEnabled => _s3 != null;

    public async Task<string?> UploadFileAsync(string localPath, string runId, string testId, CancellationToken ct = default)
    {
        if (_s3 is null || !File.Exists(localPath)) return null;

        var fileName = Path.GetFileName(localPath);
        var key = $"{_options.KeyPrefix}{runId}/{testId}/{fileName}";
        var contentType = GetContentType(fileName);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            FilePath = localPath,
            ContentType = contentType
        };

        await _s3.PutObjectAsync(request, ct).ConfigureAwait(false);

        return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{key}";
    }

    public async Task<string?> UploadBytesAsync(byte[] data, string runId, string testId, string fileName, string contentType, CancellationToken ct = default)
    {
        if (_s3 is null) return null;

        var key = $"{_options.KeyPrefix}{runId}/{testId}/{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = new MemoryStream(data),
            ContentType = contentType
        };

        await _s3.PutObjectAsync(request, ct).ConfigureAwait(false);

        return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{key}";
    }

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".html" or ".htm" => "text/html",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
}

public class NoOpS3ArtifactUploader : IS3ArtifactUploader
{
    public bool IsEnabled => false;
    public Task<string?> UploadFileAsync(string localPath, string runId, string testId, CancellationToken ct = default)
        => Task.FromResult<string?>(null);
    public Task<string?> UploadBytesAsync(byte[] data, string runId, string testId, string fileName, string contentType, CancellationToken ct = default)
        => Task.FromResult<string?>(null);
}

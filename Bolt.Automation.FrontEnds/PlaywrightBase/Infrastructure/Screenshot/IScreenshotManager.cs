namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Screenshot;


public interface IScreenshotManager
{

    Task<string> CaptureScreenshotAsync(string? name = null, string? directory = null);


    Task<string> CaptureFailureScreenshotAsync(string context, Exception? exception = null);


    Task<string> CaptureTestScreenshotAsync(string? testName = null);


    Task<string> CaptureDomSnapshotAsync(string? name = null, string? directory = null);

    Task<byte[]> CaptureScreenshotBytesAsync(string? name = null);

    Task<byte[]> CaptureDomSnapshotBytesAsync(string? name = null);

    void OpenScreenshotDirectory(string? testName = null);


    List<string> GetAllScreenshots(string? testName = null);
}
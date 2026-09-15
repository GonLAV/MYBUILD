namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Screenshot;


public class ScreenshotOptions
{

    public string BaseDirectory { get; set; } = "Screenshots";


    public bool CaptureOnFailure { get; set; } = true;


    public string TimestampFormat { get; set; } = "yyyyMMdd_HHmmss";


    public bool OrganizeByTest { get; set; } = true;


    public bool IncludeUrlOverlay { get; set; } = true;
}
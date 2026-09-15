namespace Bolt.Automation.FrontEnds.FormData
{
    public class ValidationRules
    {
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string? Pattern { get; set; }
        public string? Locator { get; set; }
    }
}


namespace Automation.Configuration.ExternalServices
{
    public class OutlookClientOptions
    {
        public const string ConfigSection = "OutlookClient";
        public string? ClientId { get; set; } 
        public string? ClientSecret { get; set; } 
        public string? TenantId { get; set; } 
    }
}

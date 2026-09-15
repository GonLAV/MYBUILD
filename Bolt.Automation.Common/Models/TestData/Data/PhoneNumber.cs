namespace Bolt.Automation.Common.Models.TestData.Data
{
    public record PhoneNumber
    {
        public string? Number { get; set; }
        public string? Type { get; set; } 
        public bool? IsDefault { get; set; } 
    }
}

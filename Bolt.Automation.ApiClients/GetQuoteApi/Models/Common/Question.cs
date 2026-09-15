namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Common
{
    public class Question
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public bool Required { get; set; }
        public float Order { get; set; }
        public List<string>? Products { get; set; }
        public IEnumerable<SelectOption>? Options { get; set; }
    }

    public class SelectOption
    {
        public string? Display { get; set; }
        public object? Value { get; set; }
        public object? Id { get; set; }
        public static object? GetValue(SelectOption o) => o.Value;
        public static object? GetId(SelectOption o) => o.Id;
    }
}

namespace Bolt.Automation.InternalServices.MultiConfiguration
{
    internal class ConfigSectionState
    {
        public object? ConfigSectionData { get; set; }
        public bool HasError => Error is not null;
        public Exception? Error { get; set; }
    }
}

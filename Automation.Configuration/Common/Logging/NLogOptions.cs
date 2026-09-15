namespace Automation.Configuration.Common.Logging
{
    public class NLogOptions
    {
        public const string ConfigSection = "NLog";
        public bool AutoReload { get; set; } = true;
        public string InternalLogLevel { get; set; } = "Info";
        public string InternalLogFile { get; set; } = "logs/internal-nlog.log";
        public List<TargetOptions> Targets { get; set; } = new List<TargetOptions>();
        public List<RuleOptions> Rules { get; set; } = new List<RuleOptions>();
    }

    public class TargetOptions
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public LayoutOptions Layout { get; set; }
        public List<HighlightRowOptions> HighlightRows { get; set; } = new List<HighlightRowOptions>();
    }

    public class LayoutOptions
    {
        public string Type { get; set; } = "SimpleLayout";
        public string Pattern { get; set; }
        public bool IncludeAllProperties { get; set; }
        public List<AttributeOptions> Attributes { get; set; } = new List<AttributeOptions>();
    }

    public class RuleOptions
    {
        public string LoggerNamePattern { get; set; } = "*";
        public string MinLevel { get; set; }
        public string WriteTo { get; set; }
    }

    public class HighlightRowOptions
    {
        public string Condition { get; set; }
        public string ForegroundColor { get; set; }
        public string BackgroundColor { get; set; }
    }

    public class AttributeOptions
    {
        public string Name { get; set; }
        public string Layout { get; set; }
    }
}
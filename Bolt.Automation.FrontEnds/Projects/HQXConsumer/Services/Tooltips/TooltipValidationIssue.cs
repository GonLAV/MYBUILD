namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.Tooltips;

public record TooltipValidationIssue(string Title, string Reason, string ExpectedFragment, string Actual);

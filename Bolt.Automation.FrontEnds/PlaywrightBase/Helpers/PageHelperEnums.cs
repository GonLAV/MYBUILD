using System.Collections.Generic;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

public enum LocatorType
{
    CSS,
    XPath,
    Text,
    Role,
    TestId,
    Placeholder,
    Label,
    Title,
    Alt,
    Name
}

public enum ElementAction
{
    Click,
    Fill,
    Type,
    Check,
    Uncheck,
    Select,
    MultiSelect,
    Clear,
    Hover,
    DoubleClick,
    Press,
    SelectRadio,
    SearchSelect
}

public enum ValueType
{
    Text,
    Value,
    IsChecked,
    IsVisible,
    SelectedValue
}

public class ElementInteractionOptions
{
    public string? Value { get; set; }
    public IReadOnlyList<string>? Values { get; set; }
    public bool IgnoreIfNotFound { get; set; } = true;
    public bool ClearField { get; set; }
    public int Timeout { get; set; }
    public bool PressTab { get; set; } = true;
    public bool PressEnter { get; set; } = false;
    public bool ForceInteractionIfNotVisible { get; set; }
    public bool UseSequentialTyping { get; set; } = false;
}

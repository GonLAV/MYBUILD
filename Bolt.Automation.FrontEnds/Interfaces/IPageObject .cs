namespace Bolt.Automation.FrontEnds.Interfaces
{
    /// <summary>
    /// Base interface for all page objects - contains common functionality
    /// </summary>
    public interface IBase
    {
        Task ClickContinue();
        Task ValidatePageReady();
    }
    public interface IInterview : IBase
    {
        Task FillForm(Dictionary<string, string>? formData = null);
    }

    public interface IPopup : IBase
    {
        Task FillForm(Dictionary<string, string>? formData = null);
        Task ClosePopup();
        Task WaitForPopupToAppear(int timeoutMs = 10000);
    }

    public interface IDashboard : IBase
    {

    }

}

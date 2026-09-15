using Bolt.Automation.FrontEnds.Interfaces;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface
{
    public interface IPageFactory
    {
        IBase CreatePage(Type pageType);
        T CreatePage<T>() where T : IBase;
    }
}
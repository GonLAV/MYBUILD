using Bolt.Automation.FrontEnds.Interfaces;

namespace Bolt.Automation.FrontEnds.Executor.Helpers
{
    public class PageCallbackManager
    {
        public delegate Task PageCallbackAsync(IInterview page);

        public enum CallbackTiming
        {
            BeforeFillForm,
            AfterFillForm,
            InsteadOfFillForm,
            InsteadOfClickContinue  
        }

        public class PageCallbackConfig
        {
            public PageCallbackAsync Callback { get; }
            public CallbackTiming Timing { get; }
            public PageCallbackConfig(PageCallbackAsync callback, CallbackTiming timing = CallbackTiming.AfterFillForm)
            {
                Callback = callback ?? throw new ArgumentNullException(nameof(callback));
                Timing = timing;
            }
        }

        public async Task<bool> RunPageCallbacks(
            Dictionary<Type, PageCallbackConfig>? pageCallbacks,
            Type pageType,
            IInterview page,
            params CallbackTiming[] timings)
        {
            if (pageCallbacks == null) return false;
            foreach (var timing in timings)
            {
                if (pageCallbacks.TryGetValue(pageType, out var config) && config.Timing == timing)
                {
                    await config.Callback(page);
                    // Return true for both InsteadOfFillForm and InsteadOfClickContinue to signal skipping
                    if (timing == CallbackTiming.InsteadOfFillForm || timing == CallbackTiming.InsteadOfClickContinue)
                        return true; 
                    if (timing == CallbackTiming.BeforeFillForm)
                        break;
                }
            }
            return false;
        }

        /// <summary>
        /// Starts a typed callback dictionary for a single page, eliminating the Dictionary constructor,
        /// typeof(), PageCallbackConfig constructor, and manual cast. Chain additional pages with .And().
        /// <code>
        /// PageCallbackManager.For&lt;MyPage&gt;(async page => { ... })
        ///     .And&lt;OtherPage&gt;(async page => { ... }, CallbackTiming.BeforeFillForm)
        /// </code>
        /// </summary>
        public static Dictionary<Type, PageCallbackConfig> For<TPage>(
            Func<TPage, Task> callback,
            CallbackTiming timing = CallbackTiming.AfterFillForm)
            where TPage : class, IInterview
        {
            return new Dictionary<Type, PageCallbackConfig>
            {
                [typeof(TPage)] = new PageCallbackConfig(page => callback((TPage)page), timing)
            };
        }
    }

    /// <summary>
    /// Extension methods for building callback dictionaries fluently via .And().
    /// </summary>
    public static class PageCallbackExtensions
    {
        public static Dictionary<Type, PageCallbackManager.PageCallbackConfig> And<TPage>(
            this Dictionary<Type, PageCallbackManager.PageCallbackConfig> callbacks,
            Func<TPage, Task> callback,
            PageCallbackManager.CallbackTiming timing = PageCallbackManager.CallbackTiming.AfterFillForm)
            where TPage : class, IInterview
        {
            callbacks[typeof(TPage)] = new PageCallbackManager.PageCallbackConfig(
                page => callback((TPage)page), timing);
            return callbacks;
        }
    }
}

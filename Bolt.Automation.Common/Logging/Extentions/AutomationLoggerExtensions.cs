using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Adapters;
using Bolt.Automation.Common.Logging.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LoggerFactory = Bolt.Automation.Common.Logging.Core.LoggerFactory;

namespace Bolt.Automation.Common.Logging.Extentions
{
    public static class AutomationLoggerExtensions
    {
        public static IServiceCollection AddAutomationLogger(this IServiceCollection services)
        {
            services.AddSingleton<Func<string, IAutomationLogger>>(sp =>
                context => LoggerFactory.CreateLogger(context)
            );
            services.AddSingleton<IAutomationLogger>(sp =>
            {
                var context = "AutomationTests";
                var scopeContext = sp.GetService<IScopeContext>();
                if (scopeContext != null)
                {
                    try
                    {
                        var testName = scopeContext.GetTestValue("TestName");
                        if (!string.IsNullOrEmpty(testName))
                        {
                            context = testName;
                        }
                    }
                    catch { }
                }
                return LoggerFactory.CreateLogger(context);
            });
            services.AddSingleton(typeof(ILogger<>), typeof(AutomationLoggerAdapter<>));
            return services;
        }

        /// <summary>
        ///     Executes an async action inside a tracked step.
        ///     Automatically marks the step as Passed on success or Failed on exception.
        ///     Variables declared outside are accessible across steps.
        /// </summary>
        public static async Task ExecuteStepAsync(this IAutomationLogger? logger, string stepName, Func<Task> action, string? description = null)
        {
            if (logger is null)
            {
                await action();
                return;
            }

            using var step = logger.StartStep(stepName, description);
            try
            {
                await action();
                step.Complete();
            }
            catch (Exception ex)
            {
                step.Fail(ex.Message);
                throw;
            }
        }

        /// <summary>
        ///     Executes an async function inside a tracked step and returns the result.
        ///     Automatically marks the step as Passed on success or Failed on exception.
        ///     The returned value is available to subsequent steps in the outer scope.
        /// </summary>
        public static async Task<T> ExecuteStepAsync<T>(this IAutomationLogger? logger, string stepName, Func<Task<T>> func, string? description = null)
        {
            if (logger is null)
                return await func();

            using var step = logger.StartStep(stepName, description);
            try
            {
                var result = await func();
                step.Complete();
                return result;
            }
            catch (Exception ex)
            {
                step.Fail(ex.Message);
                throw;
            }
        }

        /// <summary>
        ///     Executes a synchronous action inside a tracked step.
        ///     Automatically marks the step as Passed on success or Failed on exception.
        /// </summary>
        public static void ExecuteStep(this IAutomationLogger? logger, string stepName, Action action, string? description = null)
        {
            if (logger is null)
            {
                action();
                return;
            }

            using var step = logger.StartStep(stepName, description);
            try
            {
                action();
                step.Complete();
            }
            catch (Exception ex)
            {
                step.Fail(ex.Message);
                throw;
            }
        }

        /// <summary>
        ///     Executes a synchronous function inside a tracked step and returns the result.
        ///     Automatically marks the step as Passed on success or Failed on exception.
        /// </summary>
        public static T ExecuteStep<T>(this IAutomationLogger? logger, string stepName, Func<T> func, string? description = null)
        {
            if (logger is null)
                return func();

            using var step = logger.StartStep(stepName, description);
            try
            {
                var result = func();
                step.Complete();
                return result;
            }
            catch (Exception ex)
            {
                step.Fail(ex.Message);
                throw;
            }
        }
    }
}


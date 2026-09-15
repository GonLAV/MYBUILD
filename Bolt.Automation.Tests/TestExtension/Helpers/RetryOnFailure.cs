using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Bolt.Automation.Tests.TestExtension.Attributes
{
    /// <summary>
    /// Retries the test on ANY exception (including System.Exception), unlike NUnit's built-in [Retry]
    /// which only retries on AssertionException.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RetryOnFailure(int count) : NUnitAttribute, IRepeatTest
    {
        public TestCommand Wrap(TestCommand command) => new RetryOnFailureCommand(command, count);

        private sealed class RetryOnFailureCommand(TestCommand innerCommand, int retryCount) : DelegatingTestCommand(innerCommand)
        {
            public override TestResult Execute(TestExecutionContext context)
            {
                var attempts = 0;

                while (true)
                {
                    attempts++;
                    context.CurrentResult = context.CurrentTest.MakeTestResult();

                    try
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    }
                    catch (Exception ex)
                    {
                        context.CurrentResult.RecordException(ex);
                    }

                    var status = context.CurrentResult.ResultState.Status;

                    if (status == TestStatus.Passed || status == TestStatus.Skipped)
                        return context.CurrentResult;

                    if (attempts >= retryCount)
                        return context.CurrentResult;

                    context.CurrentRepeatCount++;
                }
            }
        }
    }
}
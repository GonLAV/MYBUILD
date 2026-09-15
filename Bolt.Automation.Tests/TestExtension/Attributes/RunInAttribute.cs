using BoltEnvironment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.Tests.TestExtension.Attributes
{

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RunInAttribute : Attribute
    {
        public BoltEnvironment[] Environments { get; }
        public bool IncludeProduction { get; }
        public bool IncludeStaging { get; }

        /// <summary>
        /// Run only in specific environments
        /// </summary>
        public RunInAttribute(params BoltEnvironment[] environments)
        {
            Environments = environments ?? [];
            IncludeProduction = environments.Contains(BoltEnvironment.Production);
            IncludeStaging = environments.Contains(BoltEnvironment.Staging);
        }

        /// <summary>
        /// Explicitly include Production and/or Staging
        /// </summary>
        public RunInAttribute(bool includeProduction = false, bool includeStaging = false)
        {
            IncludeProduction = includeProduction;
            IncludeStaging = includeStaging;
            Environments = [];
        }
    }

    /// <summary>
    /// Exclude environments explicitly
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class NotRunInAttribute : Attribute
    {
        public BoltEnvironment[] Environments { get; }

        public NotRunInAttribute(params BoltEnvironment[] environments)
        {
            Environments = environments ?? [];
        }
    }

    /// <summary>
    /// Marks test as safe for Production environment
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ProductionSafeAttribute : RunInAttribute
    {
        public ProductionSafeAttribute() : base(includeProduction: true) { }
    }
}
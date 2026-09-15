using System.Reflection;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.FrontEnds.FormData.Base;

namespace Bolt.Automation.FrontEnds.InterviewFlowHelpers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FlowInitializerAttribute : Attribute
    {
        public Enum? FlowType { get; }

        public FlowInitializerAttribute() { }

        public FlowInitializerAttribute(object flowType)
        {
            FlowType = flowType as Enum;
        }
    }

    public class FlowsHelpers
    {
        public Enum? FlowType { get; set; }
        public List<Type>? Pages { get; set; }
        public Dictionary<string, object>? DefaultData { get; set; }
        
        /// <summary>
        /// Specifies which property from UrlTestDataCollection to use (e.g., "FrontEnd", "AdbxApi")
        /// </summary>
        public string? UrlCollectionProperty { get; set; }
        
        /// <summary>
        /// Specifies which URL field from UrlTestData to use (e.g., "D2CUrl", "BaseUrl", "LoginUrl")
        /// </summary>
        public string? UrlField { get; set; }

        public FlowsHelpers Clone() => new()
        {
            FlowType = FlowType,
            Pages = new List<Type>(Pages ?? []),
            DefaultData = new Dictionary<string, object>(DefaultData ?? []),
            UrlCollectionProperty = UrlCollectionProperty,
            UrlField = UrlField
        };

        public static class FlowRegistry
        {
            private static readonly Dictionary<Enum, FlowsHelpers> _flows = [];
            private static readonly Dictionary<Enum, MethodInfo> _flowInitializers = new();
            private static readonly object _initLock = new();

            public static void Register(Enum flowType, FlowsHelpers flow)
            {
                lock (_initLock) { _flows[flowType] = flow; }
            }

            /// <summary>
            /// Gets a flow by type, lazily initializing it if needed.
            /// </summary>
            public static FlowsHelpers Get(Enum flowType)
            {
                lock (_initLock)
                {
                    if (_flows.TryGetValue(flowType, out var flow))
                        return flow;

                    // Not found - initialize on demand (still within lock)
                    InitializeFlowOnDemand(flowType);

                    if (_flows.TryGetValue(flowType, out flow))
                        return flow;

                    var availableFlows = _flows.Keys.Any()
                        ? string.Join(", ", _flows.Keys)
                        : "No flows registered yet";

                    throw new TestSetupException(
                        $"Flow '{flowType}' not found and could not be initialized.\n" +
                        $"Available flows: {availableFlows}");
                }
            }

            private static void InitializeFlowOnDemand(Enum flowType)
            {
                // Already within lock from Get()
                if (_flows.ContainsKey(flowType)) return;

                var project = FindProjectByFlowNamespace(flowType);
                InitializeSpecificFlow(project, flowType);

                if (!_flows.ContainsKey(flowType))
                {
                    var registeredFlows = _flows.Keys.Any()
                        ? string.Join(", ", _flows.Keys)
                        : "None";

                    throw new TestSetupException(
                        $"Flow '{flowType}' was not registered after initialization.\n" +
                        $"Registered flows: {registeredFlows}\n" +
                        $"Check that the flow method calls FlowRegistry.Register().");
                }
            }

            private static void InitializeSpecificFlow(ProjectInfo project, Enum flowType)
            {
                if (project.FlowsType == null)
                    throw new TestSetupException(
                        $"Project '{project.Type}' has no FlowsType defined in ProjectRegistry.");

                // If we already have this flow initializer cached, execute it
                if (_flowInitializers.TryGetValue(flowType, out var cachedMethod))
                {
                    cachedMethod.Invoke(null, null);
                    return;
                }

                // Find by attribute FlowType property
                var flowMethod = GetFlowMethods(project.FlowsType)
                    .FirstOrDefault(m =>
                    {
                        var attr = m.GetCustomAttribute<FlowInitializerAttribute>();
                        return attr?.FlowType?.Equals(flowType) == true;
                    });

                if (flowMethod != null)
                {
                    _flowInitializers[flowType] = flowMethod;
                    flowMethod.Invoke(null, null);
                    return;
                }

                throw new TestSetupException(
                    $"Flow '{flowType}' could not be initialized in {project.FlowsType.Name}.\n" +
                    $"Ensure a method with [FlowInitializer(FlowType.{flowType})] attribute exists.");
            }

            private static ProjectInfo FindProjectByFlowNamespace(Enum flowType)
            {
                var flowNamespace = flowType.GetType().Namespace;
                var project = ProjectRegistry.GetAllProjects()
                    .FirstOrDefault(p => p.FlowsType?.Namespace == flowNamespace);

                if (project?.FlowsType != null) return project;

                var available = ProjectRegistry.GetAllProjects()
                    .Where(p => p.FlowsType != null)
                    .Select(p => $"{p.Type} ({p.FlowsType!.Namespace})");

                throw new TestSetupException(
                    $"Could not find project for flow '{flowType}' with namespace '{flowNamespace}'.\n" +
                    $"Available projects: {(available.Any() ? string.Join(", ", available) : "No projects with FlowsType registered")}");
            }

            private static List<MethodInfo> GetFlowMethods(Type flowsType)
            {
                var methods = flowsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.ReturnType == typeof(FlowsHelpers) &&
                                m.GetParameters().Length == 0 &&
                                m.GetCustomAttribute<FlowInitializerAttribute>() != null)
                    .ToList();

                if (methods.Count == 0)
                {
                    throw new TestSetupException(
                        $"No flow initializer methods found in '{flowsType.FullName}'.\n" +
                        $"Ensure methods are marked with [FlowInitializer] attribute.");
                }

                return methods;
            }
        }
    }
}

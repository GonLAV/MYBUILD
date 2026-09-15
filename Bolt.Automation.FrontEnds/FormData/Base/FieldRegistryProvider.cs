using System.Reflection;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;

namespace Bolt.Automation.FrontEnds.FormData.Base
{
    public static class FieldRegistryProvider
    {
        private static readonly Dictionary<FrontEndType, Dictionary<string, UIElement>> _registries = [];
        private static readonly HashSet<FrontEndType> _initializedKeys = [];
        private static readonly Lock _lock = new();

        // Expose registries as a public read-only property
        public static IReadOnlyDictionary<FrontEndType, Dictionary<string, UIElement>> Registries => _registries;

        public static void Register(FrontEndType frontEndType, Dictionary<string, UIElement> fields)
        {
            lock (_lock)
            {
                _registries[frontEndType] = fields;
            }
        }

        private static void EnsureRegistryInitialized(FrontEndType frontEndType)
        {
            if (_initializedKeys.Contains(frontEndType)) return;

            lock (_lock)
            {
                if (_initializedKeys.Contains(frontEndType)) return;

                var project = ProjectRegistry.GetProject(frontEndType);

                // Use attribute to find the static field
                var fieldsField = project.FieldRegistryType?.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(f => f.GetCustomAttribute<FieldRegistryAttribute>() != null);

                if (fieldsField != null)
                {
                    _ = fieldsField.GetValue(null);
                }
                else
                {
                    throw new TestSetupException($"Could not find a static field with [FieldRegistryAttribute] on {project.FieldRegistryType?.Name}");
                }

                _initializedKeys.Add(frontEndType);
            }
        }

        public static Dictionary<string, UIElement> GetRegistry(FrontEndType frontEndType)
        {
            EnsureRegistryInitialized(frontEndType);

            if (_registries.TryGetValue(frontEndType, out var registry))
            {
                return registry;
            }

            throw new TestSetupException($"Registry for {frontEndType} was not properly initialized. Available registries: {string.Join(", ", _registries.Keys)}");
        }
    }
}

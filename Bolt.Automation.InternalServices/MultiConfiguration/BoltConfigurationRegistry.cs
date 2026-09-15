using System.Collections.Concurrent;
using Bolt.Automation.InternalServices.Common.Interfaces;

namespace Bolt.Automation.InternalServices.MultiConfiguration
{
    internal class BoltConfigurationRegistry : IBoltConfigurationRegistry
    {
        private const string SectionSuffix = "Section";
        private readonly ConcurrentDictionary<Type, string> _sections = new();

        public string GetOrAddSection(Type sectionType)
        {
            return _sections.GetOrAdd(sectionType, key => GetSectionName(key.Name));
        }

        public string GetOrAddSection<T>() where T : class
        {
            return GetOrAddSection(typeof(T));
        }

        public void AddSection(Type sectionType)
        {
            _ = GetOrAddSection(sectionType);
        }

        public void AddSection<T>() where T : class
        {
            AddSection(typeof(T));
        }

        public Dictionary<string, Type> GetAllSections()
        {
            return _sections.ToDictionary(e => e.Value, e => e.Key);
        }

        private static string GetSectionName(string typeName)
        {
            if (typeName.EndsWith(SectionSuffix))
            {
                return typeName[..typeName.LastIndexOf(SectionSuffix, StringComparison.Ordinal)];
            }

            if (typeName.EndsWith("Options"))
            {
                return typeName[..typeName.LastIndexOf("Options", StringComparison.Ordinal)];
            }

            return typeName;
        }
    }
}
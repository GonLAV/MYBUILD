namespace Bolt.Automation.InternalServices.Common.Interfaces
{
    public interface IBoltConfigurationRegistry
    {
        string GetOrAddSection(Type sectionType);
        string GetOrAddSection<T>() where T : class;
        void AddSection(Type sectionType);
        void AddSection<T>() where T : class;
        Dictionary<string, Type> GetAllSections();
    }
}

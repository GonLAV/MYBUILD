namespace Bolt.Automation.InternalServices.Common.Interfaces
{
    public interface IConfigSection<T> where T : class
    {
        Task<T?> GetAsync();
    }
}

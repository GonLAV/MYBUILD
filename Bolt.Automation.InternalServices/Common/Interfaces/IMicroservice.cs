namespace Bolt.Automation.InternalServices.Common.Interfaces
{
    internal interface IMicroservice<T> where T : class
    {
        Task<string> GetAddressAsync();
        T GetClient();
        IGenericMicroserviceClient CreateLowLevelClient();
    }
}

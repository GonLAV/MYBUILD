namespace Bolt.Automation.InternalServices.Common.Interfaces
{
    internal interface IServiceRegistrationService
    {
        Task<string> GetMicroserviceAddressAsync(string? microserviceType);
    }
}

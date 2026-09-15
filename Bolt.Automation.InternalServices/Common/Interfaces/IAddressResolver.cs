namespace Bolt.Automation.InternalServices.Common.Interfaces
{
    internal interface IAddressResolver
    {
        Task<string> ResolveAddressAsync();
    }
}

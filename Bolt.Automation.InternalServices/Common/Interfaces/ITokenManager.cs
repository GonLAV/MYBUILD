using System.Security.Claims;

namespace Bolt.Automation.InternalServices.Common.Interfaces
{
    internal interface ITokenManager
    {
        string GenerateMicroserviceToken(IEnumerable<Claim> claims);
    }
}

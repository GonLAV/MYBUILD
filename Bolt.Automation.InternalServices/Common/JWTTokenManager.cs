using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Automation.Configuration.InternalServices.BoltServices;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bolt.Automation.InternalServices.Common
{
    internal class JwtTokenManager(
        IMemoryCache memoryCache,
        IOptions<AuthenticationOptions> microserviceOptions)
        : ITokenManager
    {
        private const string? MicroserviceCacheKey = "_MicroserviceAuthToken";

        public string GenerateMicroserviceToken(IEnumerable<Claim> claims)
        {
            var options = microserviceOptions.Value;

            if (string.IsNullOrWhiteSpace(options.Issuer))
                throw new TestSetupException("Issuer is empty");
            if (string.IsNullOrWhiteSpace(options.Secret))
                throw new TestSetupException("Secret is empty");
            if (string.IsNullOrWhiteSpace(options.SecurityAlgorithmSignature))
                throw new TestSetupException("SecurityAlgorithmSignature is empty");

            return GenerateToken(MicroserviceCacheKey, claims, options.Issuer, options.ExpirationMinutes,
                options.Secret, options.SecurityAlgorithmSignature);
        }

        private string GenerateToken(string? cacheKey, IEnumerable<Claim> claims, string issuer,
            int expirationMinutes, string secret, string securityAlgorithmSignature)
        {
            if (cacheKey is not null
                && memoryCache.TryGetValue(cacheKey, out string? token)
                && token is not null)
            {
                return token;
            }

            var tokenHandler = new JwtSecurityTokenHandler
            {
                // should not set default times causing services with timezone diffrence to not authorise
                SetDefaultTimesOnTokenCreation = false
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = issuer,
                NotBefore = DateTime.UtcNow.AddMinutes(-5),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Convert.FromBase64String(secret)),
                    securityAlgorithmSignature),
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            token = tokenHandler.WriteToken(securityToken);

            if (cacheKey is not null)
            {
                memoryCache.Set(cacheKey, token, TimeSpan.FromMinutes(15));
            }

            return token;
        }
    }
}

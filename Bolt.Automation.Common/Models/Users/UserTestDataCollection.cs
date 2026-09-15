using System.Reflection;
using Bolt.Automation.Common.Models.Secrets;

namespace Bolt.Automation.Common.Models.Users
{
    public class UserTestDataCollection
    {
        public UserTestData? automationfeatureoff { get; set; }

        public UserTestData? Agent { get; set; }
        public UserTestData? AgentCL { get; set; }
        public UserTestData? Admin { get; set; }
        public UserTestData? OnlineQuote { get; set; }
        public UserTestData? D2CAgent2 { get; set; }
        public UserTestData? WFGAgent { get; set; }
        public UserTestData? ServiceAgent { get; set; }
        public UserTestData? ConsumerOrganicPL { get; set; }
        public UserTestData? Consumer { get; set; }
        public UserTestData? TestAgent { get; set; }
        public UserTestData? RootAdmin { get; set; }
        public UserTestData? Underwriter { get; set; }
        public UserTestData? PartnerPortalAgent { get; set; }
        public UserTestData? PartnerPortalAdmin { get; set; }
        public UserTestData? ConsumerKeller { get; set; }
        public UserTestData? D2CAutomation { get; set; }
        public UserTestData? BMW { get; set; }
        public UserTestData? CasePortalUser { get; set; }
        public UserTestData? ServiceManager { get; set; }
        public UserTestData? LakeviewConsumer { get; set; }
        public UserTestData? D2CAgent { get; set; }
        public UserTestData? MarketLibAgent { get; set; }
        public UserTestData? MarketLibAgentCL { get; set; }
        public UserTestData? SalesEnvironmentAdmin { get; set; }
        public UserTestData? FarmersAdmin { get; set; }
        public UserTestData? ConsumerAgentBoltAccess { get; set; }
        public UserTestData? MfaPrincipal { get; set; }
        public UserTestData? NoMfaPrincipal { get; set; }
        public UserTestData? GroupMfaAgent { get; set; }
        public UserTestData? GroupNoMfaAgent { get; set; }
        public UserTestData? LSP1 { get; set; }
        public UserTestData? LSP1CL { get; set; }
        public UserTestData? DMP { get; set; }
        public UserTestData? DMPHQX { get; set; }
        public UserTestData? Mortgage { get; set; }
        public UserTestData? MPQ3 { get; set; }
        public UserTestData? MPQ3Renters { get; set; }
        public UserTestData? ProgressiveWebsite { get; set; }

        // Every UserTestData-typed member, discovered once. Role keys in the bundle's userSecrets
        // block match these property names, so a new role is automatically covered by hydration.
        private static readonly PropertyInfo[] _userProperties =
            typeof(UserTestDataCollection)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(UserTestData) && p.CanRead && p.CanWrite)
                .ToArray();

        /// <summary>
        /// Returns a deep clone of this collection with credential fields overlaid from the bundle.
        /// The clone (never the shared static) is what tests receive, so parallel execution can't
        /// race on or leak credentials across tenants/environments. When <paramref name="secrets"/>
        /// is null (bundle not loaded, or no userSecrets for this tenant/env) the clone keeps the
        /// in-code values — which, post-migration, are empty for every credential field.
        /// </summary>
        public UserTestDataCollection HydrateFrom(IReadOnlyDictionary<string, UserSecretFields>? secrets)
        {
            var clone = new UserTestDataCollection();

            foreach (var property in _userProperties)
            {
                if (property.GetValue(this) is not UserTestData original)
                {
                    continue;
                }

                var copy = original.Clone();

                if (secrets != null && secrets.TryGetValue(property.Name, out var fields) && fields != null)
                {
                    if (fields.Password is not null) copy.Password = fields.Password;
                    if (fields.ApiKey is not null) copy.ApiKey = fields.ApiKey;
                    if (fields.OAuthToken is not null) copy.OAuthToken = fields.OAuthToken;
                    if (fields.AgentIdentity is not null) copy.AgentIdentity = fields.AgentIdentity;
                    if (fields.ApiSource is not null) copy.ApiSource = fields.ApiSource;
                }

                property.SetValue(clone, copy);
            }

            return clone;
        }
    }
}

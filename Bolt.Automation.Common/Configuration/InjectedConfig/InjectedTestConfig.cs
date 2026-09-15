using System.ComponentModel.DataAnnotations;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.Users;

namespace Bolt.Automation.Common.Configuration.InjectedConfig
{
    /// <summary>
    /// Strongly-typed configuration for runtime-injected tests (e.g. Professional Services
    /// onboarding suites). Each property carries an explicit <see cref="EnvVarAttribute"/>
    /// declaring its process-environment variable name — that string is the contract between
    /// QAs (who type it in the orchestrator's Test Variables page) and the test code.
    /// There is NO naming convention, prefix stripping, or section-separator logic anywhere;
    /// the attribute value is taken verbatim.
    ///
    /// Validated by <see cref="InjectedConfigValidator"/> in the test base constructor —
    /// fail-fast with a single aggregated error listing every missing/invalid env var.
    /// </summary>
    /// <remarks>
    /// The name "Injected" (rather than "Sanity") is deliberate: the project already uses
    /// <c>[Category("Sanity")]</c> for an unrelated test category. These tests get
    /// <c>[Category("ProfessionalServices")]</c>.
    /// </remarks>

    public sealed class InjectedTestConfig
    {
        [EnvVar("INJECTED_TENANT")]
        [Required(AllowEmptyStrings = false)]
        public string Tenant { get; set; } = "";

        [EnvVar("INJECTED_ENVIRONMENT")]
        [Required(AllowEmptyStrings = false)]
        public string Environment { get; set; } = "";

        // Single state code (e.g. "TX") or full AddressKey name (e.g. "TX_Crowley"), consumed by
        // [InjectedParameter]-marked tests on any PS surface (D2C, ADBX, Partner Portal...);
        // the orchestrator sets one value per work item. Optional — only state-parameterized
        // tests read it.
        [EnvVar("INJECTED_PS_STATE")]
        public string State { get; set; } = "";

        // Single line-of-business name (e.g. "Home", "HomeAuto", "Home + Auto"), resolved via
        // D2CLobCatalog.Canonicalize. Second fan-out axis alongside INJECTED_PS_STATE — the
        // orchestrator sets one value per work item, so a 3-state x 2-LOB selection produces six
        // work items each carrying one state and one LOB. Optional: unset means Auto, which is
        // what the PS D2C test ran before LOB became selectable, so saved runs predating the LOB
        // picker are unaffected.
        [EnvVar("INJECTED_PS_LOB")]
        public string Lob { get; set; } = "";

        public AdbxConfig Adbx { get; set; } = new();

        public PartnerPortalConfig PartnerPortal { get; set; } = new();

        public D2CConfig D2C { get; set; } = new();

        public GetQuoteApiConfig GetQuoteApi { get; set; } = new();

        public UserTestData BuildAdbxWithGetQuoteApiUser()
        {
            var user = new UserTestData
            {
                Username = Adbx.Username,
                Password = Adbx.Password,
                LoginUrl = Adbx.LoginUrl,
                ApiKey = GetQuoteApi.ApiKey
            };

            if (!string.IsNullOrWhiteSpace(GetQuoteApi.AgentIdentity))
                user.AgentIdentity = GetQuoteApi.AgentIdentity;

            return user;
        }

        public UserTestData BuildPartnerPortalUser()
        {
            return new UserTestData
            {
                Username = PartnerPortal.Username,
                Password = PartnerPortal.Password,
                LoginUrl = PartnerPortal.LoginUrl,
                Source = PartnerPortal.Source,
            };
        }

        public UserTestData BuildAdbxUser()
        {
            return new UserTestData
            {
                Username = Adbx.Username,
                Password = Adbx.Password,
                LoginUrl = Adbx.LoginUrl,
            };
        }
    }


    public sealed class AdbxConfig 
    {
        [EnvVar("INJECTED_ADBX_LOGIN_URL")]
        [Required(AllowEmptyStrings = false), Url]
        public string LoginUrl { get; set; } = "";

        [EnvVar("INJECTED_ADBX_USERNAME")]
        [Required(AllowEmptyStrings = false)]
        public string Username { get; set; } = "";

        [EnvVar("INJECTED_ADBX_PASSWORD")]
        [Required(AllowEmptyStrings = false)]
        public string Password { get; set; } = "";
    }

    public sealed class PartnerPortalConfig 
    {
        [EnvVar("INJECTED_PARTNER_PORTAL_URL")]
        [Required(AllowEmptyStrings = false), Url]
        public string LoginUrl { get; set; } = "";

        [EnvVar("INJECTED_PARTNER_PORTAL_USERNAME")]
        [Required(AllowEmptyStrings = false)]
        public string Username { get; set; } = "";

        [EnvVar("INJECTED_PARTNER_PORTAL_PASSWORD")]
        [Required(AllowEmptyStrings = false)]
        public string Password { get; set; } = "";

        [EnvVar("INJECTED_PARTNER_PORTAL_SOURCE")]
        [Required(AllowEmptyStrings = false)]
        public string Source { get; set; } = "";
    }

    public sealed class D2CConfig
    {
        [EnvVar("INJECTED_D2C_URL")]
        [Required(AllowEmptyStrings = false), Url]
        public string Url { get; set; } = "";
    }

    public sealed class GetQuoteApiConfig 
    {
        //get quote api should work with api source and when needed agent identity,
        //its not fullly configured so for now not validating it.
        [EnvVar("INJECTED_GETQUOTE_API_KEY")]
        //[Required(AllowEmptyStrings = false)]
        public string ApiKey { get; set; } = "";

        [EnvVar("INJECTED_GETQUOTE_AGENT_IDENTITY")]
        public string AgentIdentity { get; set; } = "";
    }


}

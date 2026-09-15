using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models;
using Bolt.Automation.Common.Models.Database;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.Twilio;
using Bolt.Automation.Common.Models.Urls;
using Bolt.Automation.Common.Models.Users;

namespace Bolt.Automation.Common.Context
{
    /// <summary>
    /// Strongly-typed context model for per-test/session data.
    /// </summary>
    public class TestContextData
    {
        public Environment? Environment { get; set; }
        public Tenant? Tenant { get; set; }
        public UserTestData? CurrentUser { get; set; }
        public UrlTestData? CurrentUrlType { get; set; }
        public string? CurrentUrl { get; set; }
        public UrlTestDataCollection? UrlDataCollection { get; set; }
        public DatabaseTestDataCollection? DatabaseCollection { get; set; }
        public RelayStateTestData? CurrentRelayStateType { get; set; }
        public SamlTemplateType? SamlTemplate { get; set; }
        public string? ApiKey { get; set; }
        public FrontEndType? FrontEnd { get; set; }
        public string? Source { get; set; }
        public string? QuoteId { get; set; }
        public string? FriendlyId { get; set; }
        public string? ExternalId { get; set; }
        public string? ApplicantId { get; set; }
        public LobType? Lob { get; set; }
        public TwilioTestData? TwilioData { get; set; }
        public List<string> Errors { get; set; } = new();
        public Dictionary<string, object?> Metadata { get; set; } = new();

        /// <summary>
        /// Structured application error contexts captured during the test (error pages with ticket numbers,
        /// correlation ids, or network-level error details). Each entry is grouped by source and severity.
        /// </summary>
        public List<AppErrorContext> CapturedErrors { get; set; } = [];

        /// <summary>
        /// Chronological history of all identifier snapshots captured from API responses.
        /// Each entry represents one API call. The primary fields above hold the latest values.
        /// </summary>
        public List<IdentifierSnapshot> IdentifierHistory { get; set; } = [];
    }

    /// <summary>
    /// A snapshot of identifiers captured from a single API response.
    /// </summary>
    public class IdentifierSnapshot
    {
        public string Source { get; set; } = string.Empty;
        public string? ExternalId { get; set; }
        public string? FriendlyId { get; set; }
        public string? ApplicantId { get; set; }
        public string? QuoteId { get; set; }
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    }
}

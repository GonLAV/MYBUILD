namespace Bolt.Automation.Common.Models.Twilio
{
    public sealed record TwilioTestData
    {
        public required string AuthToken { get; init; }
        public required string AccountSid { get; init; }
        public required string ToPhoneNumber { get; init; }
        public required string FromPhoneNumber { get; init; }
        public string? CaseToPhoneNumber { get; init; }
        public string? LeadToPhoneNumber { get; init; }

        public IDictionary<string, string> BuildSmsReplyPayload(string body = "Test reply", SmsContext smsContext = SmsContext.Lead, string? fromPhoneOverride = null)
        {
            var sid = Guid.NewGuid().ToString("N")[..32];
            var messageSid = $"SM{sid}";
            var toPhone = smsContext == SmsContext.Case
                ? CaseToPhoneNumber ?? ToPhoneNumber
                : LeadToPhoneNumber ?? ToPhoneNumber;

            return new Dictionary<string, string>
            {
                ["SmsMessageSid"] = messageSid,
                ["SmsSid"]        = messageSid,
                ["MessageSid"]    = messageSid,
                ["AccountSid"]    = AccountSid,
                ["Body"]          = body,
                ["From"]          = fromPhoneOverride ?? FromPhoneNumber,
                ["To"]            = toPhone,
                ["ApiVersion"]    = "2010-04-01",
            };
        }
    }
}

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Note
{
    /// <summary>
    /// The <see cref="NoteResponseModel.Action"/> values GetQuote writes against an application.
    /// </summary>
    /// <remarks>Compare case-insensitively — the platform's casing is not guaranteed.</remarks>
    public static class QuoteNoteAction
    {
        /// <summary>Written for every kickout — underwriting decline, no appetite, manufactured home.</summary>
        public const string KickOut = "Kick out";

        /// <summary>Written when a quote is bridged to a carrier, by either the consumer or an agent.</summary>
        public const string Bridge = "Bridge";

        /// <summary>Written when an agent takes a quote into edit mode.</summary>
        public const string EditQuote = "Edit Quote";

        /// <summary>Written when a consumer leaves an MPQ3 home quote via the exit-to-auto link.</summary>
        public const string ReturnedToAuto = "INET returned to auto";

        /// <summary>Written when a policy is ordered against a sold quote.</summary>
        public const string PolicyOrdered = "Policy Ordered";
    }
}

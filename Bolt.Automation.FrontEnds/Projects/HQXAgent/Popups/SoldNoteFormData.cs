namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Popups
{
    /// <summary>
    /// Data required to fill the Sold Note modal inside the Create Note popup.
    /// </summary>
    public record SoldNoteFormData
    {
        /// <summary>True = "Quote owner"; false = "On behalf of an agent".</summary>
        public bool IsQuoteOwner { get; init; } = true;

        /// <summary>Agent name to search and select when <see cref="IsQuoteOwner"/> is false.</summary>
        public string? AgentName { get; init; }

        /// <summary>Policy number to enter in the Policy # field.</summary>
        public required string PolicyNumber { get; init; }

        /// <summary>Product option text (e.g. "Homeowners").</summary>
        public required string Product { get; init; }

        /// <summary>Effective date in MM/dd/yyyy format.</summary>
        public required string EffectiveDate { get; init; }

        /// <summary>Parent company option text (e.g. "Homesite").</summary>
        public required string ParentCompany { get; init; }

        /// <summary>Premium amount (digits only, max 6 chars).</summary>
        public required string Premium { get; init; }
    }
}

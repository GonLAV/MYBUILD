namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData
{
    public partial class FieldNamesHQXAgent
    {
        public const string ContinueToFullQuote = nameof(ContinueToFullQuote);
        public const string PL_Bankruptcy = nameof(PL_Bankruptcy);
        public const string InterestedInFloodQuote = nameof(InterestedInFloodQuote);
        public const string DealershipPurchase = nameof(DealershipPurchase);
        public const string PL_CeilingHeight = nameof(PL_CeilingHeight);
        public const string PL_VaultedCeilings = nameof(PL_VaultedCeilings);
        public const string PL_CrownMolding = nameof(PL_CrownMolding);
        public const string ResHeldTrust = nameof(ResHeldTrust);
        public const string ResHeldTrust_2 = nameof(ResHeldTrust_2);
        public const string PL_Houseoccup = nameof(PL_Houseoccup);
        public const string CurrentlyOnBankruptcy = nameof(CurrentlyOnBankruptcy);
        public const string PastBankruptcy = nameof(PastBankruptcy);
        public const string RoofOver24 = nameof(RoofOver24);
        public const string FuelTanksBelowGround = nameof(FuelTanksBelowGround);

        // "Utilities replaced" question set (feature flag: ba_utilities-replaced-question) — renders
        // on the Interior page for eligible FL HO3 Agent risks (home 20-49 years old). Parent Yes/No
        // radio; selecting Yes reveals the three utility "update" child questions. Field names match
        // the rendered DOM verbatim — note "PLHeatingUpdate" has no trailing 'd', and the Agent app
        // spells electrical correctly (PLElectricalUpdated) unlike the Consumer app's "Electircal" typo.
        // The plumbing child uses a distinct registry key (UtilitiesPlumbingUpdated) because the plain
        // PLPlumbingUpdated key is already taken by the carrier-questions stub; its locator still
        // targets the real DOM id 'PLPlumbingUpdated'.
        public const string UtilitiesUpdated = nameof(UtilitiesUpdated);
        public const string UtilitiesPlumbingUpdated = nameof(UtilitiesPlumbingUpdated);
        public const string PLHeatingUpdate = nameof(PLHeatingUpdate);
        public const string PLElectricalUpdated = nameof(PLElectricalUpdated);
        public const string PL_AddressApproval = nameof(PL_AddressApproval);
        public const string PL_MortgageProperty = nameof(PL_MortgageProperty);
        public const string SelectedCarrier = nameof(SelectedCarrier);
        public const string GetESRates = nameof(GetESRates);
        public const string GetDFRates = nameof(GetDFRates);
        public const string PropertyInsuranceCancelled_2 = nameof(PropertyInsuranceCancelled_2);

        // Create Note modal fields
        public const string NoteType = nameof(NoteType);
        public const string ActionValue = nameof(ActionValue);
        public const string CallProductType = nameof(CallProductType);
        public const string ActivityDescription = nameof(ActivityDescription);
        public const string QuoteOwner = nameof(QuoteOwner);
        public const string SelectedAgentNote = nameof(SelectedAgentNote);
        public const string PolicyNumberNote = nameof(PolicyNumberNote);
        public const string EffectiveDateNote = nameof(EffectiveDateNote);
        public const string ParentCompany = nameof(ParentCompany);
        public const string PremiumNote = nameof(PremiumNote);
    }
}
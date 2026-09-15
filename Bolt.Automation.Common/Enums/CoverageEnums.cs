namespace Bolt.Automation.Common.Enums
{
    public enum CoverageEnums
    {
        // Existing
        AllPerils,
        PersonalProperty,
        PersonalLiability,
        MedicalPayments,
        WindHail,
        ExtendedReplacementCost,
        IncludeWaterBackup,
        LossOfUse,
        // Added to align with CoverageData model names
        Dwelling,
        ReplaceMentCostDwelling,
        ReplaceMentCostRoof,
        OtherStructures,
        ReplaceMentCostContents
    }

    public enum CoverageValueType
    {
        Dollar,
        PercentOfCovA,
        None
    }
}

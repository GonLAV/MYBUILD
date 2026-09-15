using Bolt.Automation.Common.Enums;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    /// <summary>
    /// The carrier-questions field(s) a given carrier requires answered to gate the bridge, expressed
    /// as data so tests don't branch on carrier. Consumed by answering only these specific fields
    /// (via <c>InteractWithField</c>) — NOT via a full page <c>FillForm</c>: the Carrier Questions
    /// registry is intentionally sparse and shared across carriers, so filling every page-tagged field
    /// would answer defaulted fields and reveal unanswered required sub-questions, leaving the form
    /// invalid and blocking the bridge.
    ///
    /// Admitted carriers render the flood-quote offer (answered "true"); Excess &amp; Surplus (Bamboo
    /// Surplus) renders the fuel-tanks question instead (answered "false"). Carriers with no explicit
    /// entry fall back to <see cref="DefaultFillData"/> (the admitted-carrier flood answer).
    /// </summary>
    public static class CarrierQuestionsFillData
    {
        private static readonly Dictionary<string, string> DefaultFillData = new()
        {
            [InterestedInFloodQuote] = "true",
        };

        private static readonly Dictionary<CarrierEnums, Dictionary<string, string>> FillDataByCarrier = new()
        {
            [CarrierEnums.BambooSurplus] = new()
            {
                [FuelTanksBelowGround] = "false",
            },
        };

        public static Dictionary<string, string> GetFillData(CarrierEnums carrier) =>
            new(FillDataByCarrier.TryGetValue(carrier, out var data) ? data : DefaultFillData);
    }
}

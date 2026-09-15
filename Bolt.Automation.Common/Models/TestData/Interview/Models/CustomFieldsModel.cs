using Newtonsoft.Json;

namespace Bolt.Automation.Common.Models.TestData.Interview.Models
{
    public class CustomFieldsModel
    {
        [JsonProperty("ABTest-new-d2c")]
        public string? ABTestNewD2c { get; set; }
        [JsonProperty("ABTest-cvg-mod-exp")]
        public string? ABTestCvgModExp { get; set; }
        [JsonProperty("ABTest-d2c-long-form")]
        public string? ABTestD2cLongForm { get; set; }
        [JsonProperty("agency_display_rates")]
        public string? AgencyDisplayRates { get; set; }
        [JsonProperty("ABTest-hqx-chatbot-cd")]
        public string? ABTestHqxChatbotCd { get; set; }
        public string? HasWaterHeaterBeenReplaced { get; set; }
        public string? MHElectircalUpdated { get; set; }
        public bool? D2CAgreeToTerms { get; set; }

        /// <summary>
        /// Up to 4 carrier names, **comma-separated in a single string** — Submission reads this with
        /// <c>TryGetValueAndCast&lt;string&gt;</c> and splits it in
        /// <c>RankingCarrierOverride.ParseRequestedCarriers</c>, so a JSON array does not work even
        /// though US 250713 writes the field as "RankingCarrierOverrides[]". When honoured, these
        /// carriers are forced into the ranking result in this order instead of Progressive's ranking
        /// being called, so a carrier that would normally be dropped from the top 4 can be tested.
        /// </summary>
        [JsonProperty("RankingCarrierOverrides")]
        public string? RankingCarrierOverrides { get; set; }

        /// <summary>
        /// Shallow copy, so a test can set a per-quote field without mutating a shared static instance.
        /// </summary>
        public CustomFieldsModel Copy() => (CustomFieldsModel)MemberwiseClone();
    }
}

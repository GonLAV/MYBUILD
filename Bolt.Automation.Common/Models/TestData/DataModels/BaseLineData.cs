using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.Common.Models.TestData.DataModels
{
    public class BaseLineData
    {
        public Address? PropertyAddress { get; set; }
        public Address? MailingAddress { get; set; }
        public Address? PreviousAddress { get; set; }
        public bool? PriorInsuranceProperty { get; set; }
        public string? EffectiveDate { get; set; }
        public string? CurrentPersonalAutoCarrier { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? DateOfBirth { get; set; }
        public string? PrimaryPhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? MaritalStatus { get; set; }
        public string? PersonalLineGender { get; set; }
        public string? EmploymentIndustry { get; set; }
        public string? OccupationStr { get; set; }
        public string? PriorCarrierExperationDate { get; set; }
        public bool? IsMailAddress { get; set; }
        public string? DeclinationReason { get; set; }
        public string? QuoteNumberSpecification { get; set; }
    }
}


namespace Bolt.Automation.Common.Models.TestData.PersonalAutoModels
{
    public class DriverModel
    {
        public string? Id { get; set; }
        public string? DriverEducation { get; set; }
        public string? DriverStateLicensed { get; set; }
        public string? LastName { get; set; }
        public string? DriverOccupationStr { get; set; }
        public bool? HasViolations { get; set; }
        public string? DOB { get; set; }
        public bool? DriversLicenseBeenSuspendedOrRevoked { get; set; }
        public string? DriverRelationshipToDriver1 { get; set; }
        public string? DriverEmploymentIndustry { get; set; }
        public int SequenceNum { get; set; }
        public bool? IsDrv2RegisterOwner { get; set; }
        public bool? HasAutoLosses { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Gender { get; set; }
        public string? DriverDateLicensed { get; set; }
        public string? DriverLicenseStatus { get; set; }
        public bool? HasAccidents { get; set; }
        public bool? SR22OrFinancialResponsibilityStatement { get; set; }
        public string? FirstName { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public bool? IsDefensiveDriver { get; set; }

        public List<LossModel>? Losses { get; set; }
        public List<ViolationModel>? Violations { get; set; }
        public List<AccidentModel>? Accidents { get; set; }
    }

}

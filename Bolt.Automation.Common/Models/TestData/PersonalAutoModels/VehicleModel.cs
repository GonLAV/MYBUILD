namespace Bolt.Automation.Common.Models.TestData.PersonalAutoModels
{
    public class VehicleModel
    {
        public string? Id { get; set; }
        public int? MilesToWork { get; set; }
        public int? NumberOfMiles { get; set; }
        public string? TransportationExpense { get; set; }
        public string? CollDeductible { get; set; }
        public bool? GarageAddressDifferent { get; set; }
        public List<AssignmentModel>? Assignments { get; set; }
        public bool? WasTheCarNew { get; set; }
        public int? SequenceNum { get; set; }
        public string? OwnershipType { get; set; }
        public string? PassiveRestraints { get; set; }
        public bool? LoanLease { get; set; }
        public string? CompDeductible { get; set; }
        public string? VIN { get; set; }
        public bool? FullGlass { get; set; }
        public string? PrimaryUseOfVehicle { get; set; }
        public string? TowingAndLabor { get; set; }
        public bool? LiabilityNotRequired { get; set; }
        public bool? AnyModifications { get; set; }
        public string? DateVehiclePurchased { get; set; }
        public decimal? CostNewValue { get; set; }
        public decimal? CurrentMarketValue { get; set; }
        public bool? AntiLockBrakes { get; set; }
        public bool? DaytimeRunningLights { get; set; }
        public string? AntiTheft { get; set; }
    }
}

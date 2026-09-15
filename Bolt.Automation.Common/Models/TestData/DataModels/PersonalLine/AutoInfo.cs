using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;

namespace Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine
{
    public partial class PersonalLineData : BaseLineData
    {
        public List<DriverModel>? Drivers { get; set; }
        public List<VehicleModel>? PersonalVehicles { get; set; }

    }
}

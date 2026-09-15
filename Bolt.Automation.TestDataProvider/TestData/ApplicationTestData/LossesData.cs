using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;
using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class Losses
        {
            public static readonly LossModel HitAnimal = new()
            {
                SequenceNum = 0,
                Id = "c357c420-7462-11eb-9439-0242ac130002",
                AutoLossesVehicleInvolved = "2",
                AutoLossesDate = "2024-03-15",
                AutoLossesDescription = "HitAnimal",
                AutoLossesAmount = "2500"
            };
        }

        public static class CommercialLosses
        {
            public static readonly CommercialLoss WcIncidentOnly = new()
            {
                Id = Guid.Parse("6a9d2a6c-8e58-11ec-b909-0242ac120002"),
                SequenceNum = 0,
                LossDate = "2020-05-05",
                TotalPaidAmount = "2500",
                LossType = "WC_IncidentOnly" // VALUES: WC_IncidentOnly, WC_LostTime, WC_MedOnly
            };
        }
    }
}

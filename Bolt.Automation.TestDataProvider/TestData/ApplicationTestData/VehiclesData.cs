using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class Vehicles
        {
            public static readonly VehicleModel SCFAD02E19GB11912 = new()
            {
                Id = "4f2fc01e-74eb-11eb-9439-0242ac130002",
                MilesToWork = 20,
                NumberOfMiles = 16380,
                TransportationExpense = "cov40to1200",
                CollDeductible = "FiveHundred",
                GarageAddressDifferent = false,
                Assignments = null,
                WasTheCarNew = false,
                SequenceNum = 0,
                OwnershipType = "Owned",
                PassiveRestraints = "AirbagBothSides",
                LoanLease = false,
                CompDeductible = "FiveHundred",
                VIN = "SCFAD02E19GB11912",
                FullGlass = true,
                PrimaryUseOfVehicle = "Pleasure",
                TowingAndLabor = "Hundred",
                LiabilityNotRequired = false,
                AnyModifications = false,
                DateVehiclePurchased = "2018-05-05",
                CostNewValue = 25000,
                CurrentMarketValue = 20000,
                AntiLockBrakes = true,
                DaytimeRunningLights = true,
                AntiTheft = "None",
            };
            public static readonly VehicleModel Vin_2B3LJ44V59H599429 = new ()
            {
                Id = "4f2fc01e-74eb-11eb-9439-0242ac130001",
                MilesToWork = 20,
                NumberOfMiles = 16380,
                TransportationExpense = "cov40to1200",
                CollDeductible = "FiveHundred",
                GarageAddressDifferent = false,
                Assignments = null,
                WasTheCarNew = false,
                SequenceNum = 1,
                OwnershipType = "Owned",
                PassiveRestraints = "AirbagBothSides",
                LoanLease = false,
                CompDeductible = "FiveHundred",
                VIN = "2B3LJ44V59H599429",
                FullGlass = true,
                PrimaryUseOfVehicle = "Pleasure",
                TowingAndLabor = "Hundred",
                LiabilityNotRequired = false,
                AnyModifications = false,
                DateVehiclePurchased = "2018-05-05",
                CostNewValue = 25000,
                CurrentMarketValue = 20000,
                AntiLockBrakes = true,
                DaytimeRunningLights = true,
                AntiTheft = "None"
            };
            public static readonly VehicleModel Vin_3KPF54AD9NE508356 = new ()
            {
                Id = "4f2fc01e-74eb-11eb-9439-0242ac130003",
                MilesToWork = 20,
                NumberOfMiles = 16380,
                TransportationExpense = "cov40to1200",
                CollDeductible = "FiveHundred",
                GarageAddressDifferent = false,
                Assignments = [],
                WasTheCarNew = false,
                SequenceNum = 0,
                OwnershipType = "Owned",
                PassiveRestraints = "AirbagBothSides",
                LoanLease = false,
                CompDeductible = "FiveHundred",
                VIN = "3KPF54AD9NE508356",
                FullGlass = true,
                PrimaryUseOfVehicle = "Pleasure",
                TowingAndLabor = "Hundred",
                LiabilityNotRequired = false,
                AnyModifications = false,
                DateVehiclePurchased = "2022-03-03",
                CostNewValue = 25000,
                CurrentMarketValue = 20000,
                AntiLockBrakes = true,
                DaytimeRunningLights = true,
                AntiTheft = "None"
            };
            public static readonly VehicleModel Vin_JM3KFACM9K1246617 = new()
            {
                Id = "f5e431a3-9495-470a-bf64-0f3e494abfe5",               
                SequenceNum = 0,
                VIN = "JM3KFACM9K1246617",
                OwnershipType = "Owned"
            };
            public static readonly VehicleModel Vin_19UDE2F33HA007791 = new()
            {
                Id = "42ad81d2-74eb-11eb-9439-0242ac130002",
                SequenceNum = 0,
                VIN = "19UDE2F33HA007791", // 2017 ACURA ILX
                OwnershipType = "Owned",
                NumberOfMiles = 10000

            };
            public static readonly VehicleModel Vin_1HGCR2F59FA221666 = new()
            {
                Id = "82ad81d2-74eb-11eb-9439-0242ac130002",
                SequenceNum = 1,
                VIN = "1HGCR2F59FA221666" // 2015 HONDA ACCORD
            };

            public static readonly VehicleModel Vin_WBA53AP01PC123415 = new()
            {
                //MilesToWork = 0,
                NumberOfMiles= 12000,
                SequenceNum= 0,
                VIN= "WBA53AP01PC123415", //BMW
                WasTheCarNew= true,
				Id= "42ad81d2-74eb-11eb-9439-0242ac130002",
                OwnershipType= "Owned",
            };

            public static readonly VehicleModel Vin_2HGFC2F69LH506663 = new()
            {
                Id = "42ad81d2-74eb-11eb-9439-0242ac130002",
                SequenceNum = 0,
                VIN = "2HGFC2F69LH506663", // 2020 HONDA CIVIC LX
                OwnershipType = "Owned",
                NumberOfMiles = 12000,
                CompDeductible = "FiveHundred",
                CollDeductible = "FiveHundred",
                PrimaryUseOfVehicle = "Pleasure",
                AntiTheft = "Active",
                DateVehiclePurchased = "2021-05-05",
            };

            // Bristol West FQ (TC 237026) — same VIN, no PrimaryUse/AntiTheft/DatePurchased.
            // Make/Year/Model/BodyStyle are resolved by the platform VIN decode.
            public static readonly VehicleModel D2CVehicle = new()
            {
                Id = "42ad81d2-74eb-11eb-9439-0242ac130002",
                SequenceNum = 0,
                VIN = "2HGFC2F69LH506663", // 2020 HONDA CIVIC LX
                OwnershipType = "Owned",
                NumberOfMiles = 12000,
                CompDeductible = "FiveHundred",
                CollDeductible = "FiveHundred",
            };

            public static readonly VehicleModel Vin_4T1BF1FK0FU1058976 = new()
            {
                VIN = "4T1BF1FK0FU105897", //Toyota
                SequenceNum = 1,
                Id = "20cfed24-3d60-4442-b650-0665bca5ac4a",
                OwnershipType = "Owned",
                NumberOfMiles = 10000,
                MilesToWork = 0,
                WasTheCarNew = true,
                AnyModifications = false,
                GarageAddressDifferent = false,
                PrimaryUseOfVehicle = "Pleasure",
                CompDeductible = "NoCoverage",
                CollDeductible = "NoCoverage",
                TowingAndLabor = "NoCoverage",
                TransportationExpense = "NoCoverage",
                PassiveRestraints = "AirbagBothSides",
            };

            // Commercial Auto vehicle 
            public static readonly VehicleModel CommercialAuto_001AN4GY3MM021769 = new()
            {
                Id = "22503c09-57dd-4a3e-8e49-3609c9477130",
                SequenceNum = 0,
                VIN = "001AN4GY3MM021769",
            };

        }
    }
}

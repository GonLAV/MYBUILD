using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using ProvisioningAddress = Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.Address;

namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData
{
        public static class Addresses
        {
            public static readonly AddressModel Mapped_AK_Anchorage = FromApplicantAddress(AddressData.AK_Anchorage);
            public static readonly AddressModel Mapped_IL_Peoria = FromApplicantAddress(AddressData.IL_Peoria);
            public static readonly AddressModel Mapped_AL_Adger = FromApplicantAddress(AddressData.AL_Adger);
            public static readonly AddressModel Mapped_AZ = FromApplicantAddress(AddressData.AZ);
            public static readonly AddressModel Mapped_AZ_Tucson = FromApplicantAddress(AddressData.AZ_Tucson);
            public static readonly AddressModel Mapped_CA = FromApplicantAddress(AddressData.CA);
            public static readonly AddressModel Mapped_CO = FromApplicantAddress(AddressData.CO);
            public static readonly AddressModel Mapped_CT = FromApplicantAddress(AddressData.CT);
            public static readonly AddressModel Mapped_CT_Cheshire = FromApplicantAddress(AddressData.CT_Cheshire);
            public static readonly AddressModel Mapped_ID = FromApplicantAddress(AddressData.ID);
            public static readonly AddressModel Mapped_IN = FromApplicantAddress(AddressData.IN);
            public static readonly AddressModel Mapped_NV = FromApplicantAddress(AddressData.NV);
            public static readonly AddressModel Mapped_GA = FromApplicantAddress(AddressData.GA);
            public static readonly AddressModel Mapped_GA_Athens = FromApplicantAddress(AddressData.GA_Athens);
            public static readonly AddressModel Mapped_HI = FromApplicantAddress(AddressData.HI);
            public static readonly AddressModel Mapped_FL = FromApplicantAddress(AddressData.FL);
            public static readonly AddressModel Mapped_FL_Bradenton = FromApplicantAddress(AddressData.FL_Bradenton);
            public static readonly AddressModel Mapped_KY = FromApplicantAddress(AddressData.KY);
            public static readonly AddressModel Mapped_LA_Gonzales = FromApplicantAddress(AddressData.LA_Gonzales);
            public static readonly AddressModel Mapped_MA = FromApplicantAddress(AddressData.MA);
            public static readonly AddressModel Mapped_MA_Westford = FromApplicantAddress(AddressData.MA_Westford);
            public static readonly AddressModel Mapped_MN = FromApplicantAddress(AddressData.MN);
            public static readonly AddressModel Mapped_MO = FromApplicantAddress(AddressData.MO);
            public static readonly AddressModel Mapped_NH = FromApplicantAddress(AddressData.NH);
            public static readonly AddressModel Mapped_NH_NOTTINGHAM = FromApplicantAddress(AddressData.NH_NOTTINGHAM);
            public static readonly AddressModel Mapped_NJ = FromApplicantAddress(AddressData.NJ);
            public static readonly AddressModel Mapped_NJ_Bridgewater = FromApplicantAddress(AddressData.NJ_Bridgewater);
            public static readonly AddressModel Mapped_NJ_NewEgypt = FromApplicantAddress(AddressData.NJ_NewEgypt);
            public static readonly AddressModel Mapped_NY_Averill_Park = FromApplicantAddress(AddressData.NY_Averill_Park);
            public static readonly AddressModel Mapped_OH = FromApplicantAddress(AddressData.OH);
            public static readonly AddressModel Mapped_OH_Dayton = FromApplicantAddress(AddressData.OH_Dayton);
        public static readonly AddressModel Mapped_OR = FromApplicantAddress(AddressData.OR);
            public static readonly AddressModel Mapped_PA = FromApplicantAddress(AddressData.PA);
            public static readonly AddressModel Mapped_PA_Bensalem = FromApplicantAddress(AddressData.PA_Bensalem);
            public static readonly AddressModel Mapped_PA_Meadville = FromApplicantAddress(AddressData.PA_Meadville);
            public static readonly AddressModel Mapped_TN = FromApplicantAddress(AddressData.TN);
            public static readonly AddressModel Mapped_TX_Magnolia = FromApplicantAddress(AddressData.TX_Magnolia);
            public static readonly AddressModel Mapped_UT = FromApplicantAddress(AddressData.UT);
            public static readonly AddressModel Mapped_WA = FromApplicantAddress(AddressData.WA);
            public static readonly AddressModel Mapped_WI = FromApplicantAddress(AddressData.WI);
            public static readonly AddressModel Mapped_MI = FromApplicantAddress(AddressData.MI);
            public static readonly AddressModel Mapped_AR = FromApplicantAddress(AddressData.AR);
            public static readonly AddressModel Mapped_DC = FromApplicantAddress(AddressData.DC);
            public static readonly AddressModel Mapped_DE = FromApplicantAddress(AddressData.DE);
            public static readonly AddressModel Mapped_IA = FromApplicantAddress(AddressData.IA);
            public static readonly AddressModel Mapped_KS = FromApplicantAddress(AddressData.KS);
            public static readonly AddressModel Mapped_MD = FromApplicantAddress(AddressData.MD);
            public static readonly AddressModel Mapped_ME = FromApplicantAddress(AddressData.ME);
            public static readonly AddressModel Mapped_MS = FromApplicantAddress(AddressData.MS);
            public static readonly AddressModel Mapped_MT = FromApplicantAddress(AddressData.MT);
            public static readonly AddressModel Mapped_NC = FromApplicantAddress(AddressData.NC);
            public static readonly AddressModel Mapped_NE = FromApplicantAddress(AddressData.NE);
            public static readonly AddressModel Mapped_NM = FromApplicantAddress(AddressData.NM);
            public static readonly AddressModel Mapped_OK = FromApplicantAddress(AddressData.OK);
            public static readonly AddressModel Mapped_RI = FromApplicantAddress(AddressData.RI);
            public static readonly AddressModel Mapped_SC = FromApplicantAddress(AddressData.SC);
            public static readonly AddressModel Mapped_SD = FromApplicantAddress(AddressData.SD);
            public static readonly AddressModel Mapped_VA = FromApplicantAddress(AddressData.VA);
            public static readonly AddressModel Mapped_VT = FromApplicantAddress(AddressData.VT);
            public static readonly AddressModel Mapped_WV = FromApplicantAddress(AddressData.WV);
            public static readonly AddressModel Mapped_WY = FromApplicantAddress(AddressData.WY);
            public static readonly AddressModel Mapped_NJ_LebanonBoro = FromApplicantAddress(AddressData.NJ_LebanonBoro);
            public static readonly AddressModel Mapped_TX_Crowley = FromApplicantAddress(AddressData.TX_Crowley);

        public static readonly ProvisioningAddress Provisioning_MO_Ozark = ToProvisioningAddress(AddressData.MO_Ozark);

            public static AddressModel GetAddress(AddressKey key) => key switch
            {
                AddressKey.AK_Anchorage => Mapped_AK_Anchorage,
                AddressKey.AL_Adger => Mapped_AL_Adger,
                AddressKey.AR => Mapped_AR,
                AddressKey.AZ => Mapped_AZ,
                AddressKey.AZ_Tucson => Mapped_AZ_Tucson,
                AddressKey.CA => Mapped_CA,
                AddressKey.CO => Mapped_CO,
                AddressKey.CT => Mapped_CT,
                AddressKey.CT_Cheshire => Mapped_CT_Cheshire,
                AddressKey.DC => Mapped_DC,
                AddressKey.DE => Mapped_DE,
                AddressKey.FL => Mapped_FL,
                AddressKey.FL_Bradenton => Mapped_FL_Bradenton,
                AddressKey.GA => Mapped_GA,
                AddressKey.GA_Athens => Mapped_GA_Athens,
                AddressKey.HI => Mapped_HI,
                AddressKey.IA => Mapped_IA,
                AddressKey.ID => Mapped_ID,
                AddressKey.IL_Peoria => Mapped_IL_Peoria,
                AddressKey.IN => Mapped_IN,
                AddressKey.KS => Mapped_KS,
                AddressKey.KY => Mapped_KY,
                AddressKey.LA_Gonzales => Mapped_LA_Gonzales,
                AddressKey.MA => Mapped_MA,
                AddressKey.MA_Westford => Mapped_MA_Westford,
                AddressKey.MD => Mapped_MD,
                AddressKey.ME => Mapped_ME,
                AddressKey.MI => Mapped_MI,
                AddressKey.MN => Mapped_MN,
                AddressKey.MO => Mapped_MO,
                AddressKey.MS => Mapped_MS,
                AddressKey.MT => Mapped_MT,
                AddressKey.NC => Mapped_NC,
                AddressKey.NE => Mapped_NE,
                AddressKey.NH => Mapped_NH,
                AddressKey.NH_NOTTINGHAM => Mapped_NH_NOTTINGHAM,
                AddressKey.NJ => Mapped_NJ,
                AddressKey.NJ_Bridgewater => Mapped_NJ_Bridgewater,
                AddressKey.NJ_NewEgypt => Mapped_NJ_NewEgypt,
                AddressKey.NJ_LebanonBoro => Mapped_NJ_LebanonBoro,
                AddressKey.NM => Mapped_NM,
                AddressKey.NV => Mapped_NV,
                AddressKey.NY_Averill_Park => Mapped_NY_Averill_Park,
                AddressKey.OH => Mapped_OH,
                AddressKey.OH_Dayton => Mapped_OH_Dayton,
                AddressKey.OK => Mapped_OK,
                AddressKey.OR => Mapped_OR,
                AddressKey.PA => Mapped_PA,
                AddressKey.PA_Bensalem => Mapped_PA_Bensalem,
                AddressKey.PA_Meadville => Mapped_PA_Meadville,
                AddressKey.RI => Mapped_RI,
                AddressKey.SC => Mapped_SC,
                AddressKey.SD => Mapped_SD,
                AddressKey.TN => Mapped_TN,
                AddressKey.TX_Magnolia => Mapped_TX_Magnolia,
                AddressKey.TX_Crowley => Mapped_TX_Crowley,
                AddressKey.UT => Mapped_UT,
                AddressKey.VA => Mapped_VA,
                AddressKey.VT => Mapped_VT,
                AddressKey.WA => Mapped_WA,
                AddressKey.WI => Mapped_WI,
                AddressKey.WV => Mapped_WV,
                AddressKey.WY => Mapped_WY,
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, $"No address mapped for {key}")
            };

        private static AddressModel FromApplicantAddress(Address applicantAddress)
            {
                return new AddressModel
                {
                    Addr1 = applicantAddress.AddressLine1 ?? string.Empty,
                    Addr2 = applicantAddress.AddressLine2 ?? string.Empty,
                    City = applicantAddress.City ?? string.Empty,
                    StateProvCd = applicantAddress.State ?? string.Empty,
                    PostalCode = applicantAddress.ZipCode ?? string.Empty,
                    Country = string.Empty
                };
            }

            private static ProvisioningAddress ToProvisioningAddress(Address applicantAddress)
            {
                return new ProvisioningAddress
                {   
                    AddressLine1 = applicantAddress.AddressLine1 ?? string.Empty,
                    AddressLine2 = applicantAddress.AddressLine2 ?? string.Empty,
                    City = applicantAddress.City ?? string.Empty,
                    State = applicantAddress.State ?? string.Empty,
                    Zip = applicantAddress.ZipCode ?? string.Empty
                };
            }
        }
}


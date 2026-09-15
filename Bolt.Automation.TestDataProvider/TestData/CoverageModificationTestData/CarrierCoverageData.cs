using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData
{
    public record Carrier(CarrierEnums CarrierEnum, string Name, Coverage[] Coverages);
    public record Coverage(CoverageEnums CoverageEnum, StateValues[] States);
    public record StateValues(string States, CoverageValue[] Values);
    public record CoverageValue(CoverageValueType Type, double? Value);

    public static class CarrierCoverageData
    {
        private static string S(string states) => states;
        private static CoverageValue D(int value) => new(CoverageValueType.Dollar, value);
        private static CoverageValue P(double value) => new(CoverageValueType.PercentOfCovA, value);
        private static CoverageValue N() => new(CoverageValueType.None, null);

        public static readonly Carrier[] Carriers =
        [
            // ------------------------ Homesite ------------------------
            new(CarrierEnums.Homesite, "Homesite", [
                new Coverage(CoverageEnums.AllPerils, [
                    new StateValues(S("AZ, IL, MO, PA, GA, NV"), [D(500), D(1000), D(1500), D(2500), D(5000)]),
                    new StateValues(S("OH, IN, WI, TN, KY, TX"), [D(500), D(1000), D(1500), D(2500), D(5000)]),
                    new StateValues(S("NJ"), [D(500), D(1000), D(1500), D(2500), D(5000)])
                ]),
                new Coverage(CoverageEnums.PersonalProperty, [
                    new StateValues(S("ALL"), [P(50), P(70)])
                ]),
                new Coverage(CoverageEnums.PersonalLiability, [
                    new StateValues(S("ALL"), [D(100000), D(300000), D(500000)])
                ]),
                new Coverage(CoverageEnums.MedicalPayments, [
                    new StateValues(S("ALL"), [D(1000), D(3000), D(5000)])
                ]),
                new Coverage(CoverageEnums.WindHail, [
                    new StateValues(S("AZ, GA, NV"), [
                        D(500), D(1000), D(1500), D(2500), D(5000),
                        P(1), P(2), P(3), P(4), P(5), P(10)
                    ]),
                    new StateValues(S("IL, IN, OH, MO, TN, TX"), [
                        D(2500), D(5000),
                        P(1), P(2), P(3), P(4), P(5), P(10)
                    ]),
                    new StateValues(S("WI, KY"), [
                        D(1500), D(2500), D(5000),
                        P(1), P(2), P(3), P(4), P(5), P(10)
                    ]),
                    new StateValues(S("PA, NJ"), [
                        D(1000), D(1500), D(2500), D(5000),
                        P(1), P(2), P(3), P(4), P(5), P(10)
                    ])
                ]),
                new Coverage(CoverageEnums.ExtendedReplacementCost, [
                    new StateValues(S("AZ, OH, WI, MO, PA, TN, IL, IN, KY, NV"), [N(), P(25), P(50)]),
                    new StateValues(S("GA, NJ, TX"), [N(), P(25)])
                ]),
                new Coverage(CoverageEnums.IncludeWaterBackup, [
                    new StateValues(S("AZ, OH, WI, MO, PA, TN, GA, KY, NJ, NV, TX"),
                        [N(), D(5000), D(10000), D(15000)]),
                    new StateValues(S("IL, IN"), [N(), D(5000), D(10000), D(15000), D(20000), D(25000)])
                ])
            ]),

            // ------------------------ ASI ------------------------
            new(CarrierEnums.ASI, "ASI", [
                new Coverage(CoverageEnums.PersonalProperty, [
                    new StateValues(S("UT, ID, OR, WA, NV, AZ, TN, WI, IN, PA, KY, MN, WV, IL"), [P(50), P(55), P(60), P(65), P(70)]),
                    new StateValues(S("MI, OH, NY"), [P(50), P(70)])

                ]),
                new Coverage(CoverageEnums.LossOfUse, [
                    new StateValues(S("AZ"), [P(10), P(15), P(20), P(25), P(30)]),
                    new StateValues(S("OR, WA, UT, NV, ID, MI, NY, TN, WI, IN, PA, KY, MN, WV, IL"), [P(10), P(20)]),
                    new StateValues(S("OH"), [P(10), P(15), P(20), P(25), P(30), P(40), P(50)])
                ]),
                new Coverage(CoverageEnums.PersonalLiability, [
                    new StateValues(S("AZ, NV, UT, OR, WA, ID, NY, MN, WV, IL"), [D(100000), D(300000), D(500000)]),
                    new StateValues(S("MI, OH, TN, WI, IN, PA, KY"), [D(100000), D(300000), D(500000), D(1000000)])
                ]),
                new Coverage(CoverageEnums.MedicalPayments, [
                    new StateValues(S("AZ"), [D(500), D(1000), D(2500), D(5000)]),
                    new StateValues(S("OR, WA, UT, NV, ID, MI, OH, NY, TN, WI, IN, PA, KY, MN, WV, IL"), [D(1000), D(2500), D(5000)])
                ]),
                new Coverage(CoverageEnums.AllPerils, [
                    new StateValues(S("WA, NV"), [D(1000), D(2500), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("ID"), [D(1000), D(2500), D(5000), P(1)]),
                    new StateValues(S("OR, AZ"), [D(1000), D(1500), D(2500), D(5000), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("UT"), [D(1000), D(2500), D(5000), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("OH, TN, WI, IN, PA, KY, MN, WV, IL"), [D(1000), D(1500), D(2000), D(2500), D(5000), D(7500), D(10000), D(25000), P(0.5), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("MI"), [D(1000), D(1500), D(2000), D(2500), D(5000), D(7500), D(10000), D(25000), P(0.5), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("NY"), [D(1000), D(2500), P(1)])
                ]),
                new Coverage(CoverageEnums.WindHail, [
                    new StateValues(S("AZ, OR"), [D(1000), D(1500), D(2500), D(5000), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("ID"), [D(1000), D(2500), D(5000), P(1), P(2), P(5)]),
                    new StateValues(S("WA, NV"), [D(1000), D(2500), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("UT"), [D(1000), D(2500), D(5000), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("OH, TN, WI, IN, PA, KY, MN, WV, IL"), [D(1000), D(1500), D(2000), D(2500), D(5000), D(7500), D(10000), D(25000), P(0.5), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("MI"), [D(1000), D(1500), D(2000), D(2500), D(5000), D(7500), D(10000), D(25000), P(0.5), P(1), P(2), P(3), P(5)]),
                    new StateValues(S("NY"), [D(1000), D(2500), P(1), P(2), P(5)])
                ]),
                new Coverage(CoverageEnums.ExtendedReplacementCost, [
                    new StateValues(S("AZ, NV, UT, OR, MI, OH, TN, WI, IN, PA, KY"), [N(), P(25), P(50), P(100)]),
                    new StateValues(S("WA, ID, NY, MN, WV, IL"), [N(), P(25), P(50)])
                ]),
                new Coverage(CoverageEnums.IncludeWaterBackup, [
                    new StateValues(S("AZ, NV, UT, OR, WA, ID, NY"), [N(), D(5000), D(10000), D(25000)]),
                    new StateValues(S("MI, TN, WI, IN, PA, KY, MN, WV, IL"), [N(), D(5000), D(10000), D(25000), D(50000)]),
                    new StateValues(S("OH"), [N(), D(5000), D(10000), D(25000), D(50000), D(75000), D(100000)])
            ]),
                ]),

            // ------------------------ PlymouthRock ------------------------
            new(CarrierEnums.PlymouthRock, "PlymouthRock", [
                //new Coverage(CoverageEnums.PersonalProperty, [
                //    new StateValues(S("ALL"), [P(40), P(50), P(60), P(70)])
                //]),
                new Coverage(CoverageEnums.LossOfUse, [
                    new StateValues(S("CT"), [P(30), P(40), P(50)]),
                    new StateValues(S("NJ, NH, PA, MA"), [P(20), P(30), P(40), P(50)])
                ]),
                new Coverage(CoverageEnums.PersonalLiability, [
                    new StateValues(S("ALL"), [D(100000), D(200000), D(300000), D(400000), D(500000), D(1000000)])
                ]),
                new Coverage(CoverageEnums.MedicalPayments, [
                    new StateValues(S("ALL"), [D(1000), D(2000), D(3000), D(4000), D(5000)])
                ]),
                new Coverage(CoverageEnums.AllPerils, [
                    new StateValues(S("ALL"), [D(1000), D(2000), D(2500), D(5000)])
                ]),
                new Coverage(CoverageEnums.ExtendedReplacementCost, [
                    new StateValues(S("ALL"), [N(), P(25), P(50)])
                ]),
                new Coverage(CoverageEnums.IncludeWaterBackup, [
                    new StateValues(S("PA"), [N(), D(5000), D(10000), D(25000), D(50000)]),
                    new StateValues(S("NJ, NH, CT, MA"), [N(), D(5000), D(10000), D(25000)])
                ])
            ])
        ];


        /// <summary>
        /// Gets coverage values for a specific carrier enum, coverage type, and state
        /// </summary>
        public static CoverageValue[] GetCoverageValues(CarrierEnums carrierEnum, CoverageEnums coverageType,
            string state)
        {
            var carrier = Carriers.FirstOrDefault(c => c.CarrierEnum == carrierEnum);

            if (carrier == null)
                return [];

            var coverage = carrier.Coverages.FirstOrDefault(c => c.CoverageEnum == coverageType);
            if (coverage == null)
                return [];

            var stateGroup = coverage.States.FirstOrDefault(s =>
                s.States.Equals("ALL", StringComparison.OrdinalIgnoreCase) ||
                s.States.Split(',').Any(st => string.Equals(st.Trim(), state, StringComparison.OrdinalIgnoreCase)));

            return stateGroup?.Values ?? [];
        }

        /// <summary>
        /// Gets all coverage types for a specific carrier enum
        /// </summary>
        public static CoverageEnums[] GetCoverageTypesForCarrier(CarrierEnums carrierEnum)
        {
            var carrier = Carriers.FirstOrDefault(c => c.CarrierEnum == carrierEnum);

            return carrier?.Coverages.Select(c => c.CoverageEnum).ToArray() ?? [];
        }
    }
}
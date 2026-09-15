using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.TestDataProvider.TestData.CommonTestData
{
    public static class AddressData
    {
        // Alaska manufactured-home (MFH) address; in appetite for the Assurant / American Bankers
        // manufactured-home program used to verify the carrier information statement on Rates.
        public static readonly Address AK_Anchorage = new()
        {
            AddressLine1 = "6851 Tamir Ave",
            AddressLine2 = string.Empty,
            City = "Anchorage",
            State = "AK",
            ZipCode = "99504"
        };

        public static readonly Address AL_Adger = new()
        {
            AddressLine1 = "3255 Betty Hill Rd",
            AddressLine2 = "",
            ZipCode = "35006",
            City = "Adger",
            State = "AL"
        };

        public static readonly Address AR = new()
        {
            AddressLine1 = "104 Belles Fleurs Blvd",
            AddressLine2 = string.Empty,
            City = "Little Rock",
            State = "AR",
            ZipCode = "72223"
        };

        public static readonly Address AZ = new()
        {
            AddressLine1 = "1426 E Sheffield Ave",
            AddressLine2 = string.Empty,
            City = "Gilbert",
            State = "AZ",
            ZipCode = "85225"
        };

        public static readonly Address AZ_Tucson = new()
        {
            AddressLine1 = "7842 N Maiden Pools Pl",
            AddressLine2 = "7842",
            City = "Tucson",
            State = "AZ",
            ZipCode = "85743",
            County= "PIMA"
        };

        public static readonly Address CA = new()
        {
            AddressLine1 = "311 W Raymond St",
            AddressLine2 = string.Empty,
            City = "Compton",
            State = "CA",
            ZipCode = "90220"
        };

        public static readonly Address CA_ThousandOaks = new()
        {
            AddressLine1 = "405 HODENCAMP",
            AddressLine2 = string.Empty,
            City = "THOUSAND OAKS",
            State = "CA",
            ZipCode = "91360"
        };

        public static readonly Address CO = new()
        {
            AddressLine1 = "862 Russellville Rd",
            AddressLine2 = string.Empty,
            City = "Franktown",
            State = "CO",
            ZipCode = "80116"
        };

        // Good address to check without heating type
        public static readonly Address CT = new()
        {
            AddressLine1 = "31 Adams St",
            AddressLine2 = string.Empty,
            City = "Waterbury",
            State = "CT",
            ZipCode = "06704"
        };

        // Cheshire, CT — Plymouth Rock returns rates here on QA (used by the CovMod dropdown case).
        public static readonly Address CT_Cheshire = new()
        {
            AddressLine1 = "100 Stonegate Ct",
            AddressLine2 = string.Empty,
            City = "Cheshire",
            State = "CT",
            ZipCode = "06410"
        };

        public static readonly Address CT_Manchester = new()
        {
            AddressLine1 = "180 Deer Run Trail",
            AddressLine2 = string.Empty,
            City = "Manchester",
            State = "CT",
            ZipCode = "06042"
        };

        public static readonly Address DC = new()
        {
            AddressLine1 = "3709 Jamison St NE",
            AddressLine2 = string.Empty,
            City = "Washington",
            State = "DC",
            ZipCode = "20018"
        };

        public static readonly Address DE = new()
        {
            AddressLine1 = "3759 Green St",
            AddressLine2 = string.Empty,
            City = "Claymont",
            State = "DE",
            ZipCode = "19703"
        };

        public static readonly Address FL = new()
        {
            AddressLine1 = "319 Tomelloso Way",
            AddressLine2 = string.Empty,
            City = "DAVENPORT",
            State = "FL",
            ZipCode = "33837"
        };

        // Bradenton, FL — used by the "utilities replaced" question feature, which is presented only
        // in FL, HO3, when the home's yearBuilt is 20-49 years old (feature flag ba_utilities-replaced-question).
        public static readonly Address FL_Bradenton = new()
        {
            AddressLine1 = "5030 58th Ter E",
            AddressLine2 = string.Empty,
            City = "Bradenton",
            State = "FL",
            ZipCode = "34203"
        };

        public static readonly Address GA = new()
        {
            AddressLine1 = "2489 Brittany Park Ln",
            AddressLine2 = "",
            City = "Ellenwood",
            State = "GA",
            ZipCode = "30294"
        };

        // Athens, GA — outside Stillwater's appetite; used to drive a clean DNQ (Did Not Qualify)
        // kickout when an exotic-pet eligibility knockout is selected on the Details page.
        public static readonly Address GA_Athens = new()
        {
            AddressLine1 = "1041 Thornwell Dr",
            AddressLine2 = "",
            City = "Athens",
            State = "GA",
            ZipCode = "30606"
        };

        // Gonzales, LA — rates both Homesite and ASI, but with 3+ farm animals Homesite returns a
        // partner declination and ASI a UUD, so the submission completes with nothing to show.
        public static readonly Address LA_Gonzales = new()
        {
            AddressLine1 = "10237 Lake Park Ave",
            AddressLine2 = "",
            City = "Gonzales",
            State = "LA",
            ZipCode = "70737"
        };

        // Hawaii is out of appetite for Progressive HQX quoting; used to trigger a NoAppetite / "No available carriers" kickout.
        public static readonly Address HI = new()
        {
            AddressLine1 = "650 Kaulana Pl",
            AddressLine2 = string.Empty,
            City = "Honolulu",
            State = "HI",
            ZipCode = "96825"
        };

        public static readonly Address IA = new()
        {
            AddressLine1 = "365 Canterbury St",
            AddressLine2 = string.Empty,
            City = "North Liberty",
            State = "IA",
            ZipCode = "52317"
        };

        public static readonly Address ID = new()
        {
            AddressLine1 = "2908 N Villere Ln",
            AddressLine2 = string.Empty,
            City = "Meridian",
            State = "ID",
            ZipCode = "83646"
        };

        public static readonly Address IL = new()
        {
            AddressLine1 = "123 Elm St",
            AddressLine2 = string.Empty,
            City = "Springfield",
            State = "IL",
            ZipCode = "62701"
        };


        public static readonly Address IL_Peoria = new()
        {
            AddressLine1 = "5355 N Kirsten Cu",
            AddressLine2 = string.Empty,
            City = "Peoria",
            State = "IL",
            ZipCode = "61615"
        };

        public static readonly Address IN = new()
        {
            AddressLine1 = "2012 Autumn Ridge Ln",
            AddressLine2 = string.Empty,
            City = "Elkhart",
            State = "IN",
            ZipCode = "46514"
        };

        public static readonly Address KS = new()
        {
            AddressLine1 = "907 Chisholm Trl",
            AddressLine2 = string.Empty,
            City = "Junction City",
            State = "KS",
            ZipCode = "66441"
        };

        public static readonly Address KY = new()
        {
            AddressLine1 = "2165 Rutledge Ave",
            AddressLine2 = string.Empty,
            City = "Lexington",
            State = "KY",
            ZipCode = "40509"
        };

        public static readonly Address MA = new()
        {
            AddressLine1 = "43 Loring Avenue",
            AddressLine2 = string.Empty,
            City = "Boxborough",
            State = "MA",
            ZipCode = "01719"
        };

        // Westford, MA — Plymouth Rock returns rates here on QA (used by the CovMod dropdown case).
        public static readonly Address MA_Westford = new()
        {
            AddressLine1 = "59 Nutting Rd",
            AddressLine2 = string.Empty,
            City = "Westford",
            State = "MA",
            ZipCode = "01886"
        };

        public static readonly Address MD = new()
        {
            AddressLine1 = "1262 Drydock St",
            AddressLine2 = string.Empty,
            City = "Brunswick",
            State = "MD",
            ZipCode = "21716"
        };

        public static readonly Address ME = new()
        {
            AddressLine1 = "38 Mattie Lane",
            AddressLine2 = string.Empty,
            City = "Durham",
            State = "ME",
            ZipCode = "04222"
        };

        public static readonly Address MI = new()
        {
            AddressLine1 = "13950 Brady Rd",
            AddressLine2 = string.Empty,
            City = "Bellevue",
            State = "MI",
            ZipCode = "49021"
        };

        public static readonly Address MN = new()
        {
            AddressLine1 = "3700 Vincent Ave N",
            AddressLine2 = string.Empty,
            City = "Minneapolis",
            State = "MN",
            ZipCode = "55412"
        };

        public static readonly Address MO = new()
        {
            AddressLine1 = "120 Cloverleaf Meadows Ct",
            AddressLine2 = string.Empty,
            City = "O'Fallon",
            State = "MO",
            ZipCode = "63366"
        };

        public static readonly Address MS = new()
        {
            AddressLine1 = "19 Meadow Path Cir",
            AddressLine2 = string.Empty,
            City = "Picayune",
            State = "MS",
            ZipCode = "39466"
        };

        public static readonly Address MT = new()
        {
            AddressLine1 = "1296 Watson Peak Rd",
            AddressLine2 = string.Empty,
            City = "Billings",
            State = "MT",
            ZipCode = "59105"
        };

        public static readonly Address NC = new()
        {
            AddressLine1 = "1014 Granite Grove",
            AddressLine2 = string.Empty,
            City = "Leland",
            State = "NC",
            ZipCode = "28451"
        };

        public static readonly Address NE = new()
        {
            AddressLine1 = "834 N 26th St",
            AddressLine2 = string.Empty,
            City = "Beatrice",
            State = "NE",
            ZipCode = "68310"
        };

        public static readonly Address NH = new()
        {
            AddressLine1 = "654 Raymond Road",
            AddressLine2 = string.Empty,
            City = "Auburn",
            State = "NH",
            ZipCode = "03032"
        };

        public static readonly Address NH_NOTTINGHAM = new()
        {
            AddressLine1 = "14 Whites Grove Rd",
            AddressLine2 = string.Empty,
            City = "NOTTINGHAM",
            State = "NH",
            ZipCode = "03290"
        };

        public static readonly Address NJ = new()
        {
            AddressLine1 = "195 Firenze Street",
            AddressLine2 = string.Empty,
            City = "Northvale",
            State = "NJ",
            ZipCode = "07647"
        };

        // Bridgewater, NJ — Plymouth Rock returns rates here on QA (used by the CovMod dropdown case).
        public static readonly Address NJ_Bridgewater = new()
        {
            AddressLine1 = "245 Holland Ct",
            AddressLine2 = string.Empty,
            City = "Bridgewater",
            State = "NJ",
            ZipCode = "08807"
        };

        public static readonly Address NJ_NewEgypt = new ()
        {
            AddressLine1 = "40 Aqueduct Boulevard",
            AddressLine2 = string.Empty,
            City = "New Egypt",
            State = "NJ",
            ZipCode = "08533"
        };

        public static readonly Address NJ_LebanonBoro = new()
        {
            AddressLine1 = "133 Conover Ter",
            AddressLine2 = string.Empty,
            City = "Lebanon Boro",
            State = "NJ",
            ZipCode = "08833"
        };

        public static readonly Address NM = new()
        {
            AddressLine1 = "3895 Kodiak Rd NE",
            AddressLine2 = string.Empty,
            City = "Rio Rancho",
            State = "NM",
            ZipCode = "87144"
        };

        public static readonly Address NV = new()
        {
            AddressLine1 = "2091 S Virginia St",
            AddressLine2 = string.Empty,
            City = "Reno",
            State = "NV",
            ZipCode = "89502"
        };

        public static readonly Address NY_Averill_Park = new()
        {
            AddressLine1 = "14 Penny Lane",
            AddressLine2 = null,
            City = "Averill Park",
            State = "NY",
            ZipCode = "12018"
        };

        public static readonly Address NY_StatenIsland = new()
        {
            AddressLine1 = "259 Ilyssa Way",
            AddressLine2 = string.Empty,
            City = "Staten Island",
            State = "NY",
            ZipCode = "10312"
        };

        public static readonly Address AL_Huntsville = new()
        {
            AddressLine1 = "4906 Cotton Row NW",
            AddressLine2 = "1",
            City = "Huntsville",
            State = "AL",
            ZipCode = "35816",
            County = "MADISON"
        };

        public static readonly Address OH = new()
        {
            AddressLine1 = "318 Victory Rd",
            AddressLine2 = string.Empty,
            City = "Springfield",
            State = "OH",
            ZipCode = "45504"
        };

        public static readonly Address OH_2 = new()
        {
            AddressLine1 = "1127 Montego Dr",
            AddressLine2 = "",
            ZipCode = "45503",
            City = "Springfield",
            State = "OH"
        };
        public static readonly Address OH_Dayton = new()
        {
            AddressLine1 = "125 rosedale dr",
            AddressLine2 = "",
            ZipCode = "45402",
            City = "Dayton",
            State = "OH"
        };

        public static readonly Address OH_London = new()
        {
            AddressLine1 = "1117 Stratford Wy",
            AddressLine2 = "",
            ZipCode = "43140",
            City = "London",
            State = "OH",
        };

        public static readonly Address OK = new()
        {
            AddressLine1 = "9209 NW 147th St",
            AddressLine2 = string.Empty,
            City = "Yukon",
            State = "OK",
            ZipCode = "73099"
        };

        public static readonly Address OR = new()
        {
            AddressLine1 = "728 NE Apache Cir",
            AddressLine2 = string.Empty,
            City = "Redmond",
            State = "OR",
            ZipCode = "97756"
        };

        public static readonly Address PA = new()
        {
            AddressLine1 = "695 Pine Creek Ave",
            AddressLine2 = string.Empty,
            City = "Jersey Shore",
            State = "PA",
            ZipCode = "17740"
        };

        // Bensalem, PA — Plymouth Rock returns rates here on QA (used by the CovMod dropdown case).
        public static readonly Address PA_Bensalem = new()
        {
            AddressLine1 = "4423 Remo Crescent Rd",
            AddressLine2 = string.Empty,
            City = "Bensalem",
            State = "PA",
            ZipCode = "19020"
        };

        // Meadville, PA — in appetite for all coverage-modification (Custom package) carriers
        // (ASI, Homesite, PlymouthRock). Used by the CovMod persistence test.
        public static readonly Address PA_Meadville = new()
        {
            AddressLine1 = "10161 Williamson Road",
            AddressLine2 = string.Empty,
            City = "Meadville",
            State = "PA",
            ZipCode = "16335"
        };

        public static readonly Address RI = new()
        {
            AddressLine1 = "84 Messina St",
            AddressLine2 = string.Empty,
            City = "Providence",
            State = "RI",
            ZipCode = "02908"
        };

        public static readonly Address SC = new()
        {
            AddressLine1 = "1567 Swing Bridge Way",
            AddressLine2 = string.Empty,
            City = "Myrtle Beach",
            State = "SC",
            ZipCode = "29588"
        };

        public static readonly Address SD = new()
        {
            AddressLine1 = "7214 E Sierra Trl",
            AddressLine2 = string.Empty,
            City = "Sioux Falls",
            State = "SD",
            ZipCode = "57110"
        };

        public static readonly Address TN = new()
        {
            AddressLine1 = "5840 Sterling Oaks Dr",
            AddressLine2 = string.Empty,
            City = "Brentwood",
            State = "TN",
            ZipCode = "37027"
        };

        public static readonly Address TN_2 = new()
        {
            AddressLine1 = "653 Terrace Hill Rd",
            AddressLine2 = "653",
            City = "Mount Juliet",
            State = "TN",
            ZipCode = "37122"
        };

        public static readonly Address TX = new()
        {
            AddressLine1 = "12006 Nectar Grove Ct",
            AddressLine2 = string.Empty,
            City = "Houston",
            State = "TX",
            ZipCode = "77089"
        };

        public static readonly Address TX_Euless = new()
        {
            AddressLine1 = "811 BRIDLE DR",
            AddressLine2 = "",
            ZipCode = "76039",
            City = "EULESS",
            State = "TX"
        };

        public static readonly Address TX_Magnolia = new()
        {
            AddressLine1 = "2303 Timberbranch Ct",
            AddressLine2 = string.Empty,
            City = "Magnolia",
            State = "TX",
            ZipCode = "77355"
        };

        public static readonly Address TX_Crowley = new()
        {
            AddressLine1 = "4228 Old Timber Lane",
            AddressLine2 = string.Empty,
            City = "Crowley",
            State = "TX",
            ZipCode = "76036"
        };

        public static readonly Address TX_PLANO = new()
        {
            AddressLine1 = "3705 CROWNHILL DR",
            AddressLine2 = string.Empty,
            ZipCode = "75093",
            City = "Plano",
            State = "TX",
        };

        public static readonly Address TX_Thomaston = new()
        {
            AddressLine1 = "103 Wynncrest Ln",
            AddressLine2 = string.Empty,
            ZipCode = "30286",
            City = "Thomaston",
            State = "TX",
            //County = "UPSON"
        };

        public static readonly Address UT = new()
        {
            AddressLine1 = "4063 S 3700 W",
            AddressLine2 = string.Empty,
            City = "West Haven,",
            State = "UT",
            ZipCode = "84401"
        };

        public static readonly Address VA = new()
        {
            AddressLine1 = "5816 Godwin Blvd",
            AddressLine2 = string.Empty,
            City = "Suffolk",
            State = "VA",
            ZipCode = "23432"
        };

        public static readonly Address VT = new()
        {
            AddressLine1 = "376 Colchester Avenue",
            AddressLine2 = string.Empty,
            City = "Burlington",
            State = "VT",
            ZipCode = "05403"
        };

        public static readonly Address WA = new()
        {
            AddressLine1 = "17602 Crossing Drive E",
            AddressLine2 = string.Empty,
            City = "Puyallup",
            State = "WA",
            ZipCode = "98374"
        };

        public static readonly Address WI = new()
        {
            AddressLine1 = "403 Milky Way",
            AddressLine2 = string.Empty,
            City = "Madison",
            State = "WI",
            ZipCode = "53718"
        };

        public static readonly Address WV = new()
        {
            AddressLine1 = "74 Hosta Ct",
            AddressLine2 = string.Empty,
            City = "Martinsburg",
            State = "WV",
            ZipCode = "25401"
        };

        public static readonly Address WY = new()
        {
            AddressLine1 = "2173 Perkins Rd",
            AddressLine2 = string.Empty,
            City = "Thayne",
            State = "WY",
            ZipCode = "83127"
        };

        public static readonly Address MO_Ozark = new()
        {
            AddressLine1 = "513 E SOUTH ST",
            AddressLine2 = string.Empty,
            City = "OZARK",
            State = "MO",
            ZipCode = "65721"
        };

        public static readonly Address ND = new()
        {
            AddressLine1 = "3314 Kenner Loop",
            AddressLine2 = string.Empty,
            ZipCode = "58504",
            City = "Bismarck",
            State = "ND",
        };

        // Single source of truth for "does this AddressKey have data" — GetAddress and
        // ResolveStateKey both read this instead of the AddressKey enum directly. The enum can
        // have a member with no entry here (e.g. someone comments out a line while testing); if
        // ResolveStateKey checked the enum instead of this dictionary, it would resolve to a key
        // that then blows up in GetAddress instead of falling back to a city variant.
        //
        // Must stay declared after every Address field above: C# runs static field initializers
        // in declaration order, so referencing AL_Adger/AZ/etc. here before they're assigned
        // would capture nulls.
        private static readonly Dictionary<AddressKey, Address> _addressesByKey = new()
        {
            [AddressKey.AK_Anchorage] = AK_Anchorage,
            [AddressKey.AL_Adger] = AL_Adger,
            [AddressKey.AR] = AR,
            [AddressKey.AZ] = AZ,
            [AddressKey.AZ_Tucson] = AZ_Tucson,
            [AddressKey.CA] = CA,
            [AddressKey.CO] = CO,
            [AddressKey.CT] = CT,
            [AddressKey.CT_Cheshire] = CT_Cheshire,
            [AddressKey.DC] = DC,
            [AddressKey.DE] = DE,
            [AddressKey.FL] = FL,
            [AddressKey.FL_Bradenton] = FL_Bradenton,
            [AddressKey.GA] = GA,
            [AddressKey.GA_Athens] = GA_Athens,
            [AddressKey.HI] = HI,
            [AddressKey.IA] = IA,
            [AddressKey.ID] = ID,
            [AddressKey.IL_Peoria] = IL_Peoria,
            [AddressKey.IN] = IN,
            [AddressKey.KS] = KS,
            [AddressKey.KY] = KY,
            [AddressKey.LA_Gonzales] = LA_Gonzales,
            [AddressKey.MA] = MA,
            [AddressKey.MA_Westford] = MA_Westford,
            [AddressKey.MD] = MD,
            [AddressKey.ME] = ME,
            [AddressKey.MI] = MI,
            [AddressKey.MN] = MN,
            [AddressKey.MO] = MO,
            [AddressKey.MS] = MS,
            [AddressKey.MT] = MT,
            [AddressKey.NC] = NC,
            [AddressKey.NE] = NE,
            [AddressKey.NH] = NH,
            [AddressKey.NH_NOTTINGHAM] = NH_NOTTINGHAM,
            [AddressKey.NJ] = NJ,
            [AddressKey.NJ_Bridgewater] = NJ_Bridgewater,
            [AddressKey.NJ_NewEgypt] = NJ_NewEgypt,
            [AddressKey.NJ_LebanonBoro] = NJ_LebanonBoro,
            [AddressKey.NM] = NM,
            [AddressKey.NV] = NV,
            [AddressKey.NY_Averill_Park] = NY_Averill_Park,
            [AddressKey.OH] = OH,
            [AddressKey.OH_Dayton] = OH_Dayton,
            [AddressKey.OK] = OK,
            [AddressKey.OR] = OR,
            [AddressKey.PA] = PA,
            [AddressKey.PA_Bensalem] = PA_Bensalem,
            [AddressKey.PA_Meadville] = PA_Meadville,
            [AddressKey.RI] = RI,
            [AddressKey.SC] = SC,
            [AddressKey.SD] = SD,
            [AddressKey.TN] = TN,
            [AddressKey.TX_Magnolia] = TX_Magnolia,
            [AddressKey.TX_Crowley] = TX_Crowley,
            [AddressKey.UT] = UT,
            [AddressKey.VA] = VA,
            [AddressKey.VT] = VT,
            [AddressKey.WA] = WA,
            [AddressKey.WI] = WI,
            [AddressKey.WV] = WV,
            [AddressKey.WY] = WY,
        };

        public static Address GetAddress(AddressKey key) =>
            _addressesByKey.TryGetValue(key, out var address)
                ? address
                : throw new ArgumentOutOfRangeException(nameof(key), key, $"No address mapped for {key}");

        // Resolves an INJECTED_PS_STATE-style input to its AddressKey:
        //   - a bare state code (e.g. "AL") that matches an AddressKey exactly -> that key.
        //   - a bare state code with no plain entry -> the alphabetically first (OrdinalIgnoreCase)
        //     "{State}_City" variant (e.g. "AL" -> AL_Adger, since AL_Adger < AL_Huntsville).
        //   - a full AddressKey name (e.g. "TX_Crowley") -> that exact key.
        // Deterministic on purpose: picking "first found" from a Dictionary would be an
        // implementation-detail ordering; alphabetical-first is stable across runs and machines.
        // Checked against _addressesByKey (not Enum.GetValues) so a defined-but-unmapped enum
        // member (data removed/commented out) correctly falls through to a city variant instead
        // of resolving to a key that has no address.
        public static AddressKey ResolveStateKey(string stateCode)
        {
            if (Enum.TryParse<AddressKey>(stateCode, ignoreCase: true, out var exactKey) && _addressesByKey.ContainsKey(exactKey))
                return exactKey;

            var prefix = $"{stateCode}_";
            var match = _addressesByKey.Keys
                .Where(candidate => candidate.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(candidate => candidate.ToString(), StringComparer.OrdinalIgnoreCase)
                .Cast<AddressKey?>()
                .FirstOrDefault();

            if (match.HasValue)
                return match.Value;

            throw new ArgumentException(
                $"No AddressKey found for state '{stateCode}' — checked an exact match and '{stateCode}_*' city variants. " +
                "Add an AddressKey/AddressData entry for this state before using it.");
        }

        /// <summary>
        /// Every valid <c>INJECTED_PS_STATE</c> input: every mapped <see cref="AddressKey"/> name
        /// PLUS every distinct bare state code derivable from those names (the prefix before '_',
        /// or the whole name when it has no '_'). Sorted, distinct, case-preserving.
        /// </summary>
        /// <remarks>
        /// CROSS-REPO CONTRACT: the orchestrator's discovery tool invokes this method via reflection
        /// (type <c>Bolt.Automation.TestDataProvider.TestData.CommonTestData.AddressData</c>, method
        /// <c>SupportedStateInputs</c>) to populate the PS wizard's states multi-select. Do not
        /// rename the type or this method without updating the discovery tool.
        /// </remarks>
        public static IReadOnlyList<string> SupportedStateInputs()
        {
            var keys = _addressesByKey.Keys.Select(k => k.ToString());
            var bareCodes = keys.Select(k =>
            {
                var separatorIndex = k.IndexOf('_');
                return separatorIndex < 0 ? k : k[..separatorIndex];
            });

            return keys.Concat(bareCodes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

}

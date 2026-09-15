using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStart;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData.QuoteStartTestData;
using static Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData.QuoteStartTestData.QuoteStartRequestTestData;

namespace Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider
{
    /// <summary>
    /// Provider for creating QuoteStart requests with different prefill configurations
    /// </summary>
    public static class QuoteStartPrefillDataProvider
    {
        public enum PrefillScenario
        {
            Standard,
            Full,
            NewHomeowner,
            ExperiencedHomeowner,
            RentalProperty
        }

        // Scenario helpers (kept for ergonomics) ----------------------------------
        public static QuoteStartRequestModel GetStandardPrefillData(AddressModel? address = null) =>
            GetScenarioBasedQuoteStartRequest(PrefillScenario.Standard, address);
        public static QuoteStartRequestModel GetFullPrefillData(AddressModel? address = null) =>
            GetScenarioBasedQuoteStartRequest(PrefillScenario.Full, address);

        /// <summary>
        /// Full prefill data configured for an HO6 (Condo) quote with HighRiseCondo defaulted to false.
        /// </summary>
        public static QuoteStartRequestModel GetHO6PrefillData(
            AddressModel? address = null,
            Action<PrefillDataModel>? customizePrefill = null)
        {
            return GetScenarioBasedQuoteStartRequest(
                PrefillScenario.Full,
                address,
                configureRequest: req => req.LOBCd = "Condo",
                customizePrefill: p =>
                {
                    p.PolicyData.PLHighRiseCondo = false;
                    customizePrefill?.Invoke(p);
                });
        }
        /// <summary>
        /// Full prefill configured for a manufactured-home (MFH) quote: LOBCd = "MFH" plus the
        /// manufactured-home property answers, so the HQX short flow reaches Rates without walking
        /// the manufactured-home interview by hand. Values mirror the canonical MFH HomeDetails.
        /// </summary>
        public static QuoteStartRequestModel GetMFHPrefillData(
            AddressModel? address = null,
            Action<PrefillDataModel>? customizePrefill = null)
        {
            return GetScenarioBasedQuoteStartRequest(
                PrefillScenario.Full,
                address,
                configureRequest: req => req.LOBCd = nameof(Lobs.MFH),
                customizePrefill: p =>
                {
                    p.PolicyData.PLTypeOfDwelling = "ManufacturedHome";
                    p.PolicyData.MHArchitectureStyle = "SingleWide";
                    p.PolicyData.HomeLength = 60;
                    p.PolicyData.HomeWidth = 14;
                    p.PolicyData.ModularHome = false;
                    p.PolicyData.HomeTiedDown = true;
                    p.PolicyData.IsLocatedInPark = false;
                    p.PolicyData.UtilityHookup365 = true;
                    p.PolicyData.PLSquareFootage = 840;
                    p.PolicyData.PLConstructionType = "Frame";
                    p.PolicyData.TypeOfFoundation = "Slab";
                    p.PolicyData.PlumbingType = "EntirelyCopper";
                    p.PolicyData.RoofType = "ARCHITECTURAL_SHINGLES";
                    p.PolicyData.MitRoofShape = "B_Gable";
                    p.PolicyData.PLNumberOfStories = "One";
                    p.PolicyData.FullBathNum = 2;
                    p.PolicyData.HalfBathNum = 0;
                    p.PolicyData.BuiltOnSlope = false;
                    p.PolicyData.NumberofAcres = 1;
                    p.PolicyData.PL_AdditionalStructures_Garage = false;
                    customizePrefill?.Invoke(p);
                });
        }

        /// <summary>
        /// Full prefill for an HO3 (Home) quote whose home was built <paramref name="ageInYears"/>
        /// years ago. Used by the "utilities replaced" question feature, which is presented only in
        /// FL / HO3 when the home's yearBuilt is 20-49 years old (feature flag ba_utilities-replaced-question).
        /// Defaults to 20 years old — the boundary the TC exercises.
        /// </summary>
        public static QuoteStartRequestModel GetHomeBuiltAgePrefillData(
            AddressModel? address = null,
            int ageInYears = 20,
            Action<PrefillDataModel>? customizePrefill = null)
        {
            return GetScenarioBasedQuoteStartRequest(
                PrefillScenario.Full,
                address,
                customizePrefill: p =>
                {
                    p.PolicyData.PLYearBuilt = DateTime.Now.Year - ageInYears;
                    customizePrefill?.Invoke(p);
                });
        }

        public static QuoteStartRequestModel GetNewHomeownerPrefillData(AddressModel? address = null) =>
            GetScenarioBasedQuoteStartRequest(PrefillScenario.NewHomeowner, address);
        public static QuoteStartRequestModel GetExperiencedHomeownerPrefillData(AddressModel? address = null) =>
            GetScenarioBasedQuoteStartRequest(PrefillScenario.ExperiencedHomeowner, address);
        public static QuoteStartRequestModel GetRentalPropertyPrefillData(AddressModel? address = null) =>
            GetScenarioBasedQuoteStartRequest(PrefillScenario.RentalProperty, address);

        // Custom strongly-typed prefill (build from empty) ------------------------
        public static QuoteStartRequestModel GetCustomPrefillData(
            AddressModel? address = null,
            Action<PrefillDataModel>? configurePrefill = null)
        {
            var model = new PrefillDataModel();
            configurePrefill?.Invoke(model);
            var request = CreateBase(address);
            request.PrefillData = PrefillMapper.ToDictionary(model.PolicyData, model.CustomFields);
            return request;
        }

        // Full control: request first, with optional strongly typed prefill -------
        public static QuoteStartRequestModel GetCustomQuoteStartRequest(
            Action<QuoteStartRequestModel>? configureRequest = null,
            Action<PrefillDataModel>? configurePrefill = null)
        {
            var request = CreateBase();
            if (configurePrefill != null)
            {
                var model = new PrefillDataModel();
                configurePrefill(model);
                request.PrefillData = PrefillMapper.ToDictionary(model.PolicyData, model.CustomFields);
            }
            configureRequest?.Invoke(request);
            return request;
        }

        // Overload: provide pre-built policy/custom objects -----------------------
        public static QuoteStartRequestModel GetCustomQuoteStartRequest(
            AddressModel? address = null,
            PolicyDataPrefill? policyData = null,
            CustomFieldsPrefill? customFields = null,
            Action<QuoteStartRequestModel>? configureRequest = null)
        {
            var request = CreateBase(address);
            request.PrefillData = PrefillMapper.ToDictionary(policyData ?? new PolicyDataPrefill(), customFields ?? new CustomFieldsPrefill());
            configureRequest?.Invoke(request);
            return request;
        }

        // Scenario with hooks -----------------------------------------------------
        public static QuoteStartRequestModel GetScenarioBasedQuoteStartRequest(
            PrefillScenario scenario,
            AddressModel? address = null,
            Action<QuoteStartRequestModel>? configureRequest = null,
            Action<PrefillDataModel>? customizePrefill = null)
        {
            var (policy, custom) = GetScenarioPrefillData(scenario);
            if (customizePrefill != null)
            {
                var temp = new PrefillDataModel { PolicyData = policy, CustomFields = custom };
                customizePrefill(temp);
                policy = temp.PolicyData;
                custom = temp.CustomFields;
            }
            var request = CreateBase(address);
            request.PrefillData = PrefillMapper.ToDictionary(policy, custom);
            configureRequest?.Invoke(request);
            return request;
        }

        // Internal scenario dispatcher --------------------------------------------
        private static (PolicyDataPrefill, CustomFieldsPrefill) GetScenarioPrefillData(PrefillScenario scenario) =>
            scenario switch
            {
                PrefillScenario.Standard => (PrefillTestData.StandardPolicyData, PrefillTestData.StandardCustomFields),
                PrefillScenario.Full => (PrefillTestData.FullPolicyData, PrefillTestData.FullCustomFields),
                PrefillScenario.NewHomeowner => (PrefillTestData.NewHomeownerPolicyData, PrefillTestData.StandardCustomFields),
                PrefillScenario.ExperiencedHomeowner => (PrefillTestData.ExperiencedHomeownerPolicyData, PrefillTestData.StandardCustomFields),
                PrefillScenario.RentalProperty => (PrefillTestData.RentalPropertyPolicyData, PrefillTestData.StandardCustomFields),
                _ => (PrefillTestData.StandardPolicyData, PrefillTestData.StandardCustomFields)
            };

        // Base request factory ----------------------------------------------------
        internal static QuoteStartRequestModel CreateBase(AddressModel? address = null) => new()
        {
            ClientDt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            SPName = nameof(SPNames.BOLTAPI),
            ProxyClientName = nameof(DeviceIndicator.Desktop),
            ChannelIndicator = nameof(ChannelIndicators.Direct),
            RedirectURL = "https://www.progressive.com/",
            LOBCd = nameof(Lobs.Home),
            PropertyAddr = address ?? Addresses.Mapped_OH,
            ApplicantDetails = Applicants.Credipulse,
            PrefillData = new Dictionary<string, string>(),
            TransType = nameof(TransitionType.QuoteStart),
            Org = nameof(Orgs.ProgressivePL),
            SourceName = "Progressive.com"
        };
    }

    /// <summary>
    /// Fluent builder for QuoteStartRequestModel with deferred dictionary materialization
    /// </summary>
    public sealed class QuoteStartRequestBuilder
    {
        private QuoteStartPrefillDataProvider.PrefillScenario? _scenario;
        private AddressModel? _address;
        private readonly PolicyDataPrefill _policy = new();
        private readonly CustomFieldsPrefill _custom = new();
        private readonly List<Action<QuoteStartRequestModel>> _requestMutators = new();
        private bool _replacePrefill; // if user calls ReplacePrefill()

        public static QuoteStartRequestBuilder Create() => new();

        public QuoteStartRequestBuilder WithScenario(QuoteStartPrefillDataProvider.PrefillScenario scenario)
        {
            _scenario = scenario;
            return this;
        }

        public QuoteStartRequestBuilder WithAddress(AddressModel address)
        {
            _address = address;
            return this;
        }

        public QuoteStartRequestBuilder ConfigurePolicy(Action<PolicyDataPrefill> cfg)
        {
            cfg(_policy);
            return this;
        }

        public QuoteStartRequestBuilder ConfigureCustomFields(Action<CustomFieldsPrefill> cfg)
        {
            cfg(_custom);
            return this;
        }

        public QuoteStartRequestBuilder ConfigureRequest(Action<QuoteStartRequestModel> cfg)
        {
            _requestMutators.Add(cfg);
            return this;
        }

        /// <summary>
        /// Indicates that scenario baseline prefill should be ignored; only explicitly configured values used.
        /// </summary>
        public QuoteStartRequestBuilder ReplacePrefill()
        {
            _replacePrefill = true;
            return this;
        }

        public QuoteStartRequestBuilder WithApplicant(ApplicantDetails applicant)
        {
            _requestMutators.Add(r => r.ApplicantDetails = applicant);
            return this;
        }

        public QuoteStartRequestModel Build()
        {
            // Start base
            var request = QuoteStartPrefillDataProvider.CreateBase(_address);

            PolicyDataPrefill finalPolicy;
            CustomFieldsPrefill finalCustom;

            if (_scenario.HasValue && !_replacePrefill)
            {
                var (scenarioPolicy, scenarioCustom) = GetScenarioPrefill(_scenario.Value);
                // merge: only overwrite scenario when user provided non-null values
                MergeScenarioIntoWorking(_policy, scenarioPolicy);
                MergeScenarioIntoWorking(_custom, scenarioCustom);
                finalPolicy = _policy;
                finalCustom = _custom;
            }
            else if (_scenario.HasValue && _replacePrefill)
            {
                // ignore scenario baseline completely, use only modified values
                finalPolicy = _policy;
                finalCustom = _custom;
            }
            else
            {
                finalPolicy = _policy;
                finalCustom = _custom;
            }

            request.PrefillData = PrefillMapper.ToDictionary(finalPolicy, finalCustom);

            foreach (var m in _requestMutators)
                m(request);

            // Ensure ApplicantDetails included if not explicitly set
            request.ApplicantDetails ??= Applicants.Credipulse;

            return request;
        }

        private static (PolicyDataPrefill, CustomFieldsPrefill) GetScenarioPrefill(QuoteStartPrefillDataProvider.PrefillScenario scenario) =>
            scenario switch
            {
                QuoteStartPrefillDataProvider.PrefillScenario.Standard => (PrefillTestData.StandardPolicyData, PrefillTestData.StandardCustomFields),
                QuoteStartPrefillDataProvider.PrefillScenario.Full => (PrefillTestData.FullPolicyData, PrefillTestData.FullCustomFields),
                QuoteStartPrefillDataProvider.PrefillScenario.NewHomeowner => (PrefillTestData.NewHomeownerPolicyData, PrefillTestData.StandardCustomFields),
                QuoteStartPrefillDataProvider.PrefillScenario.ExperiencedHomeowner => (PrefillTestData.ExperiencedHomeownerPolicyData, PrefillTestData.StandardCustomFields),
                QuoteStartPrefillDataProvider.PrefillScenario.RentalProperty => (PrefillTestData.RentalPropertyPolicyData, PrefillTestData.StandardCustomFields),
                _ => (PrefillTestData.StandardPolicyData, PrefillTestData.StandardCustomFields)
            };

        private static void MergeScenarioIntoWorking<T>(T working, T scenarioObj) where T : class
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                var userValue = prop.GetValue(working);
                if (userValue != null) continue; // user already set
                var scenarioValue = prop.GetValue(scenarioObj);
                if (scenarioValue != null)
                    prop.SetValue(working, scenarioValue);
            }
        }
    }
}
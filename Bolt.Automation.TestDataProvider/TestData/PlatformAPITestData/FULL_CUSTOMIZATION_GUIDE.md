# QuoteStart Request Full Customization Guide

This guide demonstrates how to use the enhanced `QuoteStartPrefillDataProvider` to customize **any property** of the `QuoteStartRequestModel`, not just address and prefill data.

## ??? Full Customization Options

The `QuoteStartRequestModel` has many properties you can customize:

### **Core Request Properties**
```csharp
// Basic request metadata
req.ClientDt = "custom-timestamp";
req.SPName = "CustomSP";
req.TransType = "CustomTransType";
req.Org = "CustomOrg";
req.SourceName = "CustomSource";
req.SourceType = "TestAutomation";

// Device and channel configuration
req.ProxyClientName = DeviceIndicator.Mobile.ToString();
req.ChannelIndicator = ChannelIndicators.Direct.ToString();
req.RedirectURL = "https://custom-redirect.com";
req.LOBCd = Lobs.Home.ToString();
```

### **Address Configuration**
```csharp
// Property address
req.PropertyAddr = new AddressModel
{
    Addr1 = "123 Custom St",
    Addr2 = "Apt 4B", 
    City = "Custom City",
    StateProvCd = "CA",
    PostalCode = "90210"
};

// Separate mailing address
req.MailingAddress = new AddressModel
{
    Addr1 = "PO Box 999",
    City = "Different City",
    StateProvCd = "NY", 
    PostalCode = "10001"
};
```

### **Applicant Details**
```csharp
// Primary applicant
req.ApplicantDetails = new ApplicantDetails
{
    GivenName = "CustomFirst",
    Surname = "CustomLast",
    EmailAddr = "custom@test.com",
    PhoneNumber = "555-123-4567",
    BirthDt = DateOnly.Parse("1985-06-15"),
    MaritalStatusCd = "S"
};

// Co-applicant (if needed)
req.CoApplicantDetails = new ApplicantDetails
{
    GivenName = "CoFirst",
    Surname = "CoLast",
    EmailAddr = "co@test.com"
};
```

### **External Integration IDs**
```csharp
// For integration and tracking
req.AgentExternalId = "AGENT123";
req.GroupExternalId = "GROUP456"; 
req.AccountExternalId = "ACCT789";
req.PartnerPolicyNumber = "POLICY12345";
req.ConsumerLine = "TestLine";

// Carrier-specific IDs
req.CarrierUserAgentId = Guid.NewGuid();
req.CarrierUserConsumerAgentId = Guid.NewGuid();
```

### **Prefill Data Options**
```csharp
// Standard prefill (policy data + custom fields)
req.PrefillData = customPrefillDictionary;

// Third-party prefill data 
req.ThirdPartyPrefillData = new Dictionary<string, string>
{
    ["ThirdParty.Source"] = "ExternalProvider",
    ["ThirdParty.Version"] = "2.0",
    ["ThirdParty.Timestamp"] = DateTime.UtcNow.ToString()
};
```

## ?? Usage Patterns

### **Pattern 1: Full Control (Everything Custom)**
```csharp
var request = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
    configureRequest: req =>
    {
        // Customize EVERYTHING
        req.PropertyAddr = customAddress;
        req.MailingAddress = differentMailingAddress;
        req.ApplicantDetails = customApplicant;
        req.CoApplicantDetails = coApplicant;
        req.RedirectURL = "https://custom.com";
        req.SourceName = "MyTestSuite";
        req.ProxyClientName = DeviceIndicator.Mobile.ToString();
        req.AgentExternalId = "TEST_AGENT";
        req.PartnerPolicyNumber = "TEST_POLICY_123";
        req.ThirdPartyPrefillData = customThirdPartyData;
    },
    configurePrefill: prefill =>
    {
        // Also customize prefill data
        prefill.PolicyData.YearsAtAddress = 10;
        prefill.CustomFields.ABTest_new_d2c = "1917A";
    });
```

### **Pattern 2: Scenario + Selective Customization**
```csharp
var request = QuoteStartPrefillDataProvider.GetScenarioBasedQuoteStartRequest(
    scenario: QuoteStartPrefillDataProvider.PrefillScenario.ExperiencedHomeowner,
    address: myAddress,
    configureRequest: req =>
    {
        // Only customize what you need
        req.SourceName = "ExperiencedHomeownerTest";
        req.AgentExternalId = "EXPERIENCED_AGENT";
        req.ProxyClientName = DeviceIndicator.Tablet.ToString();
    },
    customizePrefill: prefill =>
    {
        // Tweak the scenario data slightly
        prefill.PolicyData.NumberOfClaimsHistory = 1; // Override default
        prefill.PolicyData.CurrentPersonalHomeownerCarrier = "MyCarrier";
    });
```

### **Pattern 3: Address + Prefill + Minimal Request Changes**
```csharp
var request = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
    address: Addresses.AZ,
    policyData: myCustomPolicyData,
    customFields: myCustomFields,
    configureRequest: req =>
    {
        // Just change a few request properties  
        req.SourceName = "MinimalCustomization";
        req.PartnerPolicyNumber = "PARTNER_001";
    });
```

### **Pattern 4: Device/Channel Testing**
```csharp
// Test different device types
var mobileRequest = QuoteStartPrefillDataProvider.GetFullPrefillData(address);
mobileRequest.ProxyClientName = DeviceIndicator.Mobile.ToString();

var tabletRequest = QuoteStartPrefillDataProvider.GetFullPrefillData(address); 
tabletRequest.ProxyClientName = DeviceIndicator.Tablet.ToString();

var desktopRequest = QuoteStartPrefillDataProvider.GetFullPrefillData(address);
desktopRequest.ProxyClientName = DeviceIndicator.Desktop.ToString();
```

### **Pattern 5: Integration Testing Setup**
```csharp
var request = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
    configureRequest: req =>
    {
        // Setup for integration testing
        req.SourceName = "IntegrationTest";
        req.SourceType = "Automation";
        req.AgentExternalId = Environment.GetEnvironmentVariable("TEST_AGENT_ID");
        req.GroupExternalId = Environment.GetEnvironmentVariable("TEST_GROUP_ID");
        req.RedirectURL = configurationManager.GetValue("TestRedirectUrl");
        
        // Custom applicant for consistent testing
        req.ApplicantDetails = new ApplicantDetails
        {
            GivenName = "Integration",
            Surname = "TestUser",
            EmailAddr = $"test-{DateTime.Now.Ticks}@automation.com",
            PhoneNumber = "555-000-0001",
            BirthDt = DateOnly.Parse("1980-01-01"),
            MaritalStatusCd = "S"
        };
    });
```

## ?? Real-World Examples

### **Testing Different User Scenarios**
```csharp
// New customer (no history)
var newCustomer = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
    configureRequest: req => req.SourceName = "NewCustomer",
    configurePrefill: prefill =>
    {
        prefill.PolicyData.PriorInsuranceProperty = false;
        prefill.PolicyData.NumberOfClaimsHistory = 0;
        prefill.PolicyData.YearsAtAddress = 1;
    });

// Existing customer switching carriers  
var switchingCustomer = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
    configureRequest: req => 
    {
        req.SourceName = "CarrierSwitch";
        req.PartnerPolicyNumber = "EXISTING_POLICY_789";
    },
    configurePrefill: prefill =>
    {
        prefill.PolicyData.PriorInsuranceProperty = true;
        prefill.PolicyData.CurrentPersonalHomeownerCarrier = "OldCarrier";
        prefill.PolicyData.YearsWithPriorCarrierHome = 5;
    });

// VIP customer (special handling)
var vipCustomer = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
    configureRequest: req =>
    {
        req.SourceName = "VIPCustomer";
        req.AccountExternalId = "VIP_ACCOUNT_001";
        req.ConsumerLine = "VIP";
    });
```

### **A/B Testing Configuration**
```csharp
// Variant A
var variantA = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
    configurePrefill: prefill =>
    {
        prefill.CustomFields.ABTest_new_d2c = "1917A";
        prefill.CustomFields.ABTest_d2c_long_form = "1960A";
    });

// Variant B  
var variantB = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
    configurePrefill: prefill =>
    {
        prefill.CustomFields.ABTest_new_d2c = "1917B";
        prefill.CustomFields.ABTest_d2c_long_form = "1960B";
    });
```

### **Multi-State Testing**
```csharp
var states = new[] { "AZ", "TX", "OH", "FL" };
var requests = states.Select(state => 
    QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
        address: new AddressModel
        {
            Addr1 = "123 Test St",
            City = "Test City", 
            StateProvCd = state,
            PostalCode = "12345"
        },
        configureRequest: req => req.SourceName = $"MultiState_{state}"));
```

## ? Benefits of Full Customization

1. **Comprehensive Testing** - Test any combination of request properties
2. **Real-World Scenarios** - Model actual customer situations accurately  
3. **Integration Testing** - Configure external IDs and tracking properly
4. **A/B Testing Support** - Easy to create test variants
5. **Device Testing** - Test mobile, tablet, desktop experiences
6. **Environment Flexibility** - Adapt to different test environments
7. **Data Isolation** - Create unique test data per test run

This approach gives you **complete control** over every aspect of the QuoteStart request, enabling comprehensive testing scenarios that match real-world usage patterns! ??
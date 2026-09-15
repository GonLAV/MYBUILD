# How to Add a New Project to the Enhanced Automation Framework

This guide describes the steps required to add a new project (e.g., `XYZ`) to the modernized automation framework with the latest architectural improvements.

---

## **Framework Overview**

The automation framework now features:
- **Enhanced PageHelper** with `InteractWithField()` clean syntax
- **ProjectContextManager** for intelligent project detection
- **Modernized FieldRegistry** with automatic initialization
- **Streamlined ProjectRegistry** with `AdditionalNamespaces` support
- **Button field type** support for clickable elements
- **FlowDefaultsProvider** for automatic default data generation

---

## 1. Update the `FrontEndType` Enum

Add your new project type to the `FrontEndType` enum in `Bolt.Automation.Common/Enums/FrontEndType.cs`:
namespace Bolt.Automation.Common.Enums
{
    public enum FrontEndType
    {
        D2C,
        ADBX,
        PartnerPortal,
        HQXConsumer,
        HQXAgent,
        Interview,
        XYZ  // <-- Add this line
    }
}
---

## 2. Create the Project Folder Structure

Create a new folder for your project:Bolt.Automation.FrontEnds/Projects/XYZ/
Add the following subfolders:
- `FormData/` - Field definitions and form data helpers
- `Pages/` - Page object classes
- `Flows/` - Flow definitions for automated testing
- `Base/` - Base classes for the project
- `Popups/` - Popup/modal classes (optional)

---

## 3. Create the Enhanced Field Registry Class

In `FormData/`, create `FieldRegistryXYZ.cs` using the modern approach:
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Formdata;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.XYZ.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.XYZ.FormData
{
    public static class FieldRegistryXYZ
    {
        [FieldRegistry]
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            // Input field example using FieldNames constants
            [OnlineAddress] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input[name='address']", "#addressField" },
                FieldType = UIFieldType.Input,
                DefaultValue = "123 Main St, City, State 12345",
                Pages = new HashSet<Type> { typeof(XYZ_AddressPage) },
                Required = true
            },
            
            // Dropdown field example
            [Gender] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='gender']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Male",
                Pages = new HashSet<Type> { typeof(XYZ_PersonalInfoPage) }
            },
            
            // Checkbox field example
            ["AgreeToTerms"] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#agreeCheckbox",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = new HashSet<Type> { typeof(XYZ_PersonalInfoPage) },
                Required = true
            },
            
            // Button field example (for clickable elements)
            ["ContinueButton"] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { 
                    "button[data-automation='continue']",
                    "button[aria-label='Continue']",
                    ".continue-btn"
                },
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = new HashSet<Type> { typeof(XYZ_AddressPage), typeof(XYZ_PersonalInfoPage) }
            }
        };

        // Auto-register with FieldRegistryProvider
        static FieldRegistryXYZ() => FieldRegistryProvider.Register(FrontEndType.XYZ, Fields);
    }
}
---

## 4. Create FlowType Enum

In `Flows/`, create `FlowType.cs` for project-specific flow types:
namespace Bolt.Automation.FrontEnds.Projects.XYZ.Flows
{
    public enum FlowType
    {
        XYZAutoFlow,
        XYZHomeFlow
    }
}
---

## 5. Create the Enhanced Flows Class

In `Flows/`, create `Flows.cs` with modern flow definitions:
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.InterviewFlows;
using Bolt.Automation.FrontEnds.Projects.XYZ.Pages;

namespace Bolt.Automation.FrontEnds.Projects.XYZ.Flows
{
    public static class Flows
    {
        // Environment URLs for the project
        private static readonly Dictionary<string, string> XyzEnvironmentUrls = new()
        {
            ["QA"] = "https://xyz-qa.example.com",
            ["UAT"] = "https://xyz-uat.example.com",
            ["PROD"] = "https://xyz-prod.example.com"
        };

        private static FlowsHelpers CreateXYZFlow(FlowType flowType, List<Type> pages, Dictionary<string, object> defaultData)
        {
            var flow = new FlowsHelpers
            {
                FlowType = flowType,
                EnvironmentUrls = new Dictionary<string, string>(XyzEnvironmentUrls),
                Pages = pages,
                DefaultData = defaultData
            };

            // Register the flow
            FlowsHelpers.FlowRegistry.Register(flowType, flow);
            return flow;
        }

        // Use [FlowInitializer] attribute for automatic registration
        [FlowInitializer("XYZ Auto Insurance Flow")]
        public static FlowsHelpers CreateAutoFlow()
        {
            var pages = new List<Type>
            {
                typeof(XYZ_AddressPage),
                typeof(XYZ_PersonalInfoPage),
                typeof(XYZ_RatesPage)
            };

            return CreateXYZFlow(
                FlowType.XYZAutoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.XYZ, LobType.Auto)
            );
        }

        [FlowInitializer("XYZ Home Insurance Flow")]
        public static FlowsHelpers CreateHomeFlow()
        {
            var pages = new List<Type>
            {
                typeof(XYZ_AddressPage),
                typeof(XYZ_PersonalInfoPage),
                typeof(XYZ_RatesPage)
            };

            return CreateXYZFlow(
                FlowType.XYZHomeFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.XYZ, LobType.Home)
            );
        }
    }
}
---

## 6. Register the Project in ProjectRegistry

In `ProjectRegistry.cs`, add a new entry to the `_projects` dictionary:
[FrontEndType.XYZ] = new ProjectInfo
{
    Type = FrontEndType.XYZ,
    FormDataNamespace = "Bolt.Automation.FrontEnds.Projects.XYZ.FormData",
    PagesNamespace = "Bolt.Automation.FrontEnds.Projects.XYZ.Pages",
    AdditionalNamespaces = new[] { 
        "Bolt.Automation.FrontEnds.Projects.XYZ.Popups", 
        "Bolt.Automation.FrontEnds.Projects.XYZ.Flows" 
    },
    UrlIdentifiers = new[] { "xyz" },  // URL detection patterns
    FieldRegistryType = typeof(Projects.XYZ.FormData.FieldRegistryXYZ),
    FlowsType = typeof(Projects.XYZ.Flows.Flows)
}
**Note:** The `AdditionalNamespaces` property supports detection from Popups and Flows namespaces.

---

## 7. Create Base Class

In `Base/`, create `XYZBase.cs`:
using Bolt.Automation.FrontEnds.Formdata;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.XYZ.FormData;
using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.XYZ.Base
{
    public abstract class XYZBase : IInterview
    {
        protected readonly IPageHelper PageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        
        public IPage Page => BrowserManager.GetCurrentTab()!;
        protected abstract string PageIdentifier { get; }
        protected abstract string PageName { get; }

        // Expose the XYZ field registry for all derived pages
        protected virtual Dictionary<string, UIElement> FieldRegistry => FieldRegistryXYZ.Fields;

        // Helper for easy field access
        protected UIElement Field(string fieldName) => FieldRegistry[fieldName];

        // Helper for field access with value - avoids Field(name)[value] syntax
        protected UIElement FieldWithValue(string fieldName, string value) => FieldRegistry[fieldName][value];

        protected XYZBase(IBrowserManager browserManager, IPageHelper pageHelper, bool validatePageReady = true, IAutomationLogger? logger = null)
        {
            BrowserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
            PageHelper = pageHelper ?? throw new ArgumentNullException(nameof(pageHelper));
            _logger = logger;

            if (validatePageReady)
            {
                ValidatePageReady().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public virtual async Task ValidatePageReady()
        {
            await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName);
        }

        public abstract Task FillForm(Dictionary<string, string> formData);
    }
}
---

## 8. Implement Enhanced Page Classes

In the `Pages/` folder, create page classes using the new clean syntax:
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.XYZ.Base;

namespace Bolt.Automation.FrontEnds.Projects.XYZ.Pages
{
    public class XYZ_AddressPage : XYZBase
    {
        protected override string PageIdentifier => "address";
        protected override string PageName => "Address Page";

        public XYZ_AddressPage(IBrowserManager browserManager, IPageHelper pageHelper) 
            : base(browserManager, pageHelper) { }

        // Clean syntax: Use InteractWithField for automatic detection
        public async Task SetAddress(string address)
        {
            // Ultra-clean: automatic project detection + field registry lookup
            await PageHelper.InteractWithField(FieldNames.OnlineAddress, address);
        }

        public async Task ClickContinue()
        {
            // Button fields automatically click
            await PageHelper.InteractWithField("ContinueButton");
        }

        public override async Task FillForm(Dictionary<string, string> formData)
        {
            // Enhanced form filling with automatic field detection
            var pageSpecificData = FormDataHelper.GetFieldsForPage(this, formData);
            await Page.FillRelevantFields(pageSpecificData);
        }
    }
}
---

## 9. Add Tests Using Enhanced Syntax

Create tests that demonstrate the new clean syntax:
[Fact]
[Tenant(Tenant.BOLTAG)]
public async Task TestXYZFlow_UsingEnhancedSyntax()
{
    await BrowserManager.NavigateAsync("https://xyz-qa.example.com");
    
    // Ultra-clean syntax: Automatic project detection
    await _pageHelper!.InteractWithField(FieldNames.OnlineAddress);
    await _pageHelper.FillField(FieldNames.OnlineAddress, "123 Test St");
    await _pageHelper.InteractWithField("AgreeToTerms", "Yes");  // Checkbox
    await _pageHelper.InteractWithField("ContinueButton");       // Button click
    
    // Page-specific actions
    var personalInfoPage = PageFactory.CreatePage<XYZ_PersonalInfoPage>();
    await _pageHelper.FillField("Gender", "Male");              // Dropdown
    await personalInfoPage.ClickContinue();
}

[Fact]
[Tenant(Tenant.BOLTAG)]
public async Task TestXYZFlow_UsingExecutor()
{
    var formData = new Dictionary<string, string>
    {
        [FieldNames.OnlineAddress] = "123 Test Street",
        [FieldNames.FirstName] = "TestXYZ"
    };

    var resultPage = await Executor.Execute<XYZ_AddressPage, XYZ_RatesPage>(
        FlowType.XYZAutoFlow,
        formData,
        fillForms: true
    );
}
---

## **Summary Table**

| Step | What to Do | Modern Features |
|------|------------|-----------------|
| 1 | Add to enum | `FrontEndType.XYZ` |
| 2 | Create folders | Standard project structure |
| 3 | FieldRegistry | `[FieldRegistry]` + **FrontEndType enum** + **Button support** |
| 4 | FlowType enum | Project-specific flow type definitions |
| 5 | Flows class | **FlowDefaultsProvider.GetFlowDefaults()** + **[FlowInitializer]** |
| 6 | Register in ProjectRegistry | **AdditionalNamespaces** support |
| 7 | Create Base class | Project-specific base class with logging |
| 8 | Add pages | **PageHelper.InteractWithField()** clean syntax |
| 9 | Add tests | **Ultra-clean test syntax** + **Executor support** |

---

## **Available Field Types**

Your field registry can use these enhanced field types:
public enum UIFieldType
{
    Input,        // Text inputs, textareas
    Dropdown,     // Select elements, ng-select
    Checkbox,     // Checkboxes (automatically checks/unchecks)
    Radio,        // Radio buttons (automatically selects)
    DatePicker,   // Date input fields
    Button        // Buttons, links, clickable elements
}
---

## **Key Framework Enhancements**

### **1. Clean Syntax**// OLD approach
await PageHelper.InteractWithElement(LocatorType.CSS, "#field", ElementAction.Fill, options);

// NEW approach  
await PageHelper.InteractWithField(FieldNames.FieldName, value);
### **2. Registry resolution**
The active `FieldRegistry` is resolved from the scope context (`scopeContext.GetOrCacheFieldRegistry()` via `ProjectContextManager`) — one registry per `FrontEndType`, cached per scope.

### **3. Smart Field Type Mapping**
- **Input** → `Fill` automatically
- **Dropdown** → `Select` automatically  
- **Checkbox** → `Check/Uncheck` automatically
- **Button** → `Click` automatically

### **4. Performance Optimizations**
- **Caching**: Registry resolution cached per scope
- **Lazy loading**: Field registries and flows loaded on-demand (static-constructor registration)

---

## **Framework Benefits**

- **Clean Syntax**: One-line field interactions
- **Automatic Detection**: No manual project specification needed
- **Type Safety**: Compile-time field validation using FieldNames constants
- **High Performance**: Caching and optimizations
- **Easy Maintenance**: Centralized field definitions
- **Extensible**: Easy to add new projects and field types

---

## **Current Project Structure Examples**

### **Existing Projects:**
`Projects/` contains one folder per front end: `ADBX`, `D2C`, `HQXAgent` (formerly PGR), `HQXConsumer`, `Interview`, `PartnerPortal`, `STS`.

### **Working Components:**
- `ProjectContextManager` — resolves the active registry from the scope context
- `FieldRegistryProvider` — automatic initialization via static constructors
- `FlowDefaultsProvider` — cached per-front-end/LOB default data
- `InteractWithField()` clean syntax in PageHelper
- Configuration-driven project detection (no hardcoded strings)
- `[FlowInitializer]` attribute for automatic flow registration

---

## **Important Notes**

### **FlowType Placement**
Each project should have its own `FlowType.cs` enum in the `Flows/` folder, not a global FlowType enum.

### **Flow Registration**
- Use `[FlowInitializer]` attribute on flow creation methods for automatic registration
- Flow methods must be static, parameterless, and return `FlowsHelpers`

### **Base Class Features**
- Include `IAutomationLogger` for comprehensive logging
- Provide both `Field()` and `FieldWithValue()` helper methods
- Inherit from `IInterview` interface

### **ProjectRegistry Structure**
- The ADBX project uses folder name `ADB` but type name `ADBX`
- URL identifiers support multiple patterns per project
- `AdditionalNamespaces` enables detection from Popups and Flows

---

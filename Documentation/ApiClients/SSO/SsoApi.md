# SSO API Documentation

## Overview

The SSO (Single Sign-On) API provides authentication capabilities for the Bolt automation framework. It handles SAML-based authentication flows for different tenants (USAA, COMPARION, LIBERTYX, BOLTAG) and manages the creation, signing, and transmission of SAML assertions.

## Architecture

The SSO API follows a factory pattern with dependency injection and uses Refit for HTTP client generation. The system is designed to be tenant-aware and supports multiple authentication scenarios through configurable SAML templates.

---

## Core Classes

### 1. ISsoApi Interface

**Purpose**: Defines the contract for SSO API operations.


### 2. ISsoApiFactory Interface

**Purpose**: Factory interface for creating SSO API clients.

### 3. SsoApiFactory Class

**Purpose**: Concrete implementation of the SSO API factory that creates tenant-specific HTTP clients.


**Dependencies**:
- `IScopeContext`: Current test execution context — the tenant SSO URL comes from `_scopeContext.Data.UrlDataCollection.SsoApi` (there is no options class)
- `IAutomationLogger`: Logging functionality
- `ICryptographyService`: Cryptographic operations for SAML signing

**Key Methods**:
- **CreateClient()**: 
  - Creates HTTP client with custom handler
  - Configures the base URL from the scope context's `UrlDataCollection.SsoApi`
  - Returns Refit-generated API client


### 4. SsoApiHandler Class

**Purpose**: Custom HTTP message handler that processes SSO requests and responses.

**Key Responsibilities**:
- Generates signed SAML assertions
- Processes HTTP responses
- Handles error scenarios
- Extracts redirect URLs from JavaScript responses

**Key Methods**:

#### SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
- **Purpose**: Intercepts HTTP requests to add SAML content and process responses
- **Process**:
  1. Configures request URI based on tenant
  2. Generates signed SAML assertion
  3. Sets request content as form-urlencoded
  4. Sends request and logs API call
  5. Processes response content

#### PrepareSsoSaml(UserTestData user, UrlTestData url, RelayStateTestData relayState)
- **Purpose**: Creates signed SAML assertion
- **Process**:
  1. Gets user profile
  2. Determines SAML template
  3. Calls SsoHelper to generate signed SAML
  4. Uses cryptography service for BOLTAG tenant

**Response Processing**:
- **HTML Error Pages**: Extracts error messages from paragraph tags with "error" class
- **JavaScript Redirects**: Parses redirect URLs from window.parent.location assignments
- **JSON Responses**: Creates structured response with RedirectUrl

### 5. SsoHttpResponse Class

**Purpose**: Data transfer object for SSO API responses.


**Properties**:
- **RedirectUrl**: URL to redirect user after successful authentication

### 6. SsoApiServiceExtensions Class

**Purpose**: Extension methods for dependency injection configuration.


**Methods**:
- **AddSsoApi(IServiceCollection services, IConfiguration configuration)**: Configures all SSO-related services in the DI container

---

## SAML Folder - Comprehensive Method Documentation

The SAML folder contains classes responsible for creating, signing, and managing SAML assertions for SSO authentication.

### 1. SsoHelper Class

**Purpose**: High-level helper for generating signed SAML assertions.


#### GetSsoSignedSaml(UserTestData user, UrlTestData url, RelayStateTestData relayState, string samlTemp, ICryptographyService? cryptographyService = null, int? validMinutes = null)
- **Purpose**: Creates a complete signed SAML assertion with relay state
- **Parameters**:
  - `ssoObj`: Contains user and configuration data
  - `samlTemp`: Name of SAML template to use
  - `cryptographyService`: Optional crypto service for advanced signing
- **Process**:
  1. Extracts destination, recipient, and audience from SSO object
  2. Creates SamlRecipient object
  3. Configures SamlSigner with user data
  4. Loads XML template from resources
  5. Updates GroupExternalId attribute in SAML if present
  6. Generates signed SAML using appropriate method
  7. Returns formatted relay state with SAML response
- **Return**: Formatted string "RelayState={relayState}&SAMLResponse={encodedSaml}"

### 2. SamlSigner Class

**Purpose**: Core class for SAML assertion creation and digital signing.

**Properties**:
- **Recipient**: SAML recipient information (destination, recipient, audience)
- **Issuer**: Entity that issues the SAML assertion
- **NameId**: Subject identifier
- **MemberNumber**: Member identification number
- **GroupExternalId**: External group identifier
- **Source, FirstName, LastName, Email, Phone**: User profile data
- **ResponseId**: Unique response identifier (GUID)
- **IssueInstant**: Timestamp when assertion was issued
- **NotBefore**: Validity start time
- **NotOnOrAfter**: Validity end time
- **AssertionId**: Unique assertion identifier

#### GetSaml()
- **Purpose**: Creates a signed SAML assertion using local certificate
- **Process**:
  1. Creates copy of SAML template
  2. Sets up XML namespace manager
  3. Inserts user data using SamlAssertionBuilder
  4. Creates signed version
  5. Signs SAML with local certificate
- **Return**: Signed XmlDocument

#### GetSaml(ICryptographyService cryptoService)
- **Purpose**: Creates a signed SAML assertion using external cryptography service
- **Process**:
  1. Creates copy of SAML template
  2. Sets up XML namespace manager
  3. Inserts user data
  4. Extracts assertion ID
  5. Uses cryptography service for signing
  6. Processes signed element and inserts into assertion
- **Return**: Signed XmlDocument

#### SetCert(string subject)
- **Purpose**: Loads appropriate certificate for signing
- **Logic**:
  - If subject is "Default": Loads embedded certificate with dynamic password selection
  - Otherwise: Loads certificate from local machine store
- **Password Logic**: Uses different passwords for Farmers issuer at specific endpoint

#### SignSaml(XmlDocument saml, XmlNamespaceManager nsmgr, string assertionId)
- **Purpose**: Digitally signs SAML assertion using X.509 certificate
- **Process**:
  1. Creates SignedXml object with private key
  2. Configures KeyInfo with certificate data
  3. Sets canonicalization method
  4. Creates reference to assertion element
  5. Adds transforms for signing
  6. Computes signature
  7. Inserts signature element into assertion

#### SendSamlProduct()
- **Purpose**: Complete workflow for creating and encoding SAML
- **Process**:
  1. Sets default certificate
  2. Generates signed SAML
  3. Encodes SAML to Base64
- **Return**: Base64-encoded SAML assertion

#### SendSamlProduct(ICryptographyService cryptoService)
- **Purpose**: Complete workflow using external crypto service
- **Process**: Same as above but uses cryptography service for signing

#### GetXmlTemplateFromResource(string templateName)
- **Purpose**: Loads SAML template from embedded resources
- **Process**:
  1. Uses ResourceManager to get template string
  2. Loads string into XmlDocument
  3. Validates template exists
- **Return**: XmlDocument with template content

#### SetSamlTemplateFromResource(string templateName)
- **Purpose**: Sets internal SAML template from resources
- **Uses**: SamlTemplateManager to load template

#### SetSamlTemplate(XmlDocument samlTemplate)
- **Purpose**: Sets internal SAML template from provided XmlDocument

### 3. SamlAssertionBuilder Class

**Purpose**: Responsible for populating SAML templates with actual user data.

#### InsertData(XmlDocument saml, XmlNamespaceManager nsmgr)
- **Purpose**: Populates SAML template with user and configuration data
- **Process**:
  1. Updates response-level attributes (Destination, ID, IssueInstant)
  2. Updates issuer information
  3. Updates subject NameID
  4. Updates assertion metadata (ID, IssueInstant)
  5. Updates subject confirmation data
  6. Updates conditions (NotBefore, NotOnOrAfter)
  7. Updates audience restriction
- **XPath Updates**:
  - Response destination and metadata
  - Issuer elements at response and assertion level
  - Subject NameID and confirmation data
  - Temporal validity conditions
  - Audience restrictions
- **Return**: AttributeStatement node for further processing

#### UpdateValue(XmlDocument saml, XmlNamespaceManager nsmgr, string xpath, string val)
- **Purpose**: Helper method to update XML values using XPath
- **Logic**:
  - For XmlAttribute nodes: Updates Value property
  - For XmlElement nodes: Updates InnerText property

### 4. SamlRecipient Class

**Purpose**: Data structure representing SAML assertion recipient information.

**Properties**:
- **Destination**: Target URL where SAML response will be sent
- **Recipient**: Entity that will receive the SAML assertion
- **Audience**: Intended audience for the SAML assertion

**Constructor**: Takes destination, recipient, and audience parameters

### 5. SamlEncoder Class

**Purpose**: Utility class for encoding SAML assertions for transport.


#### Encode(XmlDocument saml)
- **Purpose**: Converts SAML XML to Base64-encoded, URL-encoded string
- **Process**:
  1. Extracts InnerXml from document
  2. Converts to ASCII bytes
  3. Encodes to Base64
  4. URL-encodes the result
- **Return**: URL-encoded Base64 string suitable for HTTP transmission

### 6. SamlTemplateManager Class

**Purpose**: Manages loading of SAML templates from embedded resources.


#### LoadTemplateFromResource(string templateName)
- **Purpose**: Loads SAML template from embedded resources
- **Process**:
  1. Uses ResourceManager to retrieve template string
  2. Validates template exists
  3. Loads into XmlDocument
- **Return**: XmlDocument containing template

### 7. CertificateLoader Class

**Purpose**: Handles loading X.509 certificates from various sources.


#### LoadFromStore(string subject)
- **Purpose**: Loads certificate from local machine certificate store
- **Process**:
  1. Opens LocalMachine certificate store
  2. Searches for certificate by subject name
  3. Determines content type (Cert or Pkcs12)
  4. Loads certificate with appropriate method
- **Return**: X509Certificate2 object or null if not found

#### LoadFromResource(string resourceName, string password)
- **Purpose**: Loads certificate from embedded resource
- **Process**:
  1. Gets resource stream from assembly
  2. Reads certificate bytes
  3. Determines content type
  4. Loads certificate with password
- **Return**: X509Certificate2 object

---

## Configuration

There are no SSO options/config classes. User, URL, and relay-state inputs come from the scope context's test data: `UserTestData`, `UrlTestData` (`_scopeContext.Data.UrlDataCollection.SsoApi`), and `RelayStateTestData`.

---

## Available SAML Templates

Based on the Resources file, the following SAML templates are available:

1. **Saml2ResponseTemplate**: Standard SAML 2.0 response template
2. **Saml2ResponseTemplateWithGroupExternalId**: Template with group external ID support
3. **SamlPartnerPortal**: Partner portal specific template
4. **SaisSamlResponseTemplateGetCarrierCredentials**: SAIS-specific template for carrier credentials


## Key SSO Usage Patterns

### 1. Basic SSO Authentication Pattern
```
1. Set Test Context (Tenant, User, RelayState)
2. Create SSO Client via Factory
3. Call GetSsoResponse()
4. Handler Generates Signed SAML
5. Post SAML to SSO Server
6. Process Response (Success/Error)
7. Extract Redirect URL or Handle Error
```

### 2. Multi-Tenant SSO Pattern
```
1. Configure Tenant-Specific Settings
2. Load Tenant Configuration at Runtime
3. Create Tenant-Aware SSO Client
4. Generate Tenant-Specific SAML
5. Use Tenant-Specific Certificate (if required)
6. Post to Tenant-Specific SSO Endpoint
7. Handle Tenant-Specific Response Format
```

### 3. SAML Template Customization Pattern
```
1. Set Custom SAML Template in Context
2. Template Manager Loads from Resources
3. SAML Signer Populates Template
4. Custom Attributes Added (GroupExternalId)
5. Template Signed with Certificate
6. Encoded SAML Posted to Server
7. Server Validates Custom SAML Structure
```

### 4. Certificate Management Pattern
```
1. Determine Certificate Source (Default/Custom)
2. Load from Store or Embedded Resource
3. Handle Password Management
4. Configure Certificate for Signing
5. Use Certificate for SAML Signing
6. Ensure Certificate Validity
7. Handle Certificate Errors
```
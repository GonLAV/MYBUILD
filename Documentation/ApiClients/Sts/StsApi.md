# STS API Documentation

## Overview

The STS (Security Token Service) and ADBX APIs work together to provide secure authentication and data access capabilities for the Bolt automation framework. The STS API handles authentication and token generation, while the ADBX API provides access to quote data and other business operations using Bearer token authentication.

## Architecture

API follows the factory pattern with dependency injection and use Refit for HTTP client generation. The authentication flow involves a multi-step process where STS provides initial authentication tokens, which are then exchanged for session tokens used by the ADBX API.

## STS API Documentation

### Overview

The STS (Security Token Service) API is responsible for handling authentication and generating security tokens that are used by other services in the Bolt ecosystem. It provides a secure way to authenticate users and obtain tokens for subsequent API calls.

### Core Components

#### 1. IStsApi Interface

**Purpose**: Defines the contract for STS API operations.

**Methods**:
- **GetLoginPage()**: Retrieves the login page HTML containing authentication forms and verification tokens
- **GetToken([Body] string body)**: Submits authentication credentials and receives a security token

#### 2. StsApiHandler Class

**Purpose**: Custom HTTP message handler for STS API requests.

**Key Responsibilities**:
- Logs all API calls for debugging and monitoring
- Handles HTTP request/response processing
- Provides transparent logging of authentication attempts

**Key Methods**:

##### SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
- **Purpose**: Intercepts and processes HTTP requests to the STS API
- **Process**:
  1. Forwards the request to the base handler
  2. Logs the API call with request and response details
  3. Returns the response to the caller

#### 3. StsApiServiceExtensions Class

**Purpose**: Extension methods for dependency injection configuration.

**Methods**:
- **AddStsApi(IServiceCollection services, IConfiguration configuration)**: Configures STS API services in the DI container

### Configuration

There is no dedicated options class — the STS base URL is resolved from the scope context's test data (`UrlDataCollection`), following the same pattern as the SSO API.

### Authentication Flow

The STS API follows a traditional form-based authentication pattern:

1. **Login Page Request**: Client requests the login page to obtain form structure and verification tokens
2. **Credential Submission**: Client submits username, password, and verification token
3. **Token Generation**: STS validates credentials and returns a security token
4. **Token Usage**: The token is used for subsequent API calls to other services

---

### Entity Classes

#### EntranceData Class

**Purpose**: Data structure for session token exchange.

**Properties**:
- **Tenant**: Tenant identifier
- **Token**: STS authentication token

### Complete Authentication Flow

1. **Initial Setup**:
   - User and tenant context established
   - Configuration retrieved from options

2. **STS Authentication**:
   - Login page requested from STS
   - Verification token extracted from HTML
   - Credentials submitted with verification token
   - Authentication token extracted from response

3. **API Operations**:
   - Authenticated requests sent with Bearer token
   - Quote data retrieved using authenticated client
   - Responses processed and returned to caller

## Key Usage Patterns

### STS API Usage Pattern

1. Create STS Client via Factory
2. Get Login Page (HTML Form)
3. Extract Verification Token
4. Submit Credentials + Token
5. Extract Authentication Token
6. Use Token for Other Services

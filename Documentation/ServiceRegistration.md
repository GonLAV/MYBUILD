# Service Registration in Bolt Automation

This document explains how dependency injection and service registration are handled in the Bolt Automation solution, focusing on the layered, modular approach and its use in test automation.

- See [Configuration](./Configuration.md) for details on configuration loading and options.
- See [Adding New Projects or Services](./AddingNewProjectOrService.md) for step-by-step guidance.

## Overview

Service registration in Bolt Automation is centralized in the `AddInfrastructureServices` extension method (Core project). This method orchestrates the registration of all required services, logging, and configuration with the .NET dependency injection container, but delegates the actual registrations to project-level and service-level extension methods.

## Layered and Modular Registration

1. **Central Entry Point (Core):**
   - `AddInfrastructureServices` is the main entry point, called during test infrastructure setup.
   - It does not register all services directly, but delegates to project-level extension methods.
2. **Project-Level Registration:**
   - Each major project exposes its own extension method for registering its services (e.g., `AddApiClients`, `AddExternalServices`).
   - These methods are implemented in files like `*ServiceExtensions.cs` or `*Extensions.cs` within each project.
3. **Service-Level Registration:**
   - Project-level extension methods further delegate to service-specific extension methods (e.g., `AddStsApi`).
   - Each is implemented in its own file, encapsulating registration logic for that service.
4. **Layering in Practice:**
   - Core orchestrates registration.
   - Each project manages its own dependencies and exposes a single extension for registration.
   - Each service within a project has its own extension for registration, keeping concerns isolated and modular.

## How It’s Used in Test Automation

- The `TestInfrastructure` class (Core) initializes configuration and builds the service provider using the registration pipeline above.
- The `TestBase` class (in Tests project) uses the static `TestInfrastructure.ServiceProvider` to create a scope for each test, resolving required services from the DI container.
- Test context (environment, tenant, etc.) is set up per test, and logging is integrated with the test output.

## Summary Diagram

```
[Test] 
   ↓
[TestBase] 
   ↓
[TestInfrastructure] 
   ↓
[AddInfrastructureServices (Core)]
   ↓
[AddApiClients] [AddExternalServices] [AddFrontEndServices] [AddInternalServices] [AddCommonServices] ...
   ↓
[AddStsApi] [AddLaunchDarklyServices] [AddBrowserServices] [AddMicroserviceInfrastructure] ...
```

## Key Benefits
- **Separation of Concerns:** Each project and service manages its own registration logic.
- **Extensibility:** New services or projects can be added with minimal changes to the central registration.
- **Test Isolation:** Each test gets its own DI scope, ensuring clean state and proper disposal.
- **Configurability:** Environment and configuration are loaded dynamically, supporting multiple test environments.

---

For more details, see the source code in each project's `*ServiceExtensions.cs` and `*Extensions.cs` files, and the `InfrastructureServiceCollectionExtensions` in the Core project.


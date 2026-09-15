# Options Classes in Bolt Automation

This document explains where to define and how to use options classes for configuration in the Bolt Automation solution.

- See [Configuration](./Configuration.md) for how configuration is loaded and bound.
- See [Adding New Projects or Services](./AddingNewProjectOrService.md) for steps to add new options classes.

## Location of Options Classes

All options classes (such as `GetQuoteApiOptions`, `BrowserOptions`, `LaunchDarklyClientOptions`, etc.) are located in the `Automation.Configuration` project. This project contains dedicated folders for each domain:

- `ApiClients/`
- `FrontEnds/`
- `ExternalServices/`
- `InternalServices/`

## How to Use Options Classes

1. **Define the options class** in the appropriate folder within `Automation.Configuration`.
2. **Reference the options class** from your service registration extension and bind it to the relevant configuration section using `.Configure<TOptions>(configuration.GetSection(...))`.
3. **Inject options** into your service using `IOptions<TOptions>` or `IOptionsSnapshot<TOptions>`.

This approach centralizes configuration schemas, ensures consistency, and makes options reusable across the solution.


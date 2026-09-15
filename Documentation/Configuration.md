# Configuration in Bolt Automation

This document describes how configuration is loaded, prioritized, and consumed in the Bolt Automation solution.

- See [Service Registration](./ServiceRegistration.md) for DI and registration details.
- See [Options Classes](./OptionsClasses.md) for where to define and how to use options classes.

## Configuration Loading Process

Configuration is loaded using a layered and prioritized approach, managed by the `ConfigurationLoader` class and invoked by `TestInfrastructure` at startup.

### Priority Hierarchy
Configuration sources are loaded in the following order (lowest to highest priority):

1. **appsettings.json** – Base configuration file.
2. **appsettings.{Environment}.json** – Environment-specific configuration, where `{Environment}` is determined from test parameters or environment variables. If not found, standard environment names are checked.
3. **Bolt secrets bundle** – `AddBoltSecrets(BOLT_SECRETS_PATH, environment)` layers the local/CI secrets bundle on top of the JSON files (see the `nexus-secrets` skill for local setup).
4. **Environment Variables** – All environment variables are added.
5. **Command-Line Arguments** – If available, these are added.
6. **Test Parameters** – Includes parameters from `.runsettings` files and any provided dictionary, added as an in-memory collection.
7. **runsettings.local.json** – Local override file, loaded last and with the highest priority.

### .runsettings File Support
- The loader searches for `.runsettings` files in the current directory, test assembly directory, and any path specified by the `RunSettingsFilePath` environment variable.
- Parameters from these files are merged with other test parameters.

### Environment Detection
- The environment name is determined from test parameters or environment variables.
- This name is used to select the correct environment-specific configuration file.

### Logging and Diagnostics
- The loader logs which configuration files and sources are loaded, and warns if expected files are missing.

### Usage in Tests
- `TestInfrastructure` calls `ConfigurationLoader.LoadConfigurations` at startup.
- The resulting configuration and environment name are available to all tests via static properties.
- The configuration is injected into the DI container and can be accessed by any service or test.

---

## How Services Load and Use Configuration

Services access configuration in a consistent and test-friendly way, leveraging .NET's dependency injection and options pattern:

1. **Dependency Injection of IConfiguration:**
   - The `IConfiguration` instance is registered as a singleton in the DI container.
   - Any service can request `IConfiguration` in its constructor to access configuration values directly.
2. **Strongly-Typed Options Pattern:**
   - For structured configuration, services define options classes (see [Options Classes](./OptionsClasses.md)).
   - During service registration, configuration sections are bound to these options classes using `.Configure<TOptions>(configuration.GetSection(...))`.
   - Services receive `IOptions<TOptions>` or `IOptionsSnapshot<TOptions>` via constructor injection.
3. **Testability and Isolation:**
   - Tests can override configuration at startup, and all services receive the correct values for the test context.
   - Each test gets its own DI scope and configuration snapshot, preventing cross-test contamination.

---

For more, see [Adding New Projects or Services](./AddingNewProjectOrService.md).


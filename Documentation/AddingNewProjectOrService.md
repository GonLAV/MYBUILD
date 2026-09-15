# Adding New Projects or Services

This guide explains the steps to add a new project (such as an API client, external integration, or frontend) or a new service within an existing project in the Bolt Automation solution.

- See [Service Registration](./ServiceRegistration.md) for the overall DI and registration architecture.
- See [Configuration](./Configuration.md) for configuration loading and options.
- See [Options Classes](./OptionsClasses.md) for where to define and how to use options classes.

## Steps for a New Project

1. **Create the Project:** Add a new class library project to the solution (e.g., `Bolt.Automation.NewService`).
2. **Define Options Classes:** If your services require configuration, define options classes in the appropriate folder within `Automation.Configuration` (see [Options Classes](./OptionsClasses.md)).
3. **Implement Service Registration Extension:**
   - Create a static extension class (e.g., `NewServiceExtensions`) with a method like `AddNewService(this IServiceCollection services, IConfiguration configuration)`.
   - Register your services and bind configuration sections to options classes using `.Configure<TOptions>(configuration.GetSection(...))`.
4. **Expose Project-Level Registration:**
   - If the project contains multiple services, create a project-level extension (e.g., `AddNewServices`) that calls all individual service registration methods.
5. **Update Central Registration:**
   - In `InfrastructureServiceCollectionExtensions` (Core), call your new project-level registration method (e.g., `services.AddNewServices(configuration);`).
6. **Add Configuration Sections:**
   - Add the required configuration sections to `appsettings.json` and environment-specific files.
   - Document the configuration keys and expected values.

## Steps for a New Service in an Existing Project

1. **Define Options Class (if needed):** Create an options class for the service’s configuration in `Automation.Configuration`.
2. **Implement Service Registration Extension:**
   - Add a static extension method (e.g., `AddSpecificService`) in the appropriate `*ServiceExtensions.cs` file.
   - Register the service and bind its configuration section.
3. **Update Project-Level Registration:**
   - Ensure the project-level extension (e.g., `AddApiClients`) calls your new service registration method.
4. **Add Configuration Section:**
   - Update configuration files with the new section and document its usage.

## General Best Practices

- **Use Strongly-Typed Options:** Prefer options classes over direct `IConfiguration` access for structured settings.
- **Constructor Injection:** Always use constructor injection for dependencies and options.
- **Document Configuration:** Clearly document required and optional configuration keys for each service.
- **Test Registration:** Add or update tests to verify that your service is registered and receives the correct configuration.

By following these steps, you ensure that new projects and services are consistently registered, properly configured, and easy to maintain within the Bolt Automation architecture.


using Bogus;
using Bolt.Automation.Common.Context;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.Common.Utils;

/// <summary>
/// Selects first and last names appropriate for the current test environment.
/// <list type="bullet">
///   <item><b>Staging</b>: generates realistic names via the Bogus library so staging data
///   doesn't contain obviously synthetic names like "AaNexusabcdef".</item>
///   <item><b>All other environments</b>: falls back to the existing random-suffix generation
///   so existing test uniqueness guarantees are preserved.</item>
/// </list>
///
/// <para>
/// Usage — register the scope context once from <c>TestBase</c> after the environment is set,
/// then all callers automatically get environment-appropriate names:
/// <code>
///   NameSelector.RegisterScopeContext(scopeContext);
///   var firstName = NameSelector.GetFirstName();
/// </code>
/// </para>
///
/// <para>
/// Explicit override:
/// <code>
///   var firstName = NameSelector.GetFirstName(Environment.Staging);
/// </code>
/// </para>
/// </summary>
public static class NameSelector
{
    private static readonly ThreadLocal<Faker> _faker =
        new(() => new Faker("en"));

    private static IScopeContext? _scopeContext;

    /// <summary>
    /// Registers the scope context used by parameterless overloads to resolve the current environment.
    /// Call this once from <c>TestBase</c> after <c>scopeContext.Set(ctx => ctx.Environment, ...)</c>.
    /// </summary>
    public static void RegisterScopeContext(IScopeContext scopeContext) => _scopeContext = scopeContext;

    /// <summary>Returns the currently active environment, resolved from scope context or fallback.</summary>
    public static Environment? ActiveEnvironment => _scopeContext?.Data.Environment;

    /// <summary>
    /// Returns a first name suitable for <paramref name="environment"/>,
    /// or the environment from the registered scope context when <paramref name="environment"/> is <c>null</c>.
    /// </summary>
    public static string GetFirstName(Environment? environment = null, string? fallback = null)
    {
        var env = environment ?? _scopeContext?.Data.Environment;
        return env == Environment.Staging
            ? _faker.Value!.Name.FirstName()
            : fallback ?? "AaNexus" + RandomManager.GetRandomString(6);
    }

    /// <summary>
    /// Returns a last name suitable for <paramref name="environment"/>,
    /// or the environment from the registered scope context when <paramref name="environment"/> is <c>null</c>.
    /// </summary>
    public static string GetLastName(Environment? environment = null, string? fallback = null)
    {
        var env = environment ?? _scopeContext?.Data.Environment;
        return env == Environment.Staging
            ? _faker.Value!.Name.LastName()
            : fallback ?? "Creditpulse" + RandomManager.GetRandomString(4);
    }

    /// <summary>
    /// Returns a business name suitable for <paramref name="environment"/>,
    /// or the environment from the registered scope context when <paramref name="environment"/> is <c>null</c>.
    /// Commercial accounts carry a business name rather than a person's name, so staging needs this for
    /// the same reason it needs <see cref="GetFirstName"/>.
    /// </summary>
    public static string GetCompanyName(Environment? environment = null, string? fallback = null)
    {
        var env = environment ?? _scopeContext?.Data.Environment;
        return env == Environment.Staging
            ? _faker.Value!.Company.CompanyName()
            : fallback ?? "Automation" + RandomManager.GetRandomString(5);
    }
}

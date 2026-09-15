using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

public static class AccessibilityServiceExtensions
{
    /// <summary>Registers the axe scanner and keyboard traversal helper, scoped per test.</summary>
    public static IServiceCollection AddAccessibilityServices(this IServiceCollection services)
    {
        services.AddScoped<IAccessibilityScanner, AccessibilityScanner>();
        services.AddScoped<KeyboardTraversalHelper>();

        return services;
    }
}

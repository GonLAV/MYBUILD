namespace Bolt.Automation.Common.Exceptions
{
    /// <summary>
    /// Base class for all automation exceptions. Overrides ToString() to use the short type name
    /// so NUnit output shows e.g. "CarrierNotFoundException" instead of the fully qualified path.
    /// </summary>
    public abstract class AutomationException : Exception
    {
        protected AutomationException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// Thrown when a page object cannot be instantiated (e.g., browser is unavailable or constructor fails).
    /// </summary>
    public class PageCreationException : AutomationException
    {
        public string PageTypeName { get; }

        public PageCreationException(string pageTypeName, Exception? inner = null)
            : base($"Failed to create page instance of type {pageTypeName}.", inner)
        {
            PageTypeName = pageTypeName;
        }
    }

    /// <summary>
    /// Thrown when an expected carrier cannot be found or selected on a page.
    /// </summary>
    public class CarrierNotFoundException : AutomationException
    {
        public string CarrierName { get; }

        public CarrierNotFoundException(string carrierName, string context, IEnumerable<string>? available = null)
            : base(BuildMessage(carrierName, context, available))
        {
            CarrierName = carrierName;
        }

        private static string BuildMessage(string carrier, string context, IEnumerable<string>? available)
        {
            var msg = $"Carrier '{carrier}' not found on {context}.";
            if (available != null)
                msg += $" Available: [{string.Join(", ", available)}]";
            return msg;
        }
    }

    /// <summary>
    /// Thrown when one or more coverage dropdown values do not match the expected values.
    /// </summary>
    public class CoverageValidationException : AutomationException
    {
        public CoverageValidationException(string summary)
            : base($"Coverage validation failed: {summary}") { }
    }

    /// <summary>
    /// Thrown when a UI element cannot be found, interacted with, or is in an unexpected state.
    /// </summary>
    public class PageElementException : AutomationException
    {
        public string ElementDescription { get; }

        public PageElementException(string elementDescription, string? detail = null, Exception? inner = null)
            : base(detail is null ? elementDescription : $"{elementDescription} {detail}", inner)
        {
            ElementDescription = elementDescription;
        }
    }

    /// <summary>
    /// Thrown when a popup fails to appear or its action buttons cannot be clicked within the timeout.
    /// </summary>
    public class PopupTimeoutException : AutomationException
    {
        public string PopupName { get; }

        public PopupTimeoutException(string popupName, string action, int timeoutMs, Exception? inner = null)
            : base($"Popup '{popupName}': {action} within {timeoutMs}ms.", inner)
        {
            PopupName = popupName;
        }
    }

    /// <summary>
    /// Thrown when required test preconditions are not met — missing API data, unconfigured URLs, or absent test data.
    /// </summary>
    public class TestSetupException : AutomationException
    {
        public TestSetupException(string message) : base(message) { }
    }

    /// <summary>
    /// Thrown when navigation to a page fails after all retry attempts.
    /// </summary>
    public class NavigationException : AutomationException
    {
        public NavigationException(string message, Exception? inner = null) : base(message, inner) { }

        public NavigationException(string fromPage, string toPage, string url, int attempts, Exception? inner = null)
            : base($"Failed to navigate FROM [{fromPage}] TO [{toPage}] ({url}) after {attempts} attempts.", inner) { }
    }

    /// <summary>
    /// Thrown when an API response is invalid or contains unexpected data (e.g., missing IDs, no successful quotes).
    /// </summary>
    public class ApiResponseException : AutomationException
    {
        public ApiResponseException(string message) : base(message) { }
        public ApiResponseException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Thrown when one or more <c>INJECTED_*</c> environment variables are missing or invalid
    /// when binding to an InjectedTestConfig. Aggregates every failure into a single message
    /// so the engineer can fix the whole environment in one pass.
    /// </summary>
    public class InjectedConfigValidationException : AutomationException
    {
        public IReadOnlyList<string> Errors { get; }

        public InjectedConfigValidationException(IReadOnlyList<string> errors)
            : base(BuildMessage(errors))
        {
            Errors = errors;
        }

        private static string BuildMessage(IReadOnlyList<string> errors)
        {
            if (errors.Count == 0)
                return "InjectedTestConfig validation failed.";

            var lines = new List<string>
            {
                $"InjectedTestConfig validation failed ({errors.Count} error{(errors.Count == 1 ? "" : "s")}):"
            };
            lines.AddRange(errors.Select(e => "  - " + e));
            return string.Join(System.Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// Thrown when the browser lands on an application error page that carries a ticket number,
    /// correlation id, or both. Groups by <see cref="Models.AppErrorSeverity"/> (expected vs unexpected)
    /// and <see cref="Models.AppErrorSource"/> (URL, DOM, or network response header).
    /// </summary>
    public class ApplicationTicketException : AutomationException
    {
        public Models.AppErrorContext Context { get; }

        public ApplicationTicketException(Models.AppErrorContext context, Exception? inner = null)
            : base(BuildMessage(context), inner)
        {
            Context = context;
        }

        private static string BuildMessage(Models.AppErrorContext ctx)
        {
            var message =
                $"[{ctx.Severity}/{ctx.Source}] Application error page detected. " +
                $"Ticket: {ctx.TicketNumber ?? "N/A"}, " +
                $"CorrelationId: {ctx.CorrelationId ?? "N/A"}, " +
                $"NetworkStatus: {ctx.NetworkStatusCode?.ToString() ?? "-"}, " +
                $"URL: {ctx.ErrorUrl}";

            // Surface EVERY failing request that fired during the navigation, in order — the final
            // /error redirect often hides an earlier failure that is the real cause (e.g. an
            // extractToken 500 before the update 400 that redirected).
            if (ctx.NetworkErrors.Count > 0)
                message += $". Network errors ({ctx.NetworkErrors.Count}): {string.Join("; ", ctx.NetworkErrors)}";

            return message;
        }
    }

    /// <summary>
    /// Thrown when the browser lands on a kickout page that was not expected by the test.
    /// Carries a <see cref="Models.KickoutContext"/> with the error code and message scraped
    /// from the kickout page DOM (e.g. "Error code: 301").
    /// </summary>
    public class UnexpectedKickoutException : AutomationException
    {
        public Models.KickoutContext Context { get; }

        public UnexpectedKickoutException(Models.KickoutContext context, Exception? inner = null)
            : base(BuildMessage(context), inner)
        {
            Context = context;
        }

        private static string BuildMessage(Models.KickoutContext ctx) =>
            $"[{ctx.Severity}] Unexpected kickout page detected. " +
            $"ErrorCode: {ctx.ErrorCode ?? "N/A"}, " +
            $"Message: {ctx.ErrorMessage ?? "N/A"}, " +
            $"URL: {ctx.KickoutUrl}";
    }
}

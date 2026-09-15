namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

/// <summary>
/// Utility class for creating standardized BsonDocument payloads for UI action logging.
/// This ensures consistent logging structure across all page helpers.
/// </summary>
public static class UiActionLogger
{
    /// <summary>
    /// Creates a standardized BsonDocument payload for UI action logging.
    /// </summary>
    /// <param name="action">The action being performed (e.g., "WaitForElement", "Click", "Type")</param>
    /// <param name="status">The status of the action ("Success" or "Failed")</param>
    /// <param name="durationMs">Duration of the action in milliseconds</param>
    /// <param name="pageUrl">The current page URL</param>
    /// <param name="additionalFields">Optional dictionary of action-specific fields (e.g., timeout, retries, locator)</param>
    /// <param name="error">Optional error message if the action failed</param>
    /// <returns>A BsonDocument with standardized structure for logging</returns>
    /// <example>
    /// var payload = UiActionLogger.CreatePayload("Click", "Success", 250, page.Url, new()
    /// {
    ///     { "locator", "#submitButton" },
    ///     { "timeout", 5000 }
    /// });
    /// logger?.LogUiAction("Click", "#submitButton", payload);
    /// </example>
    public static MongoDB.Bson.BsonDocument CreatePayload(
        string action,
        string status,
        long durationMs,
        string pageUrl,
        Dictionary<string, object>? additionalFields = null,
        string? error = null)
    {
        var payload = new MongoDB.Bson.BsonDocument
        {
            { "action", action },
            { "status", status },
            { "durationMs", durationMs },
            { "pageUrl", pageUrl }
        };

        if (additionalFields != null)
        {
            foreach (var field in additionalFields)
            {
                payload[field.Key] = MongoDB.Bson.BsonValue.Create(field.Value);
            }
        }

        if (error != null)
        {
            payload["error"] = error;
        }

        return payload;
    }
}

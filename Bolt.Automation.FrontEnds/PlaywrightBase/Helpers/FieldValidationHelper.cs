using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    /// <summary>
    /// Field-level validation checks — the value a field actually keeps, and the validation message
    /// it shows. Product-agnostic: fields resolve through the registry for the current front end,
    /// so this works for any page. Every method returns what it observed and none of them assert;
    /// the pass/fail decision stays in the test.
    /// </summary>
    public class FieldValidationHelper(
        IPageHelper pageHelper,
        IAutomationLogger? logger = null)
    {
        /// <summary>
        /// Types <paramref name="value"/> one keystroke at a time, leaves the field, and returns the
        /// value the field actually kept. Sequential typing mirrors a real user, so per-keystroke
        /// character filtering and mask formatting are exercised the way a person would hit them.
        /// An empty <paramref name="value"/> clears the field and leaves it, which is how a
        /// "left empty" case is expressed.
        /// </summary>
        public async Task<string> EnterValueAndLeaveField(string fieldName, string value)
        {
            await pageHelper.InteractWithField(fieldName, new ElementInteractionOptions
            {
                Value = value,
                UseSequentialTyping = true,
                // The interaction already blurs; Tab would additionally mark the next field touched
                // and raise its validation error, polluting later cases.
                PressTab = false
            });

            var accepted = await pageHelper.GetFieldValue(fieldName);
            logger?.LogUiAction("Type", fieldName,
                $"Typed [{value}] ({value.Length} chars) — field kept [{accepted}] ({accepted.Length} chars)");
            return accepted;
        }

        /// <summary>Types a value, leaves the field, and returns the validation message it raised.</summary>
        public async Task<string> EnterValueAndGetError(string fieldName, string value)
        {
            await EnterValueAndLeaveField(fieldName, value);
            return await GetFieldError(fieldName);
        }

        /// <summary>The field's maxlength attribute; -1 when the field declares none.</summary>
        public async Task<int> GetFieldMaxLength(string fieldName)
        {
            var raw = await pageHelper.GetFieldAttribute(fieldName, "maxlength");
            logger?.Debug($"Field '{fieldName}' maxlength attribute: {raw ?? "<absent>"}");
            return int.TryParse(raw, out var maxLength) ? maxLength : -1;
        }

        /// <summary>
        /// Validation message currently shown for the field, or empty when it has none. The shared
        /// error-message component only renders while the field is invalid, so absence of the
        /// message element is the no-error signal.
        /// </summary>
        public async Task<string> GetFieldError(string fieldName, int timeout = 2000)
        {
            if (!await IsFieldErrorDisplayed(fieldName, timeout))
            {
                logger?.Debug($"Field '{fieldName}' shows no validation error");
                return string.Empty;
            }

            var message = await pageHelper.GetFieldValidationTextAsync(fieldName);
            logger?.Info($"Field '{fieldName}' validation message: {message}");
            return message;
        }

        public Task<bool> IsFieldErrorDisplayed(string fieldName, int timeout = 2000)
            => pageHelper.ElementExists(LocatorType.CSS, pageHelper.GetValidationLocator(fieldName), timeout);
    }
}

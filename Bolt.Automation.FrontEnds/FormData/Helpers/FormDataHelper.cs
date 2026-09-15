using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.PlaywrightBase;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.FormData.Helpers
{
    public static class FormDataHelper
    {

        public static (Dictionary<string, string> formData, Dictionary<string, UIElement> fields) GetFieldsForPage(
        object pageInstance,
        IScopeContext scopeContext,
        Dictionary<string, string>? userOverrides = null)
        {
            var fields = ProjectContextManager.GetFieldRegistry(scopeContext);
            var formData = MergeDataManager.GetSmartFormData(pageInstance, scopeContext, userOverrides, fields);
            return (formData, fields);
        }

        public static async Task FillRelevantFields(
        this IPage page,
        Dictionary<string, string>? formData,
        Dictionary<string, UIElement> fields,
        IScopeContext scopeContext,
        IPageHelper? pageHelper = null,
        IAutomationLogger? logger = null)
        {
            if (formData?.Any() != true) return;

            var helper = pageHelper is PageHelper ph ? ph : new PageHelper(page, scopeContext: scopeContext, logger: logger);
            var orderedFields = GetOrderedFields(formData, fields);

            foreach (var (fieldName, value) in orderedFields)
            {
                await ProcessField(fieldName, value, fields, formData, helper, logger);
            }

            await RetryInvalidFieldsAsync(orderedFields, fields, helper, logger);

            logger?.Debug("Completed filling relevant fields");
        }

        private static async Task RetryInvalidFieldsAsync(
            List<(string, string)> orderedFields,
            Dictionary<string, UIElement> fields,
            PageHelper helper,
            IAutomationLogger? logger)
        {
            try
            {
                var invalidIds = await helper.ValidationHelper.GetInvalidFieldIdsAsync();
                if (invalidIds.Count == 0) return;

                logger?.Warning($"Invalid fields detected after first fill pass ({invalidIds.Count}): {string.Join(", ", invalidIds.OrderBy(x => x))}");

                var retryFields = orderedFields
                    .Where(f => fields.ContainsKey(f.Item1) && invalidIds.Contains($"PolicyData.{f.Item1}"))
                    .ToList();

                var unmanagedInvalid = invalidIds
                    .Where(id => !retryFields.Any(rf => id == $"PolicyData.{rf.Item1}"))
                    .ToList();
                if (unmanagedInvalid.Count > 0)
                {
                    logger?.Warning($"Invalid fields not in registry/orderedFields (cannot retry): {string.Join(", ", unmanagedInvalid)}");
                }

                if (retryFields.Count == 0) return;

                logger?.Warning($"Retrying {retryFields.Count} invalid fields: {string.Join(", ", retryFields.Select(f => f.Item1))}");

                foreach (var (fieldName, value) in retryFields)
                {
                    var field = fields[fieldName];
                    var opts = MergeOptionsWithDefaults(field.InteractionOptions);
                    opts.IgnoreIfNotFound = false;

                    try
                    {
                        logger?.Debug($"Retrying field: {fieldName} = {value}");
                        await helper.InteractWithElement(
                            field.Strategy,
                            field.GetLocators(value),
                            field.FieldType,
                            value,
                            opts);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogException(ex, $"Retry of field '{fieldName}' failed");
                    }
                }

                var stillInvalid = await helper.ValidationHelper.GetInvalidFieldIdsAsync();
                if (stillInvalid.Count > 0)
                {
                    logger?.Error($"Fields still invalid after retry pass ({stillInvalid.Count}): {string.Join(", ", stillInvalid.OrderBy(x => x))}");
                }
                else
                {
                    logger?.Info("All previously-invalid fields resolved after retry pass");
                }
            }
            catch (Exception ex)
            {
                logger?.LogException(ex, "Validation-driven retry pass failed");
            }
        }

        private static async Task ProcessField(
        string fieldName,
        string value,
        Dictionary<string, UIElement> fields,
        Dictionary<string, string> formData,
        PageHelper helper,
        IAutomationLogger? logger)
        {
            if (!fields.TryGetValue(fieldName, out var field) || !ShouldFillField(field, formData))
            {
                logger?.Debug($"Field '{fieldName}' skipped");
                return;
            }

            try
            {
                var options = MergeOptionsWithDefaults(field.InteractionOptions);

                logger?.Debug($"Processing field: {fieldName} = {value}");
                await helper.InteractWithElement(
                    field.Strategy,
                    field.GetLocators(value),
                    field.FieldType,
                    value,
                    options);
            }
            catch (Exception ex)
            {
                logger?.LogException(ex, $"Failed to fill field '{fieldName}'");
            }
        }

        private static List<(string, string)> GetOrderedFields(
        Dictionary<string, string> formData,
        Dictionary<string, UIElement> fields)
        {
            var result = new List<(string, string)>();
            var processed = new HashSet<string>();

            foreach (var (fieldName, value) in formData)
                AddWithDependencies(fieldName, value, formData, fields, result, processed);

            return result;
        }

        private static void AddWithDependencies(
        string fieldName,
        string value,
        Dictionary<string, string> formData,
        Dictionary<string, UIElement> fields,
        List<(string, string)> result,
        HashSet<string> processed)
        {
            if (processed.Contains(fieldName)) return;

            if (fields.TryGetValue(fieldName, out var field) && !string.IsNullOrEmpty(field.DependsOn) &&
            formData.TryGetValue(field.DependsOn, out var depValue))
            {
                AddWithDependencies(field.DependsOn, depValue, formData, fields, result, processed);
            }

            result.Add((fieldName, value));
            processed.Add(fieldName);
        }

        private static bool ShouldFillField(UIElement field, Dictionary<string, string> formData)
        {
            return string.IsNullOrEmpty(field.DependsOn) || string.IsNullOrEmpty(field.DependsOnValue) ||
            (formData.TryGetValue(field.DependsOn, out var depValue) &&
            string.Equals(depValue, field.DependsOnValue, StringComparison.OrdinalIgnoreCase));
        }

        private static ElementInteractionOptions MergeOptionsWithDefaults(ElementInteractionOptions? fieldOptions)
        {
            if (fieldOptions == null)
                return new ElementInteractionOptions { IgnoreIfNotFound = true };

            return new ElementInteractionOptions
            {
                IgnoreIfNotFound = true, 
                UseSequentialTyping = fieldOptions.UseSequentialTyping,
                ForceInteractionIfNotVisible = fieldOptions.ForceInteractionIfNotVisible,
                PressTab = fieldOptions.PressTab,
                Timeout = fieldOptions.Timeout,
                ClearField = fieldOptions.ClearField
            };
        }
    }
}

using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using NUnit.Framework;

namespace Bolt.Automation.Tests.TestHelpers.Layout
{
    /// <summary>
    /// What a page must render: the text each element carries, the order the page reads top-to-bottom,
    /// and the form fields in the order they must appear. Composition test cases differ from page to
    /// page only in this data, so each page declares one of these and the checking lives once in
    /// <see cref="PageCompositionExtensions"/>. Anything a page leaves unset is simply not checked.
    /// </summary>
    internal sealed class PageComposition
    {
        /// <summary>Names the page in assertion messages, so a failure says which one broke.</summary>
        public required string PageName { get; init; }

        /// <summary>Expected text, keyed by the CSS selector of the element that carries it.</summary>
        public IReadOnlyDictionary<string, string> Texts { get; init; } = new Dictionary<string, string>();

        /// <summary>CSS selectors in the order the page must stack them, top down.</summary>
        public string[] StackedTopToBottom { get; init; } = [];

        /// <summary>Registry field names in the order the page must present them, top down.</summary>
        public string[] FieldsInOrder { get; init; } = [];
    }

    internal static class PageCompositionExtensions
    {
        /// <summary>
        /// Checks everything the composition declares and reports every deviation together, so one run
        /// tells the whole story instead of stopping at the first wrong element.
        /// </summary>
        public static async Task ShouldRender(this PageLayoutHelper layout, PageComposition expected)
        {
            var wrongText = await layout.GetTextMismatches(expected.Texts);
            var badStacking = await layout.GetStackViolations(expected.StackedTopToBottom);
            var fieldOrder = await layout.GetFieldsTopToBottom(expected.FieldsInOrder);

            Assert.Multiple(() =>
            {
                Assert.That(wrongText, Is.Empty, $"{expected.PageName} text");
                Assert.That(badStacking, Is.Empty,
                    $"{expected.PageName} should read top-to-bottom as {string.Join(" -> ", expected.StackedTopToBottom)}");
                Assert.That(fieldOrder, Is.EqualTo(expected.FieldsInOrder),
                    $"{expected.PageName} fields should read top-to-bottom as {string.Join(" -> ", expected.FieldsInOrder)}");
            });
        }
    }
}

using System.Text.Json;
using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>An element as it appeared when focus landed on it.</summary>
public sealed record FocusedElement
{
    public required string Tag { get; init; }
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Role { get; init; }
    public string? AccessibleName { get; init; }

    /// <summary>The element sits inside an aria-hidden="true" subtree but still took focus.</summary>
    public required bool InsideAriaHidden { get; init; }

    public required bool Visible { get; init; }

    /// <summary>An outline or box-shadow changes on focus. Best-effort — a CSS-only cue can evade this.</summary>
    public required bool HasFocusIndicator { get; init; }

    /// <summary>Unique DOM path — distinguishes elements a tag/id/name signature cannot (e.g. unnamed links).</summary>
    public required string Path { get; init; }

    public override string ToString() =>
        $"<{Tag}>{(Id is null ? "" : $" id='{Id}'")}{(AccessibleName is null ? "" : $" \"{AccessibleName}\"")}";
}

/// <summary>Outcome of tabbing through a screen.</summary>
public sealed record KeyboardTraversalResult
{
    public required IReadOnlyList<FocusedElement> Order { get; init; }

    /// <summary>Focus stopped advancing before the screen was exhausted.</summary>
    public required bool TrapDetected { get; init; }

    public required string? TrapSignature { get; init; }

    public IEnumerable<FocusedElement> FocusableInsideAriaHidden => Order.Where(e => e.InsideAriaHidden);
    public IEnumerable<FocusedElement> WithoutFocusIndicator => Order.Where(e => !e.HasFocusIndicator);
    public IEnumerable<FocusedElement> NotVisible => Order.Where(e => !e.Visible);
}

/// <summary>Tabs through a screen and reports what focus actually did.</summary>
public sealed class KeyboardTraversalHelper(IAutomationLogger? logger = null)
{
    private const int DefaultMaxTabs = 120;

    // Repeats mean focus is cycling or stuck; a wrapped tab ring legitimately repeats once.
    private const int RepeatsBeforeTrap = 3;

    private const string DescribeFocusScript = """
        () => {
            const el = document.activeElement;
            if (!el || el === document.body || el === document.documentElement) return null;
            const style = getComputedStyle(el);
            const labelEl = el.labels && el.labels.length ? el.labels[0] : null;
            const outlined = style.outlineStyle !== 'none' && parseFloat(style.outlineWidth || '0') > 0;
            const path = (() => {
                const parts = [];
                for (let n = el; n && n.nodeType === 1 && n !== document.body; n = n.parentElement) {
                    let i = 1;
                    for (let s = n; (s = s.previousElementSibling); ) if (s.tagName === n.tagName) i++;
                    parts.unshift(n.tagName.toLowerCase() + ':' + i);
                }
                return parts.join('>');
            })();
            return {
                path: path,
                tag: el.tagName.toLowerCase(),
                id: el.id || null,
                name: el.getAttribute('name'),
                role: el.getAttribute('role'),
                accessibleName: el.getAttribute('aria-label') || (labelEl ? labelEl.innerText.trim() : null),
                insideAriaHidden: !!el.closest('[aria-hidden="true"]'),
                visible: !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length),
                hasFocusIndicator: outlined || (style.boxShadow && style.boxShadow !== 'none')
            };
        }
        """;

    /// <summary>Tabs forward until focus stops advancing or <paramref name="maxTabs"/> is reached.</summary>
    public async Task<KeyboardTraversalResult> TraverseAsync(IPage page, int maxTabs = DefaultMaxTabs)
    {
        ArgumentNullException.ThrowIfNull(page);

        var order = new List<FocusedElement>();
        var seen = new List<string>();
        var trapSignature = (string?)null;

        // Start from the top of the document so the order is reproducible.
        await page.Locator("body").First.FocusAsync();

        for (var step = 0; step < maxTabs; step++)
        {
            await page.Keyboard.PressAsync("Tab");
            var focused = await DescribeFocusAsync(page);
            if (focused is null) continue;

            order.Add(focused);
            seen.Add(focused.Path);

            if (IsStuck(seen, focused.Path))
            {
                trapSignature = focused.Path;
                logger?.Warning($"Keyboard focus stopped advancing at {focused}. Tab order may be trapped.");
                break;
            }
        }

        logger?.Info($"Keyboard traversal reached {order.Count} control(s){(trapSignature is null ? "" : " before focus stopped advancing")}.");

        return new KeyboardTraversalResult
        {
            Order = order,
            TrapDetected = trapSignature is not null,
            TrapSignature = trapSignature
        };
    }

    /// <summary>Describes whatever currently holds focus, or null when focus is on the body.</summary>
    public async Task<FocusedElement?> DescribeFocusAsync(IPage page)
    {
        var raw = await page.EvaluateAsync<JsonElement?>(DescribeFocusScript);
        if (raw is not { ValueKind: JsonValueKind.Object } element) return null;

        return new FocusedElement
        {
            Path = GetString(element, "path") ?? "unknown",
            Tag = GetString(element, "tag") ?? "unknown",
            Id = GetString(element, "id"),
            Name = GetString(element, "name"),
            Role = GetString(element, "role"),
            AccessibleName = GetString(element, "accessibleName"),
            InsideAriaHidden = GetBool(element, "insideAriaHidden"),
            Visible = GetBool(element, "visible"),
            HasFocusIndicator = GetBool(element, "hasFocusIndicator")
        };
    }

    /// <summary>True when focus has landed on the body — the SPA route-change defect.</summary>
    public async Task<bool> IsFocusOnBodyAsync(IPage page) => await DescribeFocusAsync(page) is null;

    private static bool IsStuck(List<string> seen, string current) =>
        seen.Count >= RepeatsBeforeTrap
        && seen.TakeLast(RepeatsBeforeTrap).All(signature => signature == current);

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool GetBool(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
}

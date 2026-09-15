using System.Globalization;
using System.Text.RegularExpressions;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.CarrierSelection;

/// <summary>
/// Service for managing carrier selection operations on the rates page.
/// Optimized flow: Main Brick ? See Other Rates ? Select in Table ? Verify Main Brick
/// </summary>
public class CarrierSelectionService
{
    private readonly IPage _page;
    private readonly CarrierImageResolver _imageResolver;
    private readonly IAutomationLogger? _logger;

    private const string OtherRatesButtonLocator = "button:has-text('See other rates'), a:has-text('See other rates')";
    private const string OnlineBuyButtonLocator = "#onlineBuyBtn";
    // Scope to the DESKTOP comparison table. The Rates page renders TWO comparison tables —
    // .comparison-table.mobile-tablet (hidden at desktop viewport) and .comparison-table.desktop —
    // each with its own th.rate-head cells. A bare "th.rate-head" matches both, so iterating it can
    // land on the hidden table's Select button, whose click never resolves (30s timeout). Tests run
    // at desktop viewport, so the .desktop table is the visible/clickable one.
    private const string ComparisonTableHeaderLocator = ".comparison-table.desktop th.rate-head";

    // The selected carrier's "information statement" block on the Rates page — the paragraph plus
    // bullet list describing the underwriting carrier (e.g. American Bankers / Assurant for a
    // manufactured-home policy). Confirmed against the live QA Rates DOM: the block lives in
    // app-carrier-info-section's .rate-information (p.primary-text paragraph + div.highlights bullets).
    // The wider .rate-information (with the .lower-section fallback) captures the full statement text.
    private static readonly string[] CarrierInfoStatementLocators =
    [
        "app-carrier-info-section .rate-information",
        "app-carrier-info-section .lower-section",
        "app-carrier-info-section"
    ];

    // The Rates page shows three package cards (Basic / Popular / Deluxe); the chosen one is
    // marked ".selected". The premium lives in the selected card's ".price" node. Confirmed
    // against the live QA Rates DOM: the selected package's price === the header's
    // preferredCarrierPremium (e.g. Homesite "Standard"/Popular = 1213.00). The mobile-tablet
    // ".package" button and the desktop ".features-wrapper" section both carry ".selected".
    //
    // A single-package carrier (e.g. Nationwide) renders NO package tiles — its premium shows in
    // the single-rate card ".single-rate-wrapper .price" (confirmed live: Nationwide = 1,412). So
    // the multi-package tile locators are tried first, then the single-rate price, making the
    // reader work regardless of which carrier is selected.
    private static readonly string[] SelectedPremiumLocators =
    [
        "button.package.selected .price",
        ".features-wrapper.selected .price",
        ".single-rate-wrapper .price"
    ];
    private const int CarrierSwitchPollIterations = 20;
    private const int CarrierSwitchPollDelayMs = 250;
    private const int OnlineBuyVisibilityTimeoutMs = 2000;

    public CarrierSelectionService(IPage page, IAutomationLogger? logger = null)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _logger = logger;
        _imageResolver = new CarrierImageResolver(logger);
    }

    /// <summary>
    /// Gets the currently selected carrier name
    /// </summary>
    public async Task<string?> GetSelectedCarrierAsync()
    {
        _logger?.Debug("Resolving selected carrier");

        // Strategy 1: Check main container first (primary display)
        var mainCarrier = await GetMainContainerCarrierAsync();
        if (!string.IsNullOrWhiteSpace(mainCarrier))
        {
            _logger?.Debug($"Carrier '{mainCarrier}' found in main container");
            return mainCarrier;
        }

        // Strategy 2: Check comparison table (when "See other rates" is expanded)
        var tableCarrier = await GetSelectedCarrierFromComparisonTableAsync();
        if (!string.IsNullOrWhiteSpace(tableCarrier))
        {
            _logger?.Debug($"Carrier '{tableCarrier}' found in comparison table");
            return tableCarrier;
        }

        // Strategy 3: Fallback to descriptor text
        var descriptorCarrier = await GetCarrierFromDescriptorAsync();
        if (!string.IsNullOrWhiteSpace(descriptorCarrier))
        {
            _logger?.Debug($"Carrier '{descriptorCarrier}' found in descriptor");
            return descriptorCarrier;
        }

        _logger?.Debug("No carrier resolved by any strategy");
        return null;
    }

    /// <summary>
    /// Reads the premium displayed for the currently selected carrier on the main rate card
    /// and parses it into a decimal (currency symbols and thousands separators stripped).
    /// Throws when no price node can be resolved or the text can't be parsed — the caller is
    /// asserting on this value, so a silent null would mask a real UI regression.
    /// </summary>
    public async Task<decimal> GetSelectedCarrierPremiumAsync()
    {
        foreach (var locator in SelectedPremiumLocators)
        {
            var nodes = _page.Locator(locator);
            var count = await nodes.CountAsync();

            // The same ".selected .price" exists in both the mobile-tablet and desktop layouts;
            // only one is visible. Read the first visible one.
            for (int i = 0; i < count; i++)
            {
                var node = nodes.Nth(i);
                if (!await node.IsVisibleAsync())
                {
                    continue;
                }

                var text = (await node.InnerTextAsync())?.Trim();
                _logger?.Debug($"GetSelectedCarrierPremium: candidate '{locator}'[{i}] -> '{text}'");

                if (TryParsePremium(text, out var premium))
                {
                    _logger?.Info($"Selected package premium resolved as {premium} (from '{text}')");
                    return premium;
                }
            }
        }

        throw new PageElementException("Selected package premium",
            $"Could not resolve the selected package premium. Tried: [{string.Join(", ", SelectedPremiumLocators)}]. Page: {_page.Url}");
    }

    /// <summary>
    /// Switches the selected carrier (via the comparison table) and returns the premium
    /// displayed for the newly selected carrier on the main rate card.
    /// </summary>
    public async Task<decimal> SwitchCarrierAndGetPremiumAsync(string targetCarrier)
    {
        await EnsureCarrierSelectedAsync(targetCarrier);
        return await GetSelectedCarrierPremiumAsync();
    }

    /// <summary>
    /// Expands the "See other rates" comparison table, switches to any carrier other than the
    /// currently selected one, and returns the newly selected carrier's premium. Throws when
    /// fewer than two carriers are offered (nothing to switch to). Keeps table expansion and
    /// carrier discovery inside the service so callers don't read an un-expanded (empty) table.
    /// </summary>
    public async Task<(string Carrier, decimal Premium)> SwitchToAnyOtherCarrierAndGetPremiumAsync()
    {
        var current = await GetSelectedCarrierAsync();
        await EnsureComparisonTableExpandedAsync(failIfTriggerMissing: true, contextMessage: "switch to a different carrier");

        var available = await GetAvailableCarriersAsync();
        var target = available.FirstOrDefault(c => !string.Equals(c, current, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(target))
        {
            throw new CarrierNotFoundException("(any other carrier)",
                $"only one carrier offered (current '{current ?? "(none)"}', available [{string.Join(", ", available)}])");
        }

        _logger?.Info($"Switching carrier from '{current ?? "(none)"}' to '{target}'");
        var premium = await SwitchCarrierAndGetPremiumAsync(target);
        return (target, premium);
    }

    // The multi-package tiles are <li> items inside <ul.multi-packages> — each <li> carries an
    // aria-label like "Basic1845.49$/yr." / "Popular1911.38$/yr. selected. " / "Deluxe2105.83$/yr.".
    // Bare CSS (.features-wrapper / button.package) doesn't resolve them, but the <li> under the
    // <ul> does; the selected tier's aria-label contains "selected". The clickable control is the
    // ".radio-button" inside each <li>.
    private const string PackageTileLocator = "ul.multi-packages li";
    private static readonly string[] PackageTierNames = ["Basic", "Popular", "Deluxe"];

    /// <summary>
    /// Returns the package tier names currently offered by the selected carrier (e.g. Basic,
    /// Popular, Deluxe). Empty for a single-package carrier that renders no tiles.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAvailablePackageTiersAsync()
    {
        var tiles = _page.Locator(PackageTileLocator);
        var count = await tiles.CountAsync();
        var tiers = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var aria = await tiles.Nth(i).GetAttributeAsync("aria-label") ?? string.Empty;
            var tier = PackageTierNames.FirstOrDefault(t => aria.StartsWith(t, StringComparison.OrdinalIgnoreCase));
            if (tier is not null && !tiers.Contains(tier))
            {
                tiers.Add(tier);
            }
        }

        _logger?.Debug($"Available package tiers: [{string.Join(", ", tiers)}]");
        return tiers;
    }

    /// <summary>
    /// Returns the tier name of the currently selected package (aria-label contains "selected"),
    /// or null if no multi-package tiles are shown.
    /// </summary>
    public async Task<string?> GetSelectedPackageTierAsync()
    {
        var tiles = _page.Locator(PackageTileLocator);
        var count = await tiles.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var aria = await tiles.Nth(i).GetAttributeAsync("aria-label") ?? string.Empty;
            if (aria.Contains("selected", StringComparison.OrdinalIgnoreCase))
            {
                return PackageTierNames.FirstOrDefault(t => aria.StartsWith(t, StringComparison.OrdinalIgnoreCase));
            }
        }

        return null;
    }

    /// <summary>
    /// Selects the given package tier (Basic / Popular / Deluxe) on the current carrier by clicking
    /// its tile's radio control, waits for the selection to move to it, and returns the tier's
    /// premium. Throws if the tier isn't offered or the selection doesn't take.
    /// </summary>
    public async Task<decimal> SelectPackageTierAndGetPremiumAsync(string tier)
    {
        var tiles = _page.Locator(PackageTileLocator);
        var count = await tiles.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var tile = tiles.Nth(i);
            var aria = await tile.GetAttributeAsync("aria-label") ?? string.Empty;
            if (!aria.StartsWith(tier, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (aria.Contains("selected", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.Info($"Package tier '{tier}' is already selected");
                return await GetSelectedCarrierPremiumAsync();
            }

            _logger?.Info($"Selecting package tier '{tier}'");
            var radio = tile.Locator(".radio-button");
            await (await radio.CountAsync() > 0 ? radio.First : tile).ClickAsync();

            // Wait for this tile's aria-label to gain "selected".
            for (int poll = 0; poll < CarrierSwitchPollIterations; poll++)
            {
                await Task.Delay(CarrierSwitchPollDelayMs);
                var refreshed = await tile.GetAttributeAsync("aria-label") ?? string.Empty;
                if (refreshed.Contains("selected", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.Info($"Package tier '{tier}' selection confirmed");
                    return await GetSelectedCarrierPremiumAsync();
                }
            }

            throw new PageElementException("Package tier selection",
                $"Clicked package tier '{tier}' but its tile did not become selected. Page: {_page.Url}");
        }

        var offered = await GetAvailablePackageTiersAsync();
        throw new CarrierNotFoundException(tier,
            $"package tier not offered by the selected carrier (available: [{string.Join(", ", offered)}])");
    }

    /// <summary>
    /// Selects any package tier other than the currently selected one and returns its premium.
    /// Requires the selected carrier to offer at least two tiers.
    /// </summary>
    public async Task<decimal> SelectDifferentPackageTierAndGetPremiumAsync()
    {
        var current = await GetSelectedPackageTierAsync();
        var tiers = await GetAvailablePackageTiersAsync();
        var target = tiers.FirstOrDefault(t => !string.Equals(t, current, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(target))
        {
            throw new CarrierNotFoundException("(a different package tier)",
                $"the selected carrier offers fewer than two tiers (current '{current ?? "(none)"}', available [{string.Join(", ", tiers)}])");
        }

        return await SelectPackageTierAndGetPremiumAsync(target);
    }

    /// <summary>
    /// Parses a displayed price such as "$1,243.42", "$1,243.42/yr" or "1243.42" into a decimal.
    /// </summary>
    private static bool TryParsePremium(string? text, out decimal premium)
    {
        premium = 0m;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Keep the first number-like token (digits, thousands separators, decimal point).
        var match = Regex.Match(text, @"[\d][\d,]*(\.\d+)?");
        if (!match.Success)
        {
            return false;
        }

        var cleaned = match.Value.Replace(",", string.Empty);
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out premium);
    }

    /// <summary>
    /// Gets all available carriers in the comparison table
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAvailableCarriersAsync()
    {
        var headers = _page.Locator(ComparisonTableHeaderLocator);
        var list = new List<string>();
        var count = await headers.CountAsync();

        _logger?.Debug($"Found {count} carrier header(s) in comparison table");

        for (int i = 0; i < count; i++)
        {
            var resolved = await ResolveCarrierFromHeaderAsync(headers.Nth(i));
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                list.Add(resolved);
            }
        }

        var distinctCarriers = list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _logger?.Debug($"Available carriers: [{string.Join(", ", distinctCarriers)}]");

        return distinctCarriers;
    }

    /// <summary>
    /// Ensures the specified carrier is selected, switching if necessary.
    /// Flow: Check Main Brick ? Expand "See Other Rates" ? Select from Table ? Verify Main Brick
    /// </summary>
    public async Task<bool> EnsureCarrierSelectedAsync(string expectedCarrier)
    {
        expectedCarrier = CarrierImageResolver.NormalizeCarrier(expectedCarrier ?? throw new ArgumentNullException(nameof(expectedCarrier)));
        _logger?.Info($"Starting carrier selection process for '{expectedCarrier}'");

        // Step 1: Check if carrier is already displayed on main brick, polling briefly since a fresh
        // navigation (e.g. close/reopen via QuoteRetrieval) can land on the URL before the DOM hydrates.
        var mainCarrier = await GetSelectedCarrierAsync();
        for (int poll = 0; string.IsNullOrWhiteSpace(mainCarrier) && poll < CarrierSwitchPollIterations; poll++)
        {
            await Task.Delay(CarrierSwitchPollDelayMs);
            mainCarrier = await GetSelectedCarrierAsync();
        }
        _logger?.Debug($"Main brick carrier: '{mainCarrier ?? "(none)"}'");

        if (CarrierMatches(mainCarrier, expectedCarrier))
        {
            _logger?.Info($"Carrier '{expectedCarrier}' is already displayed on main brick - no action needed");
            return true;
        }

        _logger?.Info($"Carrier '{expectedCarrier}' not on main brick. Current: '{mainCarrier ?? "(none)"}'. Checking 'See other rates'...");

        // Step 2-3: Ensure comparison table is expanded (clicks "See other rates" if needed)
        await EnsureComparisonTableExpandedAsync(failIfTriggerMissing: true, contextMessage: $"select carrier '{expectedCarrier}'");

        // Step 4: Verify carrier is available in comparison table
        var availableCarriers = await GetAvailableCarriersAsync();
        if (!availableCarriers.Any(c => CarrierMatches(c, expectedCarrier)))
        {
            _logger?.Error($"Carrier '{expectedCarrier}' not found in comparison table. Available: [{string.Join(", ", availableCarriers)}]");
            throw new CarrierNotFoundException(expectedCarrier, "comparison table", availableCarriers);
        }

        _logger?.Info($"Carrier '{expectedCarrier}' found in comparison table - attempting selection");

        // Step 5: Select the carrier from comparison table
        if (!await TrySelectCarrierInTableAsync(expectedCarrier))
        {
            _logger?.Error($"Failed to select carrier '{expectedCarrier}' in comparison table");
            throw new CarrierNotFoundException(expectedCarrier, "comparison table (selection click failed)");
        }

        // Step 6: Verify carrier is now displayed on main brick
        var finalMainCarrier = await GetSelectedCarrierAsync();
        _logger?.Debug($"Main brick carrier after selection: '{finalMainCarrier ?? "(none)"}'");

        if (!CarrierMatches(finalMainCarrier, expectedCarrier))
        {
            _logger?.Error($"Verification failed - expected '{expectedCarrier}' on main brick, but got '{finalMainCarrier ?? "(none)"}'");
            throw new CarrierNotFoundException(expectedCarrier, $"main brick after selection (got '{finalMainCarrier ?? "(none)"}')");
        }

        _logger?.Info($"Successfully selected and verified carrier '{expectedCarrier}' on main brick");
        return true;
    }

    /// <summary>
    /// Returns true when the currently selected carrier exposes a real online buy link
    /// (i.e. <c>#onlineBuyBtn</c> exists, is visible, and is NOT a <c>tel:</c> phone link —
    /// non-online-buy carriers render the same id as a phone CTA).
    /// </summary>
    public async Task<bool> IsOnlineBuyAvailableAsync(int timeoutMs = OnlineBuyVisibilityTimeoutMs)
    {
        var button = _page.Locator(OnlineBuyButtonLocator);
        try
        {
            await button.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });
        }
        catch
        {
            return false;
        }

        var href = await button.GetAttributeAsync("href");
        return string.IsNullOrEmpty(href)
            || !href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Expands the "See other rates" comparison table if not already visible.
    /// </summary>
    /// <param name="failIfTriggerMissing">When true, throws if the trigger isn't on the page; when false, returns silently.</param>
    /// <param name="contextMessage">Free-form context appended to the thrown exception, e.g. the operation that needed the table.</param>
    public async Task EnsureComparisonTableExpandedAsync(bool failIfTriggerMissing = false, string? contextMessage = null)
    {
        var headers = _page.Locator(ComparisonTableHeaderLocator);
        if (await headers.CountAsync() > 0)
        {
            _logger?.Debug("Comparison table already visible");
            return;
        }

        var trigger = _page.Locator(OtherRatesButtonLocator);
        var triggerCount = await trigger.CountAsync();
        if (triggerCount == 0)
        {
            if (failIfTriggerMissing)
            {
                _logger?.Error("'See other rates' button not found - cannot expand comparison table");
                throw new PageElementException("'See other rates' button",
                    contextMessage is null ? "Required to expand the comparison table." : $"Required to {contextMessage}.");
            }
            _logger?.Debug("'See other rates' trigger not present - comparison table cannot be expanded");
            return;
        }

        _logger?.Info("Expanding 'See other rates' comparison table");
        try
        {
            await trigger.First.ClickAsync();
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to click 'See other rates' button: {ex.Message}");
        }

        for (int i = 0; i < 15 && await headers.CountAsync() == 0; i++)
        {
            await Task.Delay(150);
        }

        if (await headers.CountAsync() == 0)
        {
            _logger?.Error("Comparison table failed to appear after clicking 'See other rates'");
            throw new PageElementException("Comparison table", "Did not appear after clicking 'See other rates'.");
        }
    }

    /// <summary>
    /// Ensures a carrier that supports online buy is selected. If the current carrier on the main
    /// brick already has the online buy link, no action is taken. Otherwise the comparison table is
    /// expanded and each <paramref name="preferredOrder"/> carrier is tried in turn (then any
    /// remaining available carrier) until one exposes the online buy link. Returns that carrier's name.
    /// </summary>
    public async Task<string> EnsureOnlineBuyCarrierSelectedAsync(IEnumerable<string> preferredOrder)
    {
        if (await IsOnlineBuyAvailableAsync())
        {
            var current = await GetSelectedCarrierAsync();
            _logger?.Info($"Online buy already available with current carrier '{current ?? "(unknown)"}'");
            return current ?? string.Empty;
        }

        await EnsureComparisonTableExpandedAsync(failIfTriggerMissing: true, contextMessage: "find an online-buy-capable carrier");

        var available = await GetAvailableCarriersAsync();
        if (available.Count == 0)
        {
            throw new PageElementException("Online Buy button",
                "Comparison table has no carriers - cannot find an online-buy-capable carrier.");
        }

        var preferred = preferredOrder as string[] ?? [.. preferredOrder];
        var ordered = preferred
            .Where(p => available.Any(a => string.Equals(a, p, StringComparison.OrdinalIgnoreCase)))
            .Concat(available.Where(a => !preferred.Any(p => string.Equals(p, a, StringComparison.OrdinalIgnoreCase))))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var carrier in ordered)
        {
            _logger?.Info($"Trying carrier '{carrier}' for online buy availability");
            await EnsureCarrierSelectedAsync(carrier);
            if (await IsOnlineBuyAvailableAsync())
            {
                _logger?.Info($"Carrier '{carrier}' supports online buy - selection complete");
                return carrier;
            }
        }

        throw new PageElementException("Online Buy button",
            $"None of the available carriers [{string.Join(", ", available)}] exposed an online buy link.");
    }

    /// <summary>
    /// Reads the selected carrier's "information statement" block on the Rates page — the paragraph
    /// and bullet points describing the underwriting carrier (e.g. American Bankers / Assurant for a
    /// manufactured-home policy). Returns the block's visible text with normalized whitespace, or
    /// throws when no statement block can be resolved (the caller asserts on this, so a silent null
    /// would mask a real UI regression).
    /// </summary>
    public async Task<string> GetCarrierInformationStatementAsync()
    {
        foreach (var locator in CarrierInfoStatementLocators)
        {
            var nodes = _page.Locator(locator);
            var count = await nodes.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var node = nodes.Nth(i);
                if (!await node.IsVisibleAsync())
                {
                    continue;
                }

                var text = NormalizeWhitespace(await node.InnerTextAsync());
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _logger?.Info($"Carrier information statement resolved from '{locator}'[{i}] ({text.Length} chars)");
                    _logger?.Info($"Carrier information statement text: {text}");
                    return text;
                }
            }
        }

        throw new PageElementException("Carrier information statement",
            $"Could not resolve the carrier information statement. Tried: [{string.Join(", ", CarrierInfoStatementLocators)}]. Page: {_page.Url}");
    }

    /// <summary>
    /// Collapses runs of whitespace (including newlines) to single spaces and trims, so that a
    /// multi-line rendered statement compares cleanly against an expected single-string value.
    /// </summary>
    private static string NormalizeWhitespace(string? text) =>
        string.IsNullOrWhiteSpace(text) ? string.Empty : Regex.Replace(text, @"\s+", " ").Trim();

    // Bamboo/BambooSurplus and Foremost/ForemostSignature are sibling carriers where one name is a
    // superset string of the other, so those stay exact-only; others fall back to a contains check.
    private static readonly HashSet<string> AmbiguousShortCarrierNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bamboo", "foremost"
    };

    private static bool CarrierMatches(string? actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(actual)) return false;
        if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) return true;
        if (AmbiguousShortCarrierNames.Contains(expected)) return false;
        return actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    #region Private Helper Methods

    private async Task<string?> GetMainContainerCarrierAsync()
    {
        // Check for image-based logos (standard pattern)
        var imgs = _page.Locator("#top-container .logo img.carrier-logo[src*='carrier=']");
        var count = await imgs.CountAsync();

        if (count > 0)
        {
            _logger?.Debug($"GetMainContainerCarrier: Found {count} image carrier logo(s) in main container");
            for (int i = 0; i < count; i++)
            {
                var resolved = await _imageResolver.ResolveFromImageAsync(imgs.Nth(i));
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    return resolved;
                }
            }
        }

        // Check for component-based logos (e.g. SVG logos)
        var appLogo = _page.Locator("app-logo");
        if (await appLogo.CountAsync() > 0)
        {
            // Evaluate on the page to find child elements of app-logo that start with 'logo-'
            var carrierComponentTag = await appLogo.First.EvaluateAsync<string>(@"el => {
                const child = el.firstElementChild;
                return child ? child.tagName.toLowerCase() : '';
            }");

            if (!string.IsNullOrEmpty(carrierComponentTag) && carrierComponentTag.StartsWith("logo-"))
            {
                // Extract carrier name from tag: logo-plymouth-rock -> plymouth-rock, then normalize
                // through the same canonicalizer the comparison-table header uses so the two sides
                // compare equal for the same carrier (esp. cobranded ones like Stillwater).
                var rawCarrier = carrierComponentTag.Replace("logo-", "", StringComparison.OrdinalIgnoreCase);
                var normalized = CarrierImageResolver.NormalizeCarrier(rawCarrier);

                _logger?.Debug($"GetMainContainerCarrier: Resolved '{normalized}' from component tag '{carrierComponentTag}'");
                return normalized;
            }
        }

        return null;
    }

    private async Task<string?> GetSelectedCarrierFromComparisonTableAsync()
    {
        var selectedHeader = _page.Locator($"{ComparisonTableHeaderLocator}.selected");
        var count = await selectedHeader.CountAsync();

        if (count == 0)
        {
            _logger?.Debug("GetSelectedCarrierFromTable: No selected header found");
            return null;
        }

        return await ResolveCarrierFromHeaderAsync(selectedHeader.First);
    }

    private async Task<string?> GetCarrierFromDescriptorAsync()
    {
        const string descriptorLocator = "div.underwritten, div.underwritten.redesign";
        var descriptor = _page.Locator(descriptorLocator);
        var count = await descriptor.CountAsync();

        if (count == 0)
        {
            _logger?.Debug("GetCarrierFromDescriptor: No descriptor element found");
            return null;
        }

        var text = (await descriptor.First.TextContentAsync())?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            _logger?.Debug("GetCarrierFromDescriptor: Descriptor text is empty");
            return null;
        }

        var idx = text.LastIndexOf(" by ", StringComparison.OrdinalIgnoreCase);
        var carrier = idx >= 0 ? text[(idx + 4)..].Trim().TrimEnd('.') : text;

        _logger?.Debug($"GetCarrierFromDescriptor: Resolved '{carrier}' from text '{text}'");
        return carrier;
    }

    private async Task<bool> TrySelectCarrierInTableAsync(string expectedCarrier)
    {
        _logger?.Debug($"TrySelectCarrierInTable: Looking for carrier '{expectedCarrier}'");
        var headerCells = _page.Locator(ComparisonTableHeaderLocator);
        var count = await headerCells.CountAsync();
        _logger?.Debug($"Found {count} header cell(s) total");

        for (int i = 0; i < count; i++)
        {
            _logger?.Debug($"Checking header cell {i + 1}/{count}");
            var cell = headerCells.Nth(i);

            var cellCarrier = await ResolveCarrierFromHeaderAsync(cell);
            if (!CarrierMatches(cellCarrier, expectedCarrier))
            {
                _logger?.Debug($"Cell {i + 1} carrier '{cellCarrier ?? "(none)"}' does not match '{expectedCarrier}' - skipping");
                continue;
            }

            // Check if already selected
            if (await cell.Locator("button.btn:has-text('Selected')").CountAsync() > 0)
            {
                _logger?.Info($"Carrier already marked as 'Selected' in cell {i + 1}");
                return true;
            }

            // Find and click the select button. Match the text EXACTLY — a :has-text('Select')
            // substring match also matches the already-selected "Selected" button, which is a no-op
            // control and would waste the click (and its 30s timeout) on the wrong element.
            var selectBtn = cell.GetByRole(AriaRole.Button, new() { Name = "Select", Exact = true });
            if (await selectBtn.CountAsync() == 0)
            {
                _logger?.Debug($"No 'Select' button in cell {i + 1} - skipping");
                continue;
            }

            _logger?.Debug($"Clicking 'Select' button in cell {i + 1}");
            try
            {
                await selectBtn.First.ClickAsync();
                _logger?.Debug("Clicked 'Select' button successfully");
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to click 'Select' button: {ex.Message}");
                continue;
            }

            // Poll for carrier switch confirmation via the comparison table's own selected header.
            // The main rate card can't be used here: for cobranded carriers (e.g. Stillwater) its
            // app-logo renders as an inline styled SVG (logo-dark) with no carrier-identifying tag or
            // title, so GetMainContainerCarrierAsync resolves it to a non-carrier value and the poll
            // never matches. The table header carries a resolvable SVG title for every carrier.
            _logger?.Debug($"Polling for carrier switch (max {CarrierSwitchPollIterations} x {CarrierSwitchPollDelayMs}ms)");
            for (int poll = 0; poll < CarrierSwitchPollIterations; poll++)
            {
                await Task.Delay(CarrierSwitchPollDelayMs);
                var currentSelected = await GetSelectedCarrierFromComparisonTableAsync();

                if (poll % 5 == 0 && poll > 0)
                {
                    _logger?.Debug($"Polling attempt {poll + 1}/{CarrierSwitchPollIterations}, table selection: '{currentSelected ?? "(none)"}'");
                }

                if (CarrierMatches(currentSelected, expectedCarrier))
                {
                    _logger?.Info($"Carrier switch confirmed in comparison table after {(poll + 1) * CarrierSwitchPollDelayMs}ms");
                    return true;
                }
            }

            _logger?.Warning($"Carrier switch not confirmed after {CarrierSwitchPollIterations * CarrierSwitchPollDelayMs}ms");
        }

        _logger?.Error($"Failed to select carrier after checking all {count} cells");
        return false;
    }

    /// <summary>
    /// Resolves the carrier name from a th.rate-head element.
    /// Handles SVG-based logos (cobranded) as well as legacy img-based logos.
    /// </summary>
    private async Task<string?> ResolveCarrierFromHeaderAsync(ILocator header)
    {
        // Strategy 1: SVG title attribute e.g. "Progressive by Homesite Logo"
        var svg = header.Locator("svg[title]");
        if (await svg.CountAsync() > 0)
        {
            var title = await svg.First.GetAttributeAsync("title");
            if (!string.IsNullOrEmpty(title))
            {
                var parsed = ParseCarrierFromSvgTitle(title);
                if (!string.IsNullOrWhiteSpace(parsed))
                {
                    _logger?.Debug($"ResolveCarrierFromHeader: '{parsed}' from SVG title '{title}'");
                    return CarrierImageResolver.NormalizeCarrier(parsed);
                }
            }
        }

        // Strategy 2: app-logo child component tag e.g. logo-homesite -> homesite
        var appLogo = header.Locator("app-logo");
        if (await appLogo.CountAsync() > 0)
        {
            var tag = await appLogo.First.EvaluateAsync<string>(@"el => {
                const child = el.firstElementChild;
                return child ? child.tagName.toLowerCase() : '';
            }");
            if (!string.IsNullOrEmpty(tag) && tag.StartsWith("logo-"))
            {
                var carrier = tag.Replace("logo-", "", StringComparison.OrdinalIgnoreCase).Replace("-", "");
                _logger?.Debug($"ResolveCarrierFromHeader: '{carrier}' from component tag '{tag}'");
                return CarrierImageResolver.NormalizeCarrier(carrier);
            }
        }

        // Strategy 3: Legacy img-based logo
        var img = header.Locator("img[class*='carrier-logo']");
        if (await img.CountAsync() > 0)
        {
            return await _imageResolver.ResolveFromImageAsync(img.First);
        }

        _logger?.Debug("ResolveCarrierFromHeader: Could not resolve carrier");
        return null;
    }

    private static string? ParseCarrierFromSvgTitle(string title)
    {
        // "Progressive by Homesite Logo" -> "Homesite"
        const string byMarker = " by ";
        const string logoSuffix = " Logo";
        var byIdx = title.IndexOf(byMarker, StringComparison.OrdinalIgnoreCase);
        if (byIdx < 0) return null;
        var start = byIdx + byMarker.Length;
        var end = title.IndexOf(logoSuffix, start, StringComparison.OrdinalIgnoreCase);
        return (end > start ? title[start..end] : title[start..]).Trim();
    }

    #endregion
}

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bolt.Automation.AgentTools.ApiClients;

public sealed record CachedTc(TestCase TestCase, DateTime CachedAtUtc);

public sealed record TcCacheEntry(int Id, string? Title, string? Partner, string CachedAt, long Bytes);

/// <summary>
/// Local disk cache for fetched test cases at
/// <c>%LOCALAPPDATA%\nexus-agent\tc\&lt;id&gt;.json</c>. The stored shape is the
/// normalized <see cref="TestCase"/> (snake_case), so cache reads emit an
/// identical record to fresh fetches. Cache age uses the file mtime.
/// </summary>
internal sealed class TcCache
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nexus-agent", "tc");

    private string PathFor(int id) => Path.Combine(Dir, $"{id}.json");

    public CachedTc? TryGet(int id)
    {
        var path = PathFor(id);
        if (!File.Exists(path)) return null;
        try
        {
            var tc = JsonSerializer.Deserialize<TestCase>(File.ReadAllText(path), Json);
            return tc == null ? null : new CachedTc(tc, File.GetLastWriteTimeUtc(path));
        }
        catch
        {
            return null; // corrupt cache entry — treat as a miss.
        }
    }

    public void Save(int id, TestCase testCase)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(PathFor(id), JsonSerializer.Serialize(testCase, Json));
    }

    public IReadOnlyList<TcCacheEntry> List()
    {
        if (!Directory.Exists(Dir)) return Array.Empty<TcCacheEntry>();

        var entries = new List<TcCacheEntry>();
        foreach (var path in Directory.EnumerateFiles(Dir, "*.json"))
        {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(path), out var id)) continue;
            var fi = new FileInfo(path);
            string? title = null, partner = null;
            try
            {
                var tc = JsonSerializer.Deserialize<TestCase>(File.ReadAllText(path), Json);
                title = tc?.Title;
                partner = tc?.Partner;
            }
            catch { /* show the entry even if unreadable */ }

            entries.Add(new TcCacheEntry(id, title, partner, fi.LastWriteTimeUtc.ToString("o"), fi.Length));
        }
        return entries.OrderBy(e => e.Id).ToList();
    }

    /// <summary>Returns (removed, remaining).</summary>
    public (int Removed, int Remaining) Clear(TimeSpan? olderThan)
    {
        if (!Directory.Exists(Dir)) return (0, 0);

        var cutoffUtc = olderThan.HasValue ? DateTime.UtcNow - olderThan.Value : (DateTime?)null;
        var removed = 0;
        var remaining = 0;
        foreach (var path in Directory.EnumerateFiles(Dir, "*.json"))
        {
            if (cutoffUtc.HasValue && File.GetLastWriteTimeUtc(path) > cutoffUtc.Value)
            {
                remaining++;
                continue;
            }
            try { File.Delete(path); removed++; }
            catch { remaining++; }
        }
        return (removed, remaining);
    }

    /// <summary>Parses durations like "7d", "24h", "30m", "90s". Null/empty → null (clear all).</summary>
    public static bool TryParseDuration(string? text, out TimeSpan? duration)
    {
        duration = null;
        if (string.IsNullOrWhiteSpace(text)) return true; // no filter = clear everything

        var m = Regex.Match(text.Trim(), @"^(\d+)\s*([smhd])$", RegexOptions.IgnoreCase);
        if (!m.Success) return false;

        // long + cap: a cache age beyond ~a million of any unit is absurd, and an
        // unguarded int.Parse (or TimeSpan.FromX on a huge value) throws OverflowException.
        if (!long.TryParse(m.Groups[1].Value, out var n) || n is < 0 or > 1_000_000)
            return false;
        duration = m.Groups[2].Value.ToLowerInvariant() switch
        {
            "s" => TimeSpan.FromSeconds(n),
            "m" => TimeSpan.FromMinutes(n),
            "h" => TimeSpan.FromHours(n),
            "d" => TimeSpan.FromDays(n),
            _ => null,
        };
        return duration != null;
    }
}

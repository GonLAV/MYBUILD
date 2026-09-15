using System.Text.RegularExpressions;

namespace Bolt.Automation.AgentTools.Code;

public sealed record ConsumerHit(string File, int Count);

public sealed record ChangedFileImpact(
    string File,
    IReadOnlyList<string> Symbols,
    IReadOnlyList<ConsumerHit> Consumers,
    int ConsumerFileCount,
    IReadOnlyList<string> PhilosophyAreas);

public sealed record DiffImpactResult(
    string Ref,
    int ChangedFileCount,
    IReadOnlyList<ChangedFileImpact> Changes);

/// <summary>
/// v1 grep-based change-impact (Roslyn is a v2 upgrade behind IConsumerFinder).
/// For each changed .cs file: derive candidate type symbols, grep the repo for
/// consumers of each, and map the file path to philosophy areas. ~80% of the
/// value at a fraction of the setup cost.
/// </summary>
internal sealed class DiffImpactAnalyzer
{
    private static readonly Regex TypeDecl = new(
        @"\b(?:class|interface|enum|record|struct)\s+([A-Z][A-Za-z0-9_]+)",
        RegexOptions.Compiled);

    private readonly string _repoRoot;
    private readonly GrepRunner _grep;

    public DiffImpactAnalyzer(string repoRoot)
    {
        _repoRoot = repoRoot;
        _grep = new GrepRunner(repoRoot);
    }

    public DiffImpactResult? Analyze(string gitRef, out string? error)
    {
        error = null;
        var diff = ProcessRunner.Run("git", ["-C", _repoRoot, "diff", "--name-only", gitRef, "--", "*.cs"], _repoRoot, 30_000);
        if (!diff.Started) { error = "git not found."; return null; }
        if (diff.ExitCode != 0) { error = diff.StdErr.Trim().Length > 0 ? diff.StdErr.Trim() : $"git diff failed (ref '{gitRef}')."; return null; }

        var changedFiles = diff.StdOut
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Symbols per changed file.
        var perFileSymbols = changedFiles.ToDictionary(f => f, ExtractSymbols, StringComparer.OrdinalIgnoreCase);

        // ONE ripgrep across the union of all symbols (Windows process spawns are
        // expensive — a call per symbol times out on a real diff). Attribute hits
        // back to each file's symbols in-process.
        var allSymbols = perFileSymbols.Values.SelectMany(s => s)
            .Distinct(StringComparer.Ordinal).ToList();
        var allHits = allSymbols.Count == 0
            ? new List<GrepHit>()
            : _grep.Search(BuildAlternation(allSymbols), maxCount: 5000).ToList();

        var changes = new List<ChangedFileImpact>();
        foreach (var file in changedFiles)
        {
            var symbols = perFileSymbols[file];
            var consumers = AttributeConsumers(symbols, file, allHits);
            changes.Add(new ChangedFileImpact(
                File: file,
                Symbols: symbols,
                Consumers: consumers.Take(10).ToList(),
                ConsumerFileCount: consumers.Count,
                PhilosophyAreas: PhilosophyAreaMap.AreasFor(file)));
        }

        return new DiffImpactResult(gitRef, changedFiles.Count, changes);
    }

    private static string BuildAlternation(IReadOnlyList<string> symbols)
        => @"\b(" + string.Join("|", symbols.Select(Regex.Escape)) + @")\b";

    private static List<ConsumerHit> AttributeConsumers(IReadOnlyList<string> symbols, string selfFile, List<GrepHit> allHits)
    {
        if (symbols.Count == 0) return new List<ConsumerHit>();
        var symRx = new Regex(BuildAlternation(symbols), RegexOptions.Compiled);

        var perFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var hit in allHits)
        {
            if (string.Equals(hit.File, selfFile, StringComparison.OrdinalIgnoreCase)) continue;
            if (!symRx.IsMatch(hit.Text)) continue;
            perFile[hit.File] = perFile.GetValueOrDefault(hit.File) + 1;
        }
        return perFile
            .Select(kv => new ConsumerHit(kv.Key, kv.Value))
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.File, StringComparer.Ordinal)
            .ToList();
    }

    private List<string> ExtractSymbols(string repoRelFile)
    {
        var symbols = new List<string>();
        var abs = Path.Combine(_repoRoot, repoRelFile.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(abs))
        {
            try
            {
                foreach (Match m in TypeDecl.Matches(File.ReadAllText(abs)))
                {
                    var name = m.Groups[1].Value;
                    if (!symbols.Contains(name)) symbols.Add(name);
                }
            }
            catch { /* unreadable — fall through to filename */ }
        }
        // Fall back to the filename stem if no declarations parsed (or file deleted).
        if (symbols.Count == 0)
        {
            var stem = Path.GetFileNameWithoutExtension(repoRelFile);
            if (stem.Length > 0) symbols.Add(stem);
        }
        return symbols;
    }

}

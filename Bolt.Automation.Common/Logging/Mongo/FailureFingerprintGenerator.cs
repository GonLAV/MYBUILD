using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bolt.Automation.Common.Logging.Mongo;

public static partial class FailureFingerprintGenerator
{
    public const string AssertionFailureType = "AssertionFailure";

    public static string Generate(string? exceptionType, string? stackTrace, string? message = null)
    {
        if (exceptionType == AssertionFailureType && !string.IsNullOrEmpty(message))
        {
            var template = NormalizeAssertionMessage(message);
            var input = $"{AssertionFailureType}|{template}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexStringLower(hash);
        }

        if (string.IsNullOrEmpty(stackTrace))
            return string.Empty;

        var normalized = NormalizeStackTrace(stackTrace);
        var stackInput = $"{exceptionType ?? "Unknown"}|{normalized}";
        var stackHash = SHA256.HashData(Encoding.UTF8.GetBytes(stackInput));
        return Convert.ToHexStringLower(stackHash);
    }

    public static string ExtractExceptionType(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return AssertionFailureType;

        var match = _exceptionTypeRegex.Match(message);
        return match.Success ? match.Groups[1].Value : AssertionFailureType;
    }

    private static string NormalizeStackTrace(string stackTrace)
    {
        var result = LineNumberRegex().Replace(stackTrace, "");
        result = GuidRegex().Replace(result, "<GUID>");
        result = DynamicIdRegex().Replace(result, "<ID>");
        result = WhitespaceRegex().Replace(result.Trim(), " ");
        return result;
    }

    private static string NormalizeAssertionMessage(string message)
    {
        var result = _objectReprRegex.Replace(message, "<obj>");
        result = GuidRegex().Replace(result, "<guid>");
        result = _dateTimeRegex.Replace(result, "<datetime>");
        result = _quotedStringRegex.Replace(result, "'*'");
        result = _numberRegex.Replace(result, "<n>");
        result = WhitespaceRegex().Replace(result.Trim(), " ");
        return result.Length > 300 ? result[..300] : result;
    }

    [GeneratedRegex(@":line \d+")]
    private static partial Regex LineNumberRegex();

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"\b\d{6,}\b")]
    private static partial Regex DynamicIdRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private static readonly Regex _exceptionTypeRegex  = new(@"^(\w*Exception)\s*:",             RegexOptions.Compiled);
    private static readonly Regex _objectReprRegex     = new(@"<[^>]{1,150}>",                   RegexOptions.Compiled);
    private static readonly Regex _dateTimeRegex       = new(@"\d{1,2}/\d{1,2}/\d{4}\s+\d{2}:\d{2}:\d{2}", RegexOptions.Compiled);
    private static readonly Regex _quotedStringRegex   = new(@"'[^']{1,80}'",                    RegexOptions.Compiled);
    private static readonly Regex _numberRegex         = new(@"\b\d+\b",                        RegexOptions.Compiled);
}

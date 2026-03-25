using System.Text.RegularExpressions;

namespace CU_TestHarness.Models;

public class FieldComparison
{
    public string FieldName { get; set; } = string.Empty;
    public string? CuFieldName { get; set; }
    public string? DiFieldName { get; set; }
    public string? CuValue { get; set; }
    public double? CuConfidence { get; set; }
    public string? DiValue { get; set; }
    public double? DiConfidence { get; set; }

    /// <summary>How the fields were matched: Exact, Normalized, Fuzzy, or None (single-source)</summary>
    public MatchQuality MatchQuality { get; set; } = MatchQuality.None;

    public bool ValuesMatch => string.Equals(CuValue?.Trim(), DiValue?.Trim(), StringComparison.OrdinalIgnoreCase);
    public double? ConfidenceDelta => (CuConfidence.HasValue && DiConfidence.HasValue)
        ? CuConfidence.Value - DiConfidence.Value
        : null;

    /// <summary>CU-only, DI-only, or Both</summary>
    public string Source =>
        (CuValue is not null && DiValue is not null) ? "Both" :
        (CuValue is not null) ? "CU Only" : "DI Only";
}

public enum MatchQuality { Exact, Normalized, Fuzzy, None }

public static partial class FieldNameNormalizer
{
    /// <summary>
    /// Normalizes a field name for comparison:
    ///   PascalCase → split words, lowercase, strip punctuation/spaces.
    ///   "ATSReference" → "ats reference" → "atsreference"
    ///   "ATS REFERENCE:" → "ats reference" → "atsreference"
    /// </summary>
    public static string Normalize(string name)
    {
        // Insert space before each uppercase letter that follows a lowercase letter or
        // before a single uppercase followed by lowercase (handles "ATSReference" → "ATS Reference")
        var spaced = PascalCaseRegex().Replace(name, " $1");
        // Lowercase, strip non-alphanumeric, collapse whitespace
        var cleaned = NonAlphaRegex().Replace(spaced.ToLowerInvariant(), " ").Trim();
        return cleaned;
    }

    /// <summary>Returns the key form (no spaces) for dictionary lookups.</summary>
    public static string NormalizeKey(string name) => Normalize(name).Replace(" ", "");

    /// <summary>Token-overlap similarity between two normalized names (0..1).</summary>
    public static double TokenSimilarity(string a, string b)
    {
        var tokensA = Normalize(a).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var tokensB = Normalize(b).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (tokensA.Count == 0 || tokensB.Count == 0) return 0;
        var intersection = tokensA.Intersect(tokensB).Count();
        var union = tokensA.Union(tokensB).Count();
        return (double)intersection / union; // Jaccard similarity
    }

    [GeneratedRegex(@"(?<=[a-z])([A-Z])|(?<=[A-Z])([A-Z][a-z])")]
    private static partial Regex PascalCaseRegex();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphaRegex();
}

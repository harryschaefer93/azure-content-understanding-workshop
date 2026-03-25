namespace CU_TestHarness.Models;

/// <summary>Method used to compare expected vs extracted field values.</summary>
public enum MatchMethod
{
    Contains,
    Semantic
}

public class TestSuiteViewModel
{
    public string AnalyzerId { get; set; } = string.Empty;
    public List<TestCaseResult> Results { get; set; } = [];
    public TimeSpan TotalDuration { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalSemanticCost { get; set; }

    public int TotalFields => Results.Sum(r => r.FieldResults.Count);
    public int PassedFields => Results.Sum(r => r.FieldResults.Count(f => f.Passed));
    public int FailedFields => TotalFields - PassedFields;
    public double AccuracyPercent => TotalFields > 0 ? (double)PassedFields / TotalFields * 100 : 0;
}

public class TestCaseResult
{
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public TimeSpan? Duration { get; set; }
    public CostEstimate? Cost { get; set; }
    public List<FieldTestResult> FieldResults { get; set; } = [];
    public int PassedCount => FieldResults.Count(f => f.Passed);
    public int FailedCount => FieldResults.Count(f => !f.Passed);
}

public class FieldTestResult
{
    public string FieldName { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string? ExtractedValue { get; set; }
    public double? Confidence { get; set; }
    public bool Passed { get; set; }
    public MatchMethod MatchMethod { get; set; } = MatchMethod.Contains;
    public string? SemanticExplanation { get; set; }
    public double? SemanticConfidence { get; set; }
    public int? CompletionTokensUsed { get; set; }
    public string? CompletionModel { get; set; }
}

/// <summary>
/// Inline expected values for a single test document.
/// </summary>
public class TestCaseInput
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[]? FileBytes { get; set; }
    public Dictionary<string, string> ExpectedFields { get; set; } = new();
    /// <summary>True when expected values were auto-filled from an analyzer run.</summary>
    public bool IsPrefilled { get; set; }
    /// <summary>Confidence scores from the pre-fill analysis, keyed by field name.</summary>
    public Dictionary<string, double?> PrefilledConfidences { get; set; } = new();
}

/// <summary>
/// JSON-serializable test manifest format.
/// </summary>
public class TestManifest
{
    public List<TestManifestCase> TestCases { get; set; } = [];
}

public class TestManifestCase
{
    public string FileName { get; set; } = string.Empty;
    public Dictionary<string, string> ExpectedFields { get; set; } = new();
}

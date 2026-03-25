namespace CU_TestHarness.Models;

/// <summary>N-document validation: stores results for all uploaded documents and a cross-document field matrix.</summary>
public class NDocValidationViewModel
{
    public List<AnalysisViewModel> DocResults { get; set; } = [];
    public List<MatrixFieldRow> FieldMatrix { get; set; } = [];
    public int ConsistentFieldCount => FieldMatrix.Count(r => r.IsConsistent);
    public int DiscrepancyCount => FieldMatrix.Count(r => !r.IsConsistent);
}

/// <summary>One row in the cross-document matrix, representing a single field across all documents.</summary>
public class MatrixFieldRow
{
    public string FieldName { get; set; } = string.Empty;
    /// <summary>Values[i] is the extracted value from document i (null if not found).</summary>
    public List<string?> Values { get; set; } = [];
    /// <summary>Confidences[i] for document i.</summary>
    public List<double?> Confidences { get; set; } = [];
    /// <summary>The consensus (most common non-null) value.</summary>
    public string? ConsensusValue { get; set; }
    /// <summary>True if all non-null values match the consensus.</summary>
    public bool IsConsistent { get; set; }

    /// <summary>Returns true if the value at index i is an outlier (differs from consensus).</summary>
    public bool IsOutlier(int i) =>
        !IsConsistent && i >= 0 && i < Values.Count &&
        Values[i] is not null &&
        !string.Equals(Values[i]?.Trim(), ConsensusValue?.Trim(), StringComparison.OrdinalIgnoreCase);
}

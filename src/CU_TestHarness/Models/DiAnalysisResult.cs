namespace CU_TestHarness.Models;

public class DiAnalysisResult
{
    public string ModelId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public string? RawJson { get; set; }
    public List<ExtractedField> Fields { get; set; } = [];
    public TimeSpan? Duration { get; set; }
    public int? PageCount { get; set; }
    public CostEstimate? Cost { get; set; }
    public List<PageInfo>? Pages { get; set; }
}

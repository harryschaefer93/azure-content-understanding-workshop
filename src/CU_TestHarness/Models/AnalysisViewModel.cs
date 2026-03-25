namespace CU_TestHarness.Models;

public class AnalysisViewModel
{
    public string AnalyzerId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public string? RawJson { get; set; }
    public string? Markdown { get; set; }
    public List<ExtractedField> Fields { get; set; } = [];
    public TimeSpan? Duration { get; set; }
    public int? PageCount { get; set; }
    public int? TokensUsed { get; set; }
    public CostEstimate? Cost { get; set; }
    public List<PageInfo>? Pages { get; set; }
}

public class ExtractedField
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Type { get; set; }
    public double? Confidence { get; set; }
    public List<BoundingRegion>? BoundingRegions { get; set; }
}

public class BoundingRegion
{
    public int PageNumber { get; set; }
    public List<double> Polygon { get; set; } = [];
}

public class PageInfo
{
    public int PageNumber { get; set; }
    public double Angle { get; set; }
}

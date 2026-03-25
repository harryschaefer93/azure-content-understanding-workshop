namespace CU_TestHarness.Models;

public class CostEstimate
{
    public decimal EstimatedCost { get; set; }
    public string CostBreakdown { get; set; } = string.Empty;
    public int? PageCount { get; set; }
    public int? TokensUsed { get; set; }
    public string PricingTier { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
}

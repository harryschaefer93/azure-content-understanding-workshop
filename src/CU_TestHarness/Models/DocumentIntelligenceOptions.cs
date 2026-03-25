namespace CU_TestHarness.Models;

public class DocumentIntelligenceOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string DefaultModelId { get; set; } = "prebuilt-invoice";
    public string ApiVersion { get; set; } = "2024-11-30";
}

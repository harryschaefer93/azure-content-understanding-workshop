namespace CU_TestHarness.Models;

public class ContentUnderstandingOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string DefaultAnalyzerId { get; set; } = "prebuilt-documentSearch";
    public int MaxFileSizeMB { get; set; } = 50;
    /// <summary>Azure OpenAI endpoint for completion models (semantic matching). Separate from CU endpoint.</summary>
    public string ModelsEndpoint { get; set; } = string.Empty;
}

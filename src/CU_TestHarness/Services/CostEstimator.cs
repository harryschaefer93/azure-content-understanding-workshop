using CU_TestHarness.Models;

namespace CU_TestHarness.Services;

/// <summary>
/// Estimates costs for CU and DI operations based on published Azure pricing.
/// Rates are hardcoded for easy updating — check https://azure.microsoft.com/pricing/details/ai-document-intelligence/
/// and https://azure.microsoft.com/pricing/details/cognitive-services/content-understanding/
/// </summary>
public class CostEstimator
{
    // --- CU Pricing (per page / per 1K tokens) ---
    // These are approximate S0 tier rates as of March 2026
    private const decimal CuReadPerPage = 0.001m;           // prebuilt-read: $0.001/page
    private const decimal CuLayoutPerPage = 0.010m;          // prebuilt-layout: $0.01/page
    private const decimal CuPrebuiltFieldPerPage = 0.010m;   // prebuilt domain (invoice, etc.): $0.01/page
    private const decimal CuCustomFieldPerPage = 0.015m;     // custom field extraction: $0.015/page
    private const decimal CuLlmPer1KTokens_Gpt41 = 0.010m;   // GPT-4.1 LLM inference cost
    private const decimal CuLlmPer1KTokens_Gpt41Mini = 0.004m; // GPT-4.1 Mini (~60% cheaper)
    private const decimal CuLlmPer1KTokens_Gpt4o = 0.005m;    // GPT-4o Standard inference cost
    private const decimal CuDocSearchPerPage = 0.020m;       // prebuilt-documentSearch: layout + LLM

    // --- DI Pricing (per page) ---
    private const decimal DiReadPerPage = 0.001m;
    private const decimal DiLayoutPerPage = 0.010m;
    private const decimal DiPrebuiltPerPage = 0.010m;
    private const decimal DiCustomPerPage = 0.015m;          // custom/composed models

    // --- Direct Azure OpenAI Completion Pricing (per 1K tokens, separate from CU) ---
    private const decimal CompletionInputPer1K_Gpt41 = 0.002m;
    private const decimal CompletionOutputPer1K_Gpt41 = 0.008m;
    private const decimal CompletionInputPer1K_Gpt41Mini = 0.0004m;
    private const decimal CompletionOutputPer1K_Gpt41Mini = 0.0016m;
    private const decimal CompletionInputPer1K_Gpt4o = 0.0025m;
    private const decimal CompletionOutputPer1K_Gpt4o = 0.010m;

    public CostEstimate EstimateCuCost(string analyzerId, int pageCount, int? tokensUsed = null, string? completionModel = null)
    {
        var tier = GetCuTier(analyzerId);
        var perPage = tier switch
        {
            "Read" => CuReadPerPage,
            "Layout" => CuLayoutPerPage,
            "Domain" => CuPrebuiltFieldPerPage,
            "RAG" => CuDocSearchPerPage,
            "Custom" => CuCustomFieldPerPage,
            _ => CuPrebuiltFieldPerPage
        };

        var llmRate = completionModel?.Contains("mini", StringComparison.OrdinalIgnoreCase) == true
            ? CuLlmPer1KTokens_Gpt41Mini
            : completionModel?.Contains("4o", StringComparison.OrdinalIgnoreCase) == true
                ? CuLlmPer1KTokens_Gpt4o
                : CuLlmPer1KTokens_Gpt41;

        var pageCost = perPage * pageCount;
        var tokenCost = (tier == "RAG" && tokensUsed.HasValue)
            ? llmRate * (tokensUsed.Value / 1000m)
            : 0m;
        var total = pageCost + tokenCost;

        var modelLabel = completionModel ?? "gpt-4.1";
        var breakdown = tokensUsed.HasValue && tokenCost > 0
            ? $"{pageCount} page(s) × ${perPage:F3} = ${pageCost:F4} + {tokensUsed.Value} tokens × ${llmRate:F3}/1K ({modelLabel}) = ${tokenCost:F4}"
            : $"{pageCount} page(s) × ${perPage:F3} = ${total:F4}";

        return new CostEstimate
        {
            EstimatedCost = total,
            CostBreakdown = breakdown,
            PageCount = pageCount,
            TokensUsed = tokensUsed,
            PricingTier = tier
        };
    }

    public CostEstimate EstimateDiCost(string modelId, int pageCount)
    {
        var tier = GetDiTier(modelId);
        var perPage = tier switch
        {
            "Read" => DiReadPerPage,
            "Layout" => DiLayoutPerPage,
            "Prebuilt" => DiPrebuiltPerPage,
            "Custom" => DiCustomPerPage,
            _ => DiPrebuiltPerPage
        };

        var total = perPage * pageCount;

        return new CostEstimate
        {
            EstimatedCost = total,
            CostBreakdown = $"{pageCount} page(s) × ${perPage:F3} = ${total:F4}",
            PageCount = pageCount,
            PricingTier = tier
        };
    }

    private static string GetCuTier(string analyzerId) => analyzerId switch
    {
        "prebuilt-read" => "Read",
        "prebuilt-layout" => "Layout",
        "prebuilt-documentSearch" or "prebuilt-imageSearch" or "prebuilt-audioSearch" or "prebuilt-videoSearch" => "RAG",
        _ when analyzerId.StartsWith("prebuilt-") => "Domain",
        _ => "Custom"
    };

    private static string GetDiTier(string modelId) => modelId switch
    {
        "prebuilt-read" => "Read",
        "prebuilt-layout" => "Layout",
        _ when modelId.StartsWith("prebuilt-") => "Prebuilt",
        _ => "Custom"
    };

    /// <summary>
    /// Estimate cost for a direct Azure OpenAI completion call (semantic matching).
    /// </summary>
    public CostEstimate EstimateCompletionCost(string deploymentName, int inputTokens, int outputTokens)
    {
        var (inputRate, outputRate) = GetCompletionRates(deploymentName);
        var inputCost = inputRate * (inputTokens / 1000m);
        var outputCost = outputRate * (outputTokens / 1000m);
        var total = inputCost + outputCost;

        return new CostEstimate
        {
            EstimatedCost = total,
            CostBreakdown = $"{inputTokens} in × ${inputRate:F4}/1K + {outputTokens} out × ${outputRate:F4}/1K = ${total:F6}",
            TokensUsed = inputTokens + outputTokens,
            PricingTier = "Completion"
        };
    }

    private static (decimal inputRate, decimal outputRate) GetCompletionRates(string deploymentName) => deploymentName switch
    {
        _ when deploymentName.Contains("mini", StringComparison.OrdinalIgnoreCase) => (CompletionInputPer1K_Gpt41Mini, CompletionOutputPer1K_Gpt41Mini),
        _ when deploymentName.Contains("4o", StringComparison.OrdinalIgnoreCase) => (CompletionInputPer1K_Gpt4o, CompletionOutputPer1K_Gpt4o),
        _ => (CompletionInputPer1K_Gpt41, CompletionOutputPer1K_Gpt41)
    };
}

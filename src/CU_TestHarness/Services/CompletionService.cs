using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using CU_TestHarness.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace CU_TestHarness.Services;

/// <summary>
/// Thin wrapper around Azure OpenAI ChatClient for semantic field matching.
/// Uses the Models account (separate from the CU endpoint) + DefaultAzureCredential.
/// </summary>
public class CompletionService
{
    private readonly AzureOpenAIClient _client;
    private readonly ModelProfileState _profileState;

    public CompletionService(IOptions<ContentUnderstandingOptions> options, ModelProfileState profileState)
    {
        var endpoint = options.Value.ModelsEndpoint;
        if (string.IsNullOrEmpty(endpoint))
            throw new InvalidOperationException("ModelsEndpoint is not configured in ContentUnderstanding settings.");
        _client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        _profileState = profileState;
    }

    /// <summary>
    /// Ask the LLM whether two field values are semantically equivalent.
    /// Returns match result with reasoning, confidence, and token counts.
    /// </summary>
    public async Task<SemanticMatchResult> SemanticMatchAsync(string fieldName, string expectedValue, string extractedValue)
    {
        var profile = _profileState.ActiveCompletion;
        var chatClient = _client.GetChatClient(profile.DeploymentName);

        var systemPrompt = """
            You are a document field comparison assistant. Compare an expected field value to an extracted value from a document.
            Determine if they are semantically equivalent — meaning they convey the same information even if worded differently.
            Consider abbreviations (e.g. "N/A" vs "Not Applicable"), formatting differences, punctuation, and contextual equivalence.
            
            Respond ONLY with this JSON (no markdown, no extra text):
            {"match": true, "confidence": 0.95, "reasoning": "brief explanation"}
            """;

        var userPrompt = $"""
            Field: {fieldName}
            Expected: {expectedValue}
            Extracted: {extractedValue}
            """;

        var options = new ChatCompletionOptions
        {
            Temperature = 0f,
            MaxOutputTokenCount = 200
        };

        var result = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            ],
            options);

        var response = result.Value;
        var content = response.Content[0].Text;
        var usage = response.Usage;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            return new SemanticMatchResult
            {
                IsMatch = root.GetProperty("match").GetBoolean(),
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : null,
                Reasoning = root.TryGetProperty("reasoning", out var reason) ? reason.GetString() ?? "" : "",
                InputTokens = usage.InputTokenCount,
                OutputTokens = usage.OutputTokenCount,
                ModelDeployment = profile.DeploymentName
            };
        }
        catch
        {
            // Fallback: try to extract match from malformed JSON
            var lower = content.ToLowerInvariant();
            return new SemanticMatchResult
            {
                IsMatch = lower.Contains("\"match\": true") || lower.Contains("\"match\":true"),
                Reasoning = content,
                InputTokens = usage.InputTokenCount,
                OutputTokens = usage.OutputTokenCount,
                ModelDeployment = profile.DeploymentName
            };
        }
    }
}

public class SemanticMatchResult
{
    public bool IsMatch { get; set; }
    public double? Confidence { get; set; }
    public string Reasoning { get; set; } = "";
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string ModelDeployment { get; set; } = "";
}

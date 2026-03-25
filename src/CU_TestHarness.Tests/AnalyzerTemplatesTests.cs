using System.Text.Json;
using System.Text.RegularExpressions;
using CU_TestHarness.Models;

namespace CU_TestHarness.Tests;

public partial class AnalyzerTemplatesTests
{
    // CU API requires: starts with letter, only letters/digits/underscores, max 32 chars
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]*$")]
    private static partial Regex ValidAnalyzerIdRegex();

    [Theory]
    [MemberData(nameof(AllTemplateNames))]
    public void Template_DefaultAnalyzerId_HasNoHyphens(string templateName)
    {
        var template = AnalyzerTemplates.All.First(t => t.Name == templateName);
        var json = template.Generate("ignored", "gpt-4.1", "text-embedding-ada-002");

        // The template uses the first parameter as analyzerId, but the default in the
        // method signature is what matters. Call with default to test the actual default.
        var defaultJson = CallTemplateWithDefaults(templateName);
        using var doc = JsonDocument.Parse(defaultJson);
        var analyzerId = doc.RootElement.GetProperty("analyzerId").GetString()!;

        Assert.DoesNotContain("-", analyzerId);
    }

    [Theory]
    [MemberData(nameof(AllTemplateNames))]
    public void Template_DefaultAnalyzerId_MatchesValidPattern(string templateName)
    {
        var defaultJson = CallTemplateWithDefaults(templateName);
        using var doc = JsonDocument.Parse(defaultJson);
        var analyzerId = doc.RootElement.GetProperty("analyzerId").GetString()!;

        Assert.Matches(ValidAnalyzerIdRegex(), analyzerId);
    }

    [Theory]
    [MemberData(nameof(AllTemplateNames))]
    public void Template_ProducesValidJson(string templateName)
    {
        var template = AnalyzerTemplates.All.First(t => t.Name == templateName);
        var json = template.Generate("test_analyzer", "gpt-4.1", "text-embedding-ada-002");

        // Should parse without error
        using var doc = JsonDocument.Parse(json);

        // Must have analyzerId
        Assert.True(doc.RootElement.TryGetProperty("analyzerId", out var idEl));
        Assert.Equal("test_analyzer", idEl.GetString());

        // Must have baseAnalyzerId
        Assert.True(doc.RootElement.TryGetProperty("baseAnalyzerId", out _));
    }

    [Fact]
    public void GenerateSchemaFromFields_PreservesAnalyzerId()
    {
        var fields = new[] { ("Name", "extract"), ("Summary", "generate") };
        var json = AnalyzerTemplates.GenerateSchemaFromFields("my_test_id", fields);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("my_test_id", doc.RootElement.GetProperty("analyzerId").GetString());
    }

    [Fact]
    public void GenerateSchemaFromFields_PassesHyphenatedIdThrough()
    {
        // The template method doesn't validate — that's the service's job.
        // Verify it passes the ID verbatim so the service can reject it.
        var fields = new[] { ("Name", "extract") };
        var json = AnalyzerTemplates.GenerateSchemaFromFields("has-hyphens", fields);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("has-hyphens", doc.RootElement.GetProperty("analyzerId").GetString());
    }

    [Fact]
    public void AllTemplates_HasExpectedCount()
    {
        Assert.Equal(5, AnalyzerTemplates.All.Count);
    }

    public static TheoryData<string> AllTemplateNames()
    {
        var data = new TheoryData<string>();
        foreach (var (name, _, _) in AnalyzerTemplates.All)
            data.Add(name);
        return data;
    }

    /// <summary>
    /// Calls each template method with its default analyzerId parameter value by calling Generate
    /// with the template's own default ID (extracted by calling with a known ID and the method's defaults).
    /// Since the Generate func takes (analyzerId, completion, embedding), we need to call the
    /// underlying static methods directly to get their default parameter values.
    /// </summary>
    private static string CallTemplateWithDefaults(string templateName) => templateName switch
    {
        "Commitment Letter" => AnalyzerTemplates.CommitmentLetter(),
        "Enhanced Title Search" => AnalyzerTemplates.EnhancedTitleSearch(),
        "Field Extraction (Title Search)" => AnalyzerTemplates.FieldExtraction(),
        "CTI Document Classification" => AnalyzerTemplates.CtiClassification(),
        "Multi-Province Title Search" => AnalyzerTemplates.MultiProvinceTitleSearch(),
        "Document Classification" => AnalyzerTemplates.DocumentClassification(),
        "RAG / Document Search" => AnalyzerTemplates.RagSearch(),
        _ => throw new ArgumentException($"Unknown template: {templateName}")
    };
}

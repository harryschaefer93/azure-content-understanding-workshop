using CU_TestHarness.Models;
using CU_TestHarness.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CU_TestHarness.Tests;

public class ContentUnderstandingServiceTests
{
    private static ContentUnderstandingService CreateService()
    {
        var options = Options.Create(new ContentUnderstandingOptions
        {
            Endpoint = "https://test.cognitiveservices.azure.com"
        });
        var logger = Mock.Of<ILogger<ContentUnderstandingService>>();
        var httpClient = new HttpClient();
        var costEstimator = new CostEstimator();

        return new ContentUnderstandingService(options, logger, httpClient, costEstimator);
    }

    [Fact]
    public async Task CreateOrUpdate_RejectsHyphenInAnalyzerId()
    {
        var service = CreateService();
        var json = """{"analyzerId": "has-hyphens", "baseAnalyzerId": "prebuilt-document"}""";

        var (success, error) = await service.CreateOrUpdateAnalyzerAsync(json);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("hyphen", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrUpdate_RejectsMultipleHyphens()
    {
        var service = CreateService();
        var json = """{"analyzerId": "my-bad-analyzer-id", "baseAnalyzerId": "prebuilt-document"}""";

        var (success, error) = await service.CreateOrUpdateAnalyzerAsync(json);

        Assert.False(success);
        Assert.Contains("'-'", error!);
    }

    [Fact]
    public async Task CreateOrUpdate_RejectsMissingAnalyzerId()
    {
        var service = CreateService();
        var json = """{"baseAnalyzerId": "prebuilt-document"}""";

        var (success, error) = await service.CreateOrUpdateAnalyzerAsync(json);

        Assert.False(success);
        Assert.Contains("analyzerId", error!);
    }

    [Fact]
    public async Task CreateOrUpdate_RejectsInvalidJson()
    {
        var service = CreateService();
        var json = "not valid json {{{";

        var (success, error) = await service.CreateOrUpdateAnalyzerAsync(json);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("valid_id")]
    [InlineData("myAnalyzer123")]
    [InlineData("test_analyzer_2026")]
    public async Task CreateOrUpdate_AcceptsValidIds(string analyzerId)
    {
        // These should pass validation but fail at the HTTP call level (no real server).
        // We're testing that they get PAST the hyphen check.
        var service = CreateService();
        var json = $$"""{"analyzerId": "{{analyzerId}}", "baseAnalyzerId": "prebuilt-document"}""";

        var (success, error) = await service.CreateOrUpdateAnalyzerAsync(json);

        // Should fail with an HTTP/auth error, NOT a validation error about hyphens
        if (!success)
        {
            Assert.DoesNotContain("hyphen", error!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("'-'", error!);
        }
        // If it somehow succeeded (unlikely without a real server), that's also fine
    }
}

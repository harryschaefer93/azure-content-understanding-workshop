using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using CU_TestHarness.Models;

namespace CU_TestHarness.Services;

public class DocumentIntelligenceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentIntelligenceService> _logger;
    private readonly CostEstimator _costEstimator;
    private readonly DefaultAzureCredential _credential;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public DocumentIntelligenceService(
        HttpClient httpClient,
        ILogger<DocumentIntelligenceService> logger,
        CostEstimator costEstimator)
    {
        _httpClient = httpClient;
        _logger = logger;
        _costEstimator = costEstimator;
        _credential = new DefaultAzureCredential();
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var context = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });
        var token = await _credential.GetTokenAsync(context, cancellationToken);
        return token.Token;
    }

    public async Task<DiAnalysisResult> AnalyzeFileAsync(
        DocumentIntelligenceOptions options,
        string modelId,
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new DiAnalysisResult
        {
            ModelId = modelId,
            FileName = fileName,
            FileSizeBytes = fileStream.Length,
            ContentType = contentType
        };

        try
        {
            var endpoint = options.Endpoint.TrimEnd('/');
            var apiVersion = options.ApiVersion;
            var analyzeUrl = $"{endpoint}/documentintelligence/documentModels/{Uri.EscapeDataString(modelId)}:analyze?api-version={Uri.EscapeDataString(apiVersion)}";

            // Enable key-value pair extraction for layout-type models
            if (modelId.Equals("prebuilt-layout", StringComparison.OrdinalIgnoreCase) ||
                modelId.Equals("prebuilt-document", StringComparison.OrdinalIgnoreCase))
            {
                analyzeUrl += "&features=keyValuePairs";
            }

            using var requestContent = new StreamContent(fileStream);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/octet-stream");

            using var request = new HttpRequestMessage(HttpMethod.Post, analyzeUrl);
            request.Content = requestContent;
            var token = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                sw.Stop();
                result.Duration = sw.Elapsed;
                result.Status = "Failed";
                result.ErrorMessage = $"DI API returned {(int)response.StatusCode} {response.ReasonPhrase}.\n\n{errorBody}";
                return result;
            }

            // Get operation location for polling
            if (!response.Headers.TryGetValues("Operation-Location", out var opLocations))
            {
                sw.Stop();
                result.Duration = sw.Elapsed;
                result.Status = "Failed";
                result.ErrorMessage = "DI API did not return an Operation-Location header.";
                return result;
            }

            var operationUrl = opLocations.First();

            // Poll for completion
            var pollResult = await PollForCompletionAsync(operationUrl, cancellationToken);

            sw.Stop();
            result.Duration = sw.Elapsed;

            if (pollResult.Status != "succeeded")
            {
                result.Status = "Failed";
                result.ErrorMessage = pollResult.ErrorMessage ?? $"DI analysis ended with status: {pollResult.Status}";
                result.RawJson = pollResult.RawJson;
                return result;
            }

            result.Status = "Succeeded";
            result.RawJson = pollResult.RawJson;

            // Parse fields and page count from the response
            ParseDiResult(pollResult.RawJson, result);

            // Estimate cost
            result.Cost = _costEstimator.EstimateDiCost(modelId, result.PageCount ?? 1);
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
            result.Status = "Failed";
            result.ErrorMessage = "Request timed out.";
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error analyzing file {FileName} with DI model {ModelId}", fileName, modelId);
        }

        return result;
    }

    public async Task<List<string>> ListModelIdsAsync(
        DocumentIntelligenceOptions options,
        CancellationToken cancellationToken = default)
    {
        var ids = new List<string>();
        try
        {
            var endpoint = options.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/documentintelligence/documentModels?api-version={Uri.EscapeDataString(options.ApiVersion)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var token = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return ids;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("value", out var models))
            {
                foreach (var model in models.EnumerateArray())
                {
                    if (model.TryGetProperty("modelId", out var modelId))
                        ids.Add(modelId.GetString()!);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing DI models");
        }
        return ids;
    }

    public async Task<bool> TestConnectionAsync(
        DocumentIntelligenceOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = options.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/documentintelligence/documentModels?api-version={Uri.EscapeDataString(options.ApiVersion)}&$top=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var token = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(string Status, string? ErrorMessage, string? RawJson)> PollForCompletionAsync(
        string operationUrl,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < 120; i++) // max ~10 min
        {
            await Task.Delay(TimeSpan.FromSeconds(i < 5 ? 2 : 5), cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, operationUrl);
            var token = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(json);
            var status = doc.RootElement.GetProperty("status").GetString() ?? "unknown";

            if (status is "succeeded" or "failed")
            {
                string? error = null;
                if (status == "failed" && doc.RootElement.TryGetProperty("error", out var errorEl))
                    error = errorEl.ToString();

                return (status, error, FormatJson(json));
            }
        }

        return ("timeout", "DI analysis polling timed out after 10 minutes.", null);
    }

    private static void ParseDiResult(string? rawJson, DiAnalysisResult result)
    {
        if (string.IsNullOrEmpty(rawJson)) return;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            // Get analyzeResult
            if (!root.TryGetProperty("analyzeResult", out var analyzeResult)) return;

            // Page count
            if (analyzeResult.TryGetProperty("pages", out var pages))
            {
                result.PageCount = pages.GetArrayLength();

                // Extract page angles for rotation support
                if (pages.ValueKind == JsonValueKind.Array)
                {
                    var pageList = new List<PageInfo>();
                    foreach (var page in pages.EnumerateArray())
                    {
                        var pi = new PageInfo();
                        if (page.TryGetProperty("pageNumber", out var pn) && pn.TryGetInt32(out var pageNum))
                            pi.PageNumber = pageNum;
                        if (page.TryGetProperty("angle", out var angle) && angle.TryGetDouble(out var angleVal))
                            pi.Angle = angleVal;
                        pageList.Add(pi);
                    }
                    if (pageList.Count > 0)
                        result.Pages = pageList;
                }
            }

            // Extract fields from documents array (prebuilt-invoice, prebuilt-receipt, etc.)
            if (analyzeResult.TryGetProperty("documents", out var documents))
            {
                foreach (var document in documents.EnumerateArray())
                {
                    if (!document.TryGetProperty("fields", out var fields)) continue;

                    foreach (var field in fields.EnumerateObject())
                    {
                        var extractedField = new ExtractedField { Name = field.Name };

                        var fieldVal = field.Value;
                        if (fieldVal.TryGetProperty("content", out var content))
                            extractedField.Value = content.GetString();
                        else if (fieldVal.TryGetProperty("valueString", out var vs))
                            extractedField.Value = vs.GetString();
                        else if (fieldVal.TryGetProperty("value", out var v))
                            extractedField.Value = v.ToString();

                        if (fieldVal.TryGetProperty("type", out var type))
                            extractedField.Type = type.GetString();

                        if (fieldVal.TryGetProperty("confidence", out var conf) && conf.TryGetDouble(out var confVal))
                            extractedField.Confidence = confVal;

                        result.Fields.Add(extractedField);
                    }
                }
            }

            // Extract key-value pairs (prebuilt-layout with features=keyValuePairs, prebuilt-document)
            if (analyzeResult.TryGetProperty("keyValuePairs", out var kvPairs))
            {
                foreach (var kvp in kvPairs.EnumerateArray())
                {
                    string? keyContent = null;
                    string? valueContent = null;
                    double? confidence = null;

                    if (kvp.TryGetProperty("key", out var key) && key.TryGetProperty("content", out var kc))
                        keyContent = kc.GetString();
                    if (kvp.TryGetProperty("value", out var val) && val.TryGetProperty("content", out var vc))
                        valueContent = vc.GetString();
                    if (kvp.TryGetProperty("confidence", out var kvConf) && kvConf.TryGetDouble(out var kvConfVal))
                        confidence = kvConfVal;

                    if (!string.IsNullOrWhiteSpace(keyContent))
                    {
                        result.Fields.Add(new ExtractedField
                        {
                            Name = keyContent,
                            Value = valueContent,
                            Type = "keyValuePair",
                            Confidence = confidence
                        });
                    }
                }
            }
        }
        catch
        {
            // Non-fatal — raw JSON is still available
        }
    }

    private static string FormatJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, s_jsonOptions);
        }
        catch
        {
            return json;
        }
    }
}

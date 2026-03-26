using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Identity;
using CU_TestHarness.Models;
using Microsoft.Extensions.Options;

namespace CU_TestHarness.Services;

public class ContentUnderstandingService
{
    private readonly ContentUnderstandingClient _client;
    private readonly HttpClient _httpClient;
    private readonly DefaultAzureCredential _credential;
    private readonly ILogger<ContentUnderstandingService> _logger;
    private readonly ContentUnderstandingOptions _options;
    private readonly CostEstimator _costEstimator;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public ContentUnderstandingService(
        IOptions<ContentUnderstandingOptions> options,
        ILogger<ContentUnderstandingService> logger,
        HttpClient httpClient,
        CostEstimator costEstimator)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        _costEstimator = costEstimator;
        _credential = new DefaultAzureCredential();
        _client = new ContentUnderstandingClient(
            new Uri(_options.Endpoint),
            _credential);
    }

    public async Task<AnalysisViewModel> AnalyzeFileAsync(
        string analyzerId,
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new AnalysisViewModel
        {
            AnalyzerId = analyzerId,
            FileName = fileName,
            FileSizeBytes = fileStream.Length,
            ContentType = contentType
        };

        try
        {
            var binaryData = await BinaryData.FromStreamAsync(fileStream, cancellationToken);

            Operation<AnalysisResult> operation = await _client.AnalyzeBinaryAsync(
                WaitUntil.Completed,
                analyzerId,
                binaryData,
                contentType: contentType,
                cancellationToken: cancellationToken);

            sw.Stop();
            result.Duration = sw.Elapsed;
            result.Status = "Succeeded";

            var analysisResult = operation.Value;

            // Extract markdown
            if (analysisResult.Contents is { Count: > 0 })
            {
                result.Markdown = string.Join("\n\n---\n\n",
                    analysisResult.Contents.Select(c => c.Markdown ?? string.Empty));
            }

            // Extract fields
            if (analysisResult.Contents is { Count: > 0 })
            {
                foreach (var content in analysisResult.Contents)
                {
                    if (content.Fields is { Count: > 0 })
                    {
                        foreach (var kvp in content.Fields)
                        {
                            var extractedField = new ExtractedField
                            {
                                Name = kvp.Key,
                                Value = kvp.Value?.Value?.ToString(),
                                Type = kvp.Value?.GetType().Name.Replace("Content", "").Replace("Field", ""),
                                Confidence = kvp.Value?.Confidence
                            };

                            // Extract bounding regions from SDK Sources property
                            ExtractBoundingRegionsFromSdk(kvp.Value, extractedField);

                            result.Fields.Add(extractedField);
                        }
                    }
                }
            }

            // Raw JSON via the operation response
            result.RawJson = FormatJson(operation.GetRawResponse().Content.ToString());

            // If SDK fields are empty, try parsing fields from the embedded DI response in the raw JSON
            // (CU prebuilt-document wraps DI response which may include documents[].fields and/or keyValuePairs)
            if (result.Fields.Count == 0 && result.RawJson is not null)
            {
                ParseFieldsFromRawJson(result);
            }

            // Extract page/token counts for cost estimation
            ExtractUsageMetrics(result);
            result.Cost = _costEstimator.EstimateCuCost(analyzerId, result.PageCount ?? 1, result.TokensUsed);
        }
        catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
            result.Status = "Failed";
            result.ErrorMessage = $"Authentication failed ({ex.Status}). Run 'az login' and ensure you have the 'Cognitive Services User' role on the CU resource.\n\n{ex.Message}";
            _logger.LogError(ex, "Auth failure calling Content Understanding API");
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error analyzing file {FileName}", fileName);
        }

        return result;
    }

    public async Task<List<string>> ListAnalyzerIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = new List<string>();
        try
        {
            await foreach (var analyzer in _client.GetAnalyzersAsync(cancellationToken))
            {
                ids.Add(analyzer.AnalyzerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing analyzers");
        }
        return ids;
    }

    // --- Analyzer CRUD (REST API — SDK doesn't cover these) ---

    private async Task<string> GetBearerTokenAsync(CancellationToken cancellationToken = default)
    {
        var tokenResult = await _credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(["https://cognitiveservices.azure.com/.default"]),
            cancellationToken);
        return tokenResult.Token;
    }

    public async Task<string?> GetAnalyzerDefinitionAsync(string analyzerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _options.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/contentunderstanding/analyzers/{Uri.EscapeDataString(analyzerId)}?api-version=2025-11-01";
            var token = await GetBearerTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return response.IsSuccessStatusCode ? FormatJson(json) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analyzer definition for {AnalyzerId}", analyzerId);
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> CreateOrUpdateAnalyzerAsync(
        string schemaJson,
        bool allowReplace = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract analyzerId from the JSON body
            using var parseDoc = JsonDocument.Parse(schemaJson);
            if (!parseDoc.RootElement.TryGetProperty("analyzerId", out var idEl))
                return (false, "JSON must contain an 'analyzerId' property.");

            var analyzerId = idEl.GetString()!;
            if (analyzerId.Contains('-'))
                return (false, "Analyzer ID cannot contain hyphens ('-'). Use underscores ('_') instead.");

            var endpoint = _options.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/contentunderstanding/analyzers/{Uri.EscapeDataString(analyzerId)}?api-version=2025-11-01";
            if (allowReplace) url += "&allowReplace=true";

            var token = await GetBearerTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(schemaJson, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) return (true, null);

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, $"{(int)response.StatusCode} {response.ReasonPhrase}: {error}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating analyzer");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAnalyzerAsync(
        string analyzerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _options.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/contentunderstanding/analyzers/{Uri.EscapeDataString(analyzerId)}?api-version=2025-11-01";
            var token = await GetBearerTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, $"{(int)response.StatusCode} {response.ReasonPhrase}: {error}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting analyzer {AnalyzerId}", analyzerId);
            return (false, ex.Message);
        }
    }

    // --- Field extraction from raw JSON ---

    internal static void ParseFieldsFromRawJson(AnalysisViewModel result)
    {
        if (string.IsNullOrEmpty(result.RawJson)) return;

        try
        {
            using var doc = JsonDocument.Parse(result.RawJson);
            var root = doc.RootElement;

            // CU payload shapes vary by analyzer and API version. Walk all common nodes.
            ParseFieldsFromCandidateNode(root, result);

            if (root.TryGetProperty("result", out var resultNode))
            {
                ParseFieldsFromCandidateNode(resultNode, result);

                if (resultNode.TryGetProperty("analyzeResult", out var nestedAnalyzeResult))
                    ParseFieldsFromCandidateNode(nestedAnalyzeResult, result);

                if (resultNode.TryGetProperty("contents", out var contents) &&
                    contents.ValueKind == JsonValueKind.Array)
                {
                    foreach (var content in contents.EnumerateArray())
                    {
                        ParseFieldsFromCandidateNode(content, result);

                        if (content.TryGetProperty("analyzeResult", out var contentAnalyzeResult))
                            ParseFieldsFromCandidateNode(contentAnalyzeResult, result);
                    }
                }
            }

            if (root.TryGetProperty("analyzeResult", out var topAnalyzeResult))
                ParseFieldsFromCandidateNode(topAnalyzeResult, result);
        }
        catch
        {
            // Non-fatal — raw JSON is still available via the JSON tab
        }
    }

    private static void ParseFieldsFromCandidateNode(JsonElement node, AnalysisViewModel result)
    {
        // Extract direct fields dictionaries: fields.{name} = { ... }
        if (node.TryGetProperty("fields", out var directFields) &&
            directFields.ValueKind == JsonValueKind.Object)
        {
            foreach (var field in directFields.EnumerateObject())
                AddStructuredField(field.Name, field.Value, result);
        }

        // Extract fields from documents array: documents[].fields.{name}
        if (node.TryGetProperty("documents", out var documents) &&
            documents.ValueKind == JsonValueKind.Array)
        {
            foreach (var document in documents.EnumerateArray())
            {
                if (!document.TryGetProperty("fields", out var fields) ||
                    fields.ValueKind != JsonValueKind.Object) continue;

                foreach (var field in fields.EnumerateObject())
                    AddStructuredField(field.Name, field.Value, result);
            }
        }

        // Extract key-value pairs: keyValuePairs[]
        if (node.TryGetProperty("keyValuePairs", out var kvPairs) &&
            kvPairs.ValueKind == JsonValueKind.Array)
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

    private static void AddStructuredField(string fieldName, JsonElement fieldVal, AnalysisViewModel result)
    {
        // Check if the value is a collection of field schema definitions
        // (e.g. from prebuilt-documentFieldSchema: { "CommitmentNumber": { "type": "string", "method": "extract", ... }, ... })
        if (fieldVal.ValueKind == JsonValueKind.Object && IsFieldSchemaCollection(fieldVal))
        {
            foreach (var subField in fieldVal.EnumerateObject())
            {
                if (subField.Value.ValueKind != JsonValueKind.Object) continue;

                var extracted = new ExtractedField { Name = subField.Name };

                if (subField.Value.TryGetProperty("type", out var ft))
                    extracted.Type = ft.GetString();

                // Use the first example as the extracted value
                if (subField.Value.TryGetProperty("examples", out var examples) &&
                    examples.ValueKind == JsonValueKind.Array && examples.GetArrayLength() > 0)
                {
                    extracted.Value = examples[0].ToString();
                }

                if (subField.Value.TryGetProperty("confidence", out var fc) && fc.TryGetDouble(out var fcVal))
                    extracted.Confidence = fcVal;

                result.Fields.Add(extracted);
            }
            return;
        }

        var extractedField = new ExtractedField { Name = fieldName };

        if (fieldVal.ValueKind == JsonValueKind.String)
            extractedField.Value = fieldVal.GetString();
        else if (fieldVal.TryGetProperty("content", out var content))
            extractedField.Value = content.GetString();
        else if (fieldVal.TryGetProperty("valueString", out var valueString))
            extractedField.Value = valueString.GetString();
        else if (fieldVal.TryGetProperty("value", out var value))
            extractedField.Value = value.ToString();
        else
            extractedField.Value = fieldVal.ToString();

        if (fieldVal.TryGetProperty("type", out var type))
            extractedField.Type = type.GetString();

        if (fieldVal.TryGetProperty("confidence", out var conf) && conf.TryGetDouble(out var confVal))
            extractedField.Confidence = confVal;

        ExtractBoundingRegions(fieldVal, extractedField);

        result.Fields.Add(extractedField);
    }

    private static void ExtractBoundingRegions(JsonElement fieldVal, ExtractedField field)
    {
        if (fieldVal.ValueKind != JsonValueKind.Object) return;
        if (!fieldVal.TryGetProperty("boundingRegions", out var regions)) return;
        if (regions.ValueKind != JsonValueKind.Array) return;

        field.BoundingRegions = [];
        foreach (var region in regions.EnumerateArray())
        {
            var br = new BoundingRegion();
            if (region.TryGetProperty("pageNumber", out var pn) && pn.TryGetInt32(out var pageNum))
                br.PageNumber = pageNum;
            if (region.TryGetProperty("polygon", out var polygon) && polygon.ValueKind == JsonValueKind.Array)
            {
                foreach (var coord in polygon.EnumerateArray())
                {
                    if (coord.TryGetDouble(out var val))
                        br.Polygon.Add(val);
                }
            }
            if (br.Polygon.Count >= 4)
                field.BoundingRegions.Add(br);
        }
        if (field.BoundingRegions.Count == 0)
            field.BoundingRegions = null;
    }

    /// <summary>
    /// Extracts bounding regions from the SDK's ContentField.Sources property.
    /// DocumentSource instances provide page number and polygon coordinates (as PointF).
    /// Converts to flat [x1,y1,x2,y2,...] format matching the raw JSON extraction path.
    /// </summary>
    private static void ExtractBoundingRegionsFromSdk(ContentField? contentField, ExtractedField field)
    {
        if (contentField?.Sources is not { Length: > 0 }) return;

        field.BoundingRegions = [];
        foreach (var source in contentField.Sources)
        {
            if (source is DocumentSource docSource && docSource.Polygon is { Count: >= 4 })
            {
                var polygon = new List<double>(docSource.Polygon.Count * 2);
                foreach (var point in docSource.Polygon)
                {
                    polygon.Add(point.X);
                    polygon.Add(point.Y);
                }
                field.BoundingRegions.Add(new BoundingRegion
                {
                    PageNumber = docSource.PageNumber,
                    Polygon = polygon
                });
            }
        }
        if (field.BoundingRegions.Count == 0)
            field.BoundingRegions = null;
    }

    /// <summary>
    /// Returns true if the JSON object looks like a collection of field schema definitions
    /// (each sub-property is an object with "type" and "method" properties).
    /// </summary>
    private static bool IsFieldSchemaCollection(JsonElement obj)
    {
        int matches = 0;
        int total = 0;
        foreach (var prop in obj.EnumerateObject())
        {
            total++;
            if (total > 50) break; // large enough sample
            if (prop.Value.ValueKind == JsonValueKind.Object &&
                prop.Value.TryGetProperty("type", out _) &&
                prop.Value.TryGetProperty("method", out _))
            {
                matches++;
            }
        }
        // If most sub-properties look like field definitions, treat as a schema collection
        return total > 0 && matches >= total / 2;
    }

    // --- Usage metrics extraction ---

    private static void ExtractUsageMetrics(AnalysisViewModel result)
    {
        if (string.IsNullOrEmpty(result.RawJson)) return;

        try
        {
            using var doc = JsonDocument.Parse(result.RawJson);
            var root = doc.RootElement;

            // Page count from result.contents array length
            if (root.TryGetProperty("result", out var resultEl))
            {
                if (resultEl.TryGetProperty("contents", out var contents))
                    result.PageCount = contents.GetArrayLength();

                if (resultEl.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("totalTokens", out var tokens))
                        result.TokensUsed = tokens.GetInt32();
                }

                // Extract page angles from nested analyzeResult.pages
                if (resultEl.TryGetProperty("analyzeResult", out var nestedAr))
                    ExtractPageAngles(nestedAr, result);

                // Also check contents[].analyzeResult.pages
                if (result.Pages is null && resultEl.TryGetProperty("contents", out var contentsForPages) &&
                    contentsForPages.ValueKind == JsonValueKind.Array)
                {
                    foreach (var content in contentsForPages.EnumerateArray())
                    {
                        if (content.TryGetProperty("analyzeResult", out var contentAr))
                        {
                            ExtractPageAngles(contentAr, result);
                            if (result.Pages is not null) break;
                        }
                    }
                }
            }

            // Also check top-level contents
            if (result.PageCount is null && root.TryGetProperty("contents", out var topContents))
                result.PageCount = topContents.GetArrayLength();

            // Check top-level analyzeResult.pages
            if (result.Pages is null && root.TryGetProperty("analyzeResult", out var topAr))
                ExtractPageAngles(topAr, result);
        }
        catch
        {
            // Non-fatal
        }
    }

    private static void ExtractPageAngles(JsonElement analyzeResult, AnalysisViewModel result)
    {
        if (!analyzeResult.TryGetProperty("pages", out var pages) ||
            pages.ValueKind != JsonValueKind.Array) return;

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

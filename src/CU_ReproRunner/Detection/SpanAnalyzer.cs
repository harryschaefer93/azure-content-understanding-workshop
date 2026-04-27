using System.Text.Json;
using System.Text.RegularExpressions;

namespace CU_ReproRunner.Detection;

/// <summary>
/// Best-effort detector for multiline values and cross-page spans in a CU analyze result.
/// CU response shape varies between analyzers (prebuilt-read vs prebuilt-documentSearch vs custom).
/// Defensive — returns false + an "evidence" string when span info is unavailable.
/// </summary>
public static class SpanAnalyzer
{
    // Matches CU source span tokens like "D(1, 1.23, 4.56, 1.5, 4.56, 1.5, 5.1, 1.23, 5.1)".
    // Captures the page index (1-based) right after "D(".
    private static readonly Regex s_dPageRegex = new(
        @"D\(\s*(\d+)\s*,",
        RegexOptions.Compiled);

    /// <summary>
    /// Analyze multiline/cross-page evidence directly from a CU field's "source" string and value.
    /// Source format: "D(page,x1,y1,...);D(page,...);..."
    /// </summary>
    public static (bool IsMultiline, bool IsCrossPage, string Evidence) AnalyzeFromSource(
        string? source,
        string? fieldValue)
    {
        var multilineByValue = !string.IsNullOrEmpty(fieldValue) &&
            (fieldValue.Contains('\n') || fieldValue.Contains("\r\n"));

        var pages = new HashSet<int>();
        var dCount = 0;
        if (!string.IsNullOrEmpty(source))
        {
            foreach (Match m in s_dPageRegex.Matches(source))
            {
                dCount++;
                if (int.TryParse(m.Groups[1].Value, out var pg)) pages.Add(pg);
            }
        }

        var crossPage = pages.Count > 1;
        var multilineByPolygon = !multilineByValue && dCount > 1;
        var multiline = multilineByValue || multilineByPolygon;

        var evidenceParts = new List<string>();
        if (multilineByValue) evidenceParts.Add("value contains newline");
        if (multilineByPolygon) evidenceParts.Add("multiple source spans");
        if (crossPage) evidenceParts.Add($"pages: {string.Join(",", pages.OrderBy(p => p))}");
        if (evidenceParts.Count == 0 && pages.Count == 1) evidenceParts.Add($"page {pages.First()}");
        if (evidenceParts.Count == 0) evidenceParts.Add("no span info");

        return (multiline, crossPage, string.Join("; ", evidenceParts));
    }

    public static (bool IsMultiline, bool IsCrossPage, string Evidence) Analyze(
        string? rawJson,
        string fieldName,
        string? fieldValue)
    {
        var multilineByValue = !string.IsNullOrEmpty(fieldValue) &&
            (fieldValue.Contains('\n') || fieldValue.Contains("\r\n"));

        if (string.IsNullOrEmpty(rawJson))
            return (multilineByValue, false, multilineByValue ? "value contains newline" : "no raw JSON");

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var fieldNode = FindFieldNode(doc.RootElement, fieldName);
            if (fieldNode is null)
            {
                return (multilineByValue, false,
                    multilineByValue
                        ? "value contains newline; field node not found in raw JSON"
                        : $"no span info for field '{fieldName}'");
            }

            var pages = new HashSet<int>();
            var sourceStrings = new List<string>();

            CollectSourceEvidence(fieldNode.Value, pages, sourceStrings);

            // Cross-page if more than one distinct page referenced.
            var crossPage = pages.Count > 1;

            // Multiline heuristic: value-newline OR multiple source spans on same page
            // where polygons differ in y. We only have a cheap check: more than one
            // source token on a single page → likely multiple lines.
            var multilineByPolygon = false;
            if (!multilineByValue && sourceStrings.Count > 0)
            {
                // Count D(...) tokens across all source strings; if > 1, treat as multi-span.
                var totalDTokens = sourceStrings.Sum(s => s_dPageRegex.Matches(s).Count);
                if (totalDTokens > 1)
                    multilineByPolygon = true;
            }

            var multiline = multilineByValue || multilineByPolygon;

            var evidenceParts = new List<string>();
            if (multilineByValue) evidenceParts.Add("value contains newline");
            if (multilineByPolygon) evidenceParts.Add("multiple source spans");
            if (crossPage) evidenceParts.Add($"pages: {string.Join(",", pages.OrderBy(p => p))}");
            if (evidenceParts.Count == 0 && pages.Count == 1) evidenceParts.Add($"page {pages.First()}");
            if (evidenceParts.Count == 0) evidenceParts.Add("no span info");

            return (multiline, crossPage, string.Join("; ", evidenceParts));
        }
        catch (Exception ex)
        {
            return (multilineByValue, false, $"span analysis failed: {ex.GetType().Name}");
        }
    }

    private static JsonElement? FindFieldNode(JsonElement root, string fieldName)
    {
        // Walk common CU response shapes looking for fields.{fieldName}.
        // The fieldName may be "Foo" or a flattened "Foo[0].Bar" — we only resolve the leaf-most token here.
        var leaf = LeafNameOf(fieldName);

        foreach (var candidate in EnumerateFieldDictionaries(root))
        {
            if (candidate.TryGetProperty(leaf, out var found))
                return found;
        }
        return null;
    }

    private static string LeafNameOf(string flattened)
    {
        // "Registered_Owners[0].First_Name" -> "First_Name"
        var lastDot = flattened.LastIndexOf('.');
        var name = lastDot >= 0 ? flattened[(lastDot + 1)..] : flattened;
        var bracket = name.IndexOf('[');
        if (bracket >= 0) name = name[..bracket];
        return name;
    }

    private static IEnumerable<JsonElement> EnumerateFieldDictionaries(JsonElement root)
    {
        // Yield every "fields" object we can find in the tree (BFS, depth-limited).
        var queue = new Queue<(JsonElement el, int depth)>();
        queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            var (el, depth) = queue.Dequeue();
            if (depth > 8) continue;

            if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in el.EnumerateObject())
                {
                    if (prop.NameEquals("fields") && prop.Value.ValueKind == JsonValueKind.Object)
                        yield return prop.Value;

                    if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                        queue.Enqueue((prop.Value, depth + 1));
                }
            }
            else if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray())
                    queue.Enqueue((item, depth + 1));
            }
        }
    }

    private static void CollectSourceEvidence(JsonElement node, HashSet<int> pages, List<string> sourceStrings)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in node.EnumerateObject())
            {
                if ((prop.NameEquals("source") || prop.NameEquals("spans")) &&
                    prop.Value.ValueKind == JsonValueKind.String)
                {
                    var s = prop.Value.GetString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        sourceStrings.Add(s);
                        foreach (Match m in s_dPageRegex.Matches(s))
                            if (int.TryParse(m.Groups[1].Value, out var pg)) pages.Add(pg);
                    }
                }

                if (prop.NameEquals("pageNumber") && prop.Value.ValueKind == JsonValueKind.Number &&
                    prop.Value.TryGetInt32(out var pn))
                    pages.Add(pn);

                if (prop.NameEquals("boundingRegions") && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var region in prop.Value.EnumerateArray())
                        if (region.TryGetProperty("pageNumber", out var rpn) && rpn.TryGetInt32(out var rpnVal))
                            pages.Add(rpnVal);
                }

                if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                    CollectSourceEvidence(prop.Value, pages, sourceStrings);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
                CollectSourceEvidence(item, pages, sourceStrings);
        }
    }
}

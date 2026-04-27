using System.Text.Json;

namespace CU_ReproRunner.Detection;

/// <summary>
/// Flattens nested CU field JSON (string / object / array) into a flat list of leaf fields
/// keyed by dotted/indexed paths like "Title_Search[0].Reg_Num".
/// </summary>
public static class RawFieldFlattener
{
    public record FlatField(string Name, string? Value, double? Confidence, string? Source);

    /// <summary>
    /// Walks the CU raw JSON for the first non-empty fields object (per content) and returns
    /// flattened leaf fields. Returns empty if rawJson is null/unparseable or no fields found.
    /// </summary>
    public static IReadOnlyList<FlatField> Flatten(string? rawJson)
    {
        var output = new List<FlatField>();
        if (string.IsNullOrEmpty(rawJson)) return output;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            foreach (var fieldsObj in EnumerateFieldsObjects(doc.RootElement))
            {
                foreach (var prop in fieldsObj.EnumerateObject())
                    Walk(prop.Name, prop.Value, output);

                if (output.Count > 0) break; // first non-empty fields wins
            }
        }
        catch
        {
            // Best-effort
        }

        return output;
    }

    private static void Walk(string path, JsonElement node, List<FlatField> output)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            // Leaf primitive — just emit as-is.
            output.Add(new FlatField(path, node.ToString(), null, null));
            return;
        }

        var type = node.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
            ? t.GetString()
            : null;

        if (type == "array" && node.TryGetProperty("valueArray", out var arr) &&
            arr.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (var item in arr.EnumerateArray())
                Walk($"{path}[{i++}]", item, output);
            return;
        }

        if (type == "object" && node.TryGetProperty("valueObject", out var obj) &&
            obj.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in obj.EnumerateObject())
                Walk($"{path}.{prop.Name}", prop.Value, output);
            return;
        }

        // Leaf scalar field — pull common value shapes.
        string? value = null;
        if (node.TryGetProperty("valueString", out var vs) && vs.ValueKind == JsonValueKind.String)
            value = vs.GetString();
        else if (node.TryGetProperty("valueNumber", out var vn))
            value = vn.ToString();
        else if (node.TryGetProperty("valueInteger", out var vi))
            value = vi.ToString();
        else if (node.TryGetProperty("valueBoolean", out var vb))
            value = vb.ToString();
        else if (node.TryGetProperty("valueDate", out var vd))
            value = vd.ToString();
        else if (node.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
            value = c.GetString();
        else if (node.TryGetProperty("value", out var v))
            value = v.ToString();

        double? confidence = null;
        if (node.TryGetProperty("confidence", out var conf) && conf.TryGetDouble(out var cv))
            confidence = cv;

        string? source = null;
        if (node.TryGetProperty("source", out var src) && src.ValueKind == JsonValueKind.String)
            source = src.GetString();

        output.Add(new FlatField(path, value, confidence, source));
    }

    private static IEnumerable<JsonElement> EnumerateFieldsObjects(JsonElement root)
    {
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
}

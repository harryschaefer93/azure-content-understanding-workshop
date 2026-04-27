using System.Diagnostics;
using CU_ReproRunner.Output;
using CU_ReproRunner.Schema;
using CU_TestHarness.Services;
using Microsoft.Extensions.Logging;

namespace CU_ReproRunner.Commands;

public class RunReproCommand
{
    private readonly ContentUnderstandingService _cu;
    private readonly ILogger<RunReproCommand> _log;
    private readonly string _defaultCompletion;
    private readonly string _defaultEmbedding;

    public RunReproCommand(
        ContentUnderstandingService cu,
        ILogger<RunReproCommand> log,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _cu = cu;
        _log = log;
        _defaultCompletion = config["ModelDeployments:DefaultCompletion"] ?? "gpt-4.1-mini";
        _defaultEmbedding = config["ModelDeployments:DefaultEmbedding"] ?? "text-embedding-3-large";
    }

    public string DefaultCompletion => _defaultCompletion;
    public string DefaultEmbedding => _defaultEmbedding;

    public static string HelpText => """
        run-repro — Create/update a residency-safe analyzer from a customer schema, then analyze a folder of PDFs.

        Options:
          --docs <dir>             Directory of PDFs to analyze. Default: test-docs/SyntheticTitleSearchDocswithSchema
          --schema <file>          Customer fieldSchema JSON. Default: <docs>/Title_Search_Sample_Schema_(996658-996659).json
          --analyzer-id <id>       Analyzer ID (alphanumeric + underscore). Default: repro_title_search_v1
          --completion-model <id>  Completion deployment name. Default from config (gpt-4.1-mini).
          --embedding-model <id>   Embedding deployment name.  Default from config (text-embedding-3-large).
          --output-dir <dir>       Output root. Default: output/
          --allow-replace          Replace analyzer if it already exists.
          --help                   Show this help.
        """;

    public async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine(HelpText);
            return 0;
        }

        var docs = ArgParser.GetValue(args, "--docs") ?? "test-docs/SyntheticTitleSearchDocswithSchema";
        var schema = ArgParser.GetValue(args, "--schema")
            ?? Path.Combine(docs, "Title_Search_Sample_Schema_(996658-996659).json");
        var analyzerId = ArgParser.GetValue(args, "--analyzer-id") ?? "repro_title_search_v1";
        var completionModel = ArgParser.GetValue(args, "--completion-model") ?? _defaultCompletion;
        var embeddingModel = ArgParser.GetValue(args, "--embedding-model") ?? _defaultEmbedding;
        var outputDir = ArgParser.GetValue(args, "--output-dir") ?? "output";
        var allowReplace = ArgParser.HasFlag(args, "--allow-replace");

        if (analyzerId.Contains('-'))
        {
            Console.Error.WriteLine($"✖ Invalid analyzer-id '{analyzerId}': hyphens not allowed by CU; use underscores.");
            return 1;
        }

        if (!File.Exists(schema))
        {
            Console.Error.WriteLine($"✖ Schema file not found: {schema}");
            return 1;
        }

        if (!Directory.Exists(docs))
        {
            Console.Error.WriteLine($"✖ Docs dir not found: {docs}");
            return 1;
        }

        Console.WriteLine($"Wrapping schema {schema} → analyzer '{analyzerId}'");
        Console.WriteLine($"  completion: {completionModel}");
        Console.WriteLine($"  embedding:  {embeddingModel}");

        var sourceSchema = await File.ReadAllTextAsync(schema, ct);
        var wrapped = SchemaWrapper.Wrap(sourceSchema, analyzerId, completionModel, embeddingModel);

        var (ok, error) = await _cu.CreateOrUpdateAnalyzerAsync(wrapped, allowReplace, ct);
        if (!ok)
        {
            if (!allowReplace && (error?.Contains("Conflict") == true || error?.Contains("409") == true))
            {
                Console.WriteLine($"⚠ Analyzer '{analyzerId}' already exists — continuing with existing definition. Pass --allow-replace to overwrite.");
            }
            else
            {
                Console.Error.WriteLine($"✖ CreateOrUpdateAnalyzer failed: {error}");
                return 1;
            }
        }
        else
        {
            Console.WriteLine($"✓ Analyzer '{analyzerId}' created/updated.");
        }

        var pdfs = Directory.EnumerateFiles(docs, "*.pdf", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        if (pdfs.Count == 0)
        {
            Console.Error.WriteLine($"✖ No PDFs in {docs}");
            return 1;
        }

        var sw = Stopwatch.StartNew();
        int failed = 0;
        foreach (var pdf in pdfs)
        {
            var docName = Path.GetFileNameWithoutExtension(pdf);
            var perDocSw = Stopwatch.StartNew();
            try
            {
                await using var fs = File.OpenRead(pdf);
                var result = await _cu.AnalyzeFileAsync(analyzerId, fs, Path.GetFileName(pdf), "application/pdf", ct);
                perDocSw.Stop();

                var dir = Path.Combine(outputDir, docName, analyzerId);
                await JsonWriter.WriteResultAsync(Path.Combine(dir, "result.json"), result, ct);
                await MarkdownWriter.WriteResultAsync(Path.Combine(dir, "result.md"), result, ct);

                var status = result.Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ? "ok" : "fail";
                if (status == "fail") failed++;
                Console.WriteLine($"[{status,-4}] {docName,-40} {perDocSw.ElapsedMilliseconds,6} ms  {result.Fields.Count,3} fields");
            }
            catch (Exception ex)
            {
                perDocSw.Stop();
                failed++;
                Console.Error.WriteLine($"[fail] {docName,-40} {perDocSw.ElapsedMilliseconds,6} ms  ✖ {ex.Message}");
            }
        }
        sw.Stop();
        Console.WriteLine($"Done in {sw.ElapsedMilliseconds} ms. {pdfs.Count - failed}/{pdfs.Count} ok.");
        return failed == 0 ? 0 : 1;
    }
}

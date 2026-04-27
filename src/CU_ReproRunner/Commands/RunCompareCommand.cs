using System.Diagnostics;
using CU_ReproRunner.Output;
using CU_TestHarness.Services;
using Microsoft.Extensions.Logging;

namespace CU_ReproRunner.Commands;

public class RunCompareCommand
{
    private readonly ContentUnderstandingService _cu;
    private readonly ILogger<RunCompareCommand> _log;

    public RunCompareCommand(ContentUnderstandingService cu, ILogger<RunCompareCommand> log)
    {
        _cu = cu;
        _log = log;
    }

    public static string HelpText => """
        run-compare — Analyze each PDF with multiple analyzers; emit per-(doc,analyzer) results + a summary table.

        Options:
          --docs <dir>             Directory of PDFs. Default: test-docs/SyntheticTitleSearchDocswithSchema
          --schema <file>          (Reserved; not used directly — analyzers must already exist.)
          --analyzers <csv>        Comma-separated analyzer IDs. Default:
                                   prebuilt-read,prebuilt-layout,prebuilt-documentSearch,repro_title_search_v1
          --output-dir <dir>       Output root. Default: output/
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
        var outputDir = ArgParser.GetValue(args, "--output-dir") ?? "output";
        var analyzersCsv = ArgParser.GetValue(args, "--analyzers")
            ?? "prebuilt-read,prebuilt-layout,prebuilt-documentSearch,repro_title_search_v1";
        var analyzers = analyzersCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!Directory.Exists(docs))
        {
            Console.Error.WriteLine($"✖ Docs dir not found: {docs}");
            return 1;
        }

        var pdfs = Directory.EnumerateFiles(docs, "*.pdf", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        if (pdfs.Count == 0)
        {
            Console.Error.WriteLine($"✖ No PDFs in {docs}");
            return 1;
        }

        var rows = new List<SummaryRow>();
        var sw = Stopwatch.StartNew();

        foreach (var pdf in pdfs)
        {
            var docName = Path.GetFileNameWithoutExtension(pdf);
            foreach (var analyzer in analyzers)
            {
                var perSw = Stopwatch.StartNew();
                try
                {
                    await using var fs = File.OpenRead(pdf);
                    var result = await _cu.AnalyzeFileAsync(analyzer, fs, Path.GetFileName(pdf), "application/pdf", ct);
                    perSw.Stop();

                    var dir = Path.Combine(outputDir, docName, analyzer);
                    await JsonWriter.WriteResultAsync(Path.Combine(dir, "result.json"), result, ct);
                    await MarkdownWriter.WriteResultAsync(Path.Combine(dir, "result.md"), result, ct);

                    rows.Add(SummaryTableWriter.Build(docName, analyzer, result));
                    var status = result.Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ? "ok" : "fail";
                    Console.WriteLine($"[{status,-4}] {docName,-40} {analyzer,-30} {perSw.ElapsedMilliseconds,6} ms  {result.Fields.Count,3} fields");
                }
                catch (Exception ex)
                {
                    perSw.Stop();
                    Console.Error.WriteLine($"[fail] {docName,-40} {analyzer,-30} {perSw.ElapsedMilliseconds,6} ms  ✖ {ex.Message}");
                }
            }
        }

        sw.Stop();
        var summaryPath = Path.Combine(outputDir, "summary_table.md");
        await SummaryTableWriter.WriteAsync(summaryPath, rows, ct);
        Console.WriteLine($"Wrote {summaryPath}");
        Console.WriteLine($"Done in {sw.ElapsedMilliseconds} ms.");
        return 0;
    }
}

using CU_ReproRunner.Output;

namespace CU_ReproRunner.Commands;

public class RunDiffCommand
{
    public static string HelpText => """
        run-diff — Compare actual analyze results vs expected/<doc>.expected.json files.

        Options:
          --actual-dir <dir>    Where result.json lives. Default: output/
          --expected-dir <dir>  Where <doc>.expected.json files live. Default: expected/
          --seed-missing        If an expected file is missing, seed one from the actual result.
          --help                Show this help.

        Exit code: 0 if all expected fields match (or partially match); 1 if any miss.
        """;

    public async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine(HelpText);
            return 0;
        }

        var actualDir = ArgParser.GetValue(args, "--actual-dir") ?? "output";
        var expectedDir = ArgParser.GetValue(args, "--expected-dir") ?? "expected";
        var seedMissing = ArgParser.HasFlag(args, "--seed-missing");

        var (misses, diffs) = await DiffReportWriter.RunAsync(actualDir, expectedDir, seedMissing, ct);
        var reportPath = Path.Combine(actualDir, "diff_report.md");
        await DiffReportWriter.WriteAsync(reportPath, diffs, ct);
        Console.WriteLine($"Wrote {reportPath}");
        Console.WriteLine($"Misses: {misses}");
        return misses == 0 ? 0 : 1;
    }
}

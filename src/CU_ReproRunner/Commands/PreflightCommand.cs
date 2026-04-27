using CU_TestHarness.Models;
using CU_TestHarness.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CU_ReproRunner.Commands;

public class PreflightCommand
{
    private readonly ContentUnderstandingService _cu;
    private readonly ContentUnderstandingOptions _options;
    private readonly ILogger<PreflightCommand> _log;

    public PreflightCommand(ContentUnderstandingService cu, IOptions<ContentUnderstandingOptions> options, ILogger<PreflightCommand> log)
    {
        _cu = cu;
        _options = options.Value;
        _log = log;
    }

    public static string HelpText => """
        preflight — Probe configured CU endpoint, list defaults + analyzers, flag residency-unsafe routing.

        Usage:
          CU_ReproRunner preflight

        Reads endpoint from appsettings.json or env vars:
          ContentUnderstanding__Endpoint
          ContentUnderstanding__ModelsEndpoint
        """;

    public async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine(HelpText);
            return 0;
        }

        Console.WriteLine($"Endpoint:       {_options.Endpoint}");
        Console.WriteLine($"ModelsEndpoint: {_options.ModelsEndpoint}");
        Console.WriteLine();

        var defaults = await _cu.GetDefaultsAsync(ct);
        if (defaults is null)
        {
            Console.Error.WriteLine("✖ Could not fetch CU defaults — endpoint unreachable or auth failed.");
            Console.Error.WriteLine("  Run `az login` and confirm the Endpoint URL.");
            return 1;
        }

        Console.WriteLine("CU defaults (modelDeployments):");
        var hasGlobalStandard = false;
        foreach (var kvp in defaults.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var flag = "";
            if (kvp.Value.Contains("gpt-41", StringComparison.OrdinalIgnoreCase) &&
                !kvp.Value.Contains("gpt-4.1", StringComparison.OrdinalIgnoreCase))
            {
                flag = "  ❌ GlobalStandard (residency risk)";
                hasGlobalStandard = true;
            }
            Console.WriteLine($"  {kvp.Key,-50} = {kvp.Value}{flag}");
        }

        var hasMini = defaults.Values.Any(v => v.Contains("gpt-4.1-mini", StringComparison.OrdinalIgnoreCase));
        var has4o = defaults.Values.Any(v => v.Contains("gpt-4o", StringComparison.OrdinalIgnoreCase));
        var hasEmb = defaults.Values.Any(v => v.Contains("text-embedding-3-large", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine();
        Console.WriteLine("Residency-safe Standard-SKU deployments referenced in defaults:");
        Console.WriteLine($"  gpt-4.1-mini           : {(hasMini ? "yes" : "no")}");
        Console.WriteLine($"  gpt-4o                 : {(has4o ? "yes" : "no")}");
        Console.WriteLine($"  text-embedding-3-large : {(hasEmb ? "yes" : "no")}");

        if (hasGlobalStandard)
        {
            Console.WriteLine();
            Console.WriteLine("⚠  WARNING: At least one default routes to a GlobalStandard model. Re-run defaults PATCH with");
            Console.WriteLine("   infra/defaults-body.json updated to point at gpt-4.1-mini before running residency-bound workloads.");
        }

        Console.WriteLine();
        Console.WriteLine("Existing analyzers:");
        var ids = await _cu.ListAnalyzerIdsAsync(ct);
        if (ids.Count == 0) Console.WriteLine("  (none)");
        foreach (var id in ids.OrderBy(s => s, StringComparer.Ordinal))
            Console.WriteLine($"  - {id}");

        return 0;
    }
}

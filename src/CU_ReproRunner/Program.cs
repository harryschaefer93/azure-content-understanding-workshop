using CU_ReproRunner.Commands;
using CU_TestHarness.Models;
using CU_TestHarness.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CU_ReproRunner;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintTopHelp();
            return args.Length == 0 ? 1 : 0;
        }

        var command = args[0];
        var rest = args.Skip(1).ToArray();

        // Short-circuit: per-command --help shouldn't require valid config / live endpoint.
        if (rest.Contains("--help") || rest.Contains("-h"))
        {
            switch (command)
            {
                case "preflight": Console.WriteLine(PreflightCommand.HelpText); return 0;
                case "run-repro": Console.WriteLine(RunReproCommand.HelpText); return 0;
                case "run-compare": Console.WriteLine(RunCompareCommand.HelpText); return 0;
                case "run-diff": Console.WriteLine(RunDiffCommand.HelpText); return 0;
            }
        }

        var builder = Host.CreateApplicationBuilder();

        // Load appsettings.json from the binary directory, not cwd — cwd is repo root for our usage.
        var appsettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(appsettingsPath))
            builder.Configuration.AddJsonFile(appsettingsPath, optional: true, reloadOnChange: false);
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });

        builder.Services.Configure<ContentUnderstandingOptions>(
            builder.Configuration.GetSection("ContentUnderstanding"));

        builder.Services.AddSingleton<CostEstimator>();
        builder.Services.AddHttpClient<ContentUnderstandingService>();

        builder.Services.AddTransient<PreflightCommand>();
        builder.Services.AddTransient<RunReproCommand>();
        builder.Services.AddTransient<RunCompareCommand>();
        builder.Services.AddTransient<RunDiffCommand>();

        using var host = builder.Build();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            return command switch
            {
                "preflight" => await host.Services.GetRequiredService<PreflightCommand>().RunAsync(rest, cts.Token),
                "run-repro" => await host.Services.GetRequiredService<RunReproCommand>().RunAsync(rest, cts.Token),
                "run-compare" => await host.Services.GetRequiredService<RunCompareCommand>().RunAsync(rest, cts.Token),
                "run-diff" => await host.Services.GetRequiredService<RunDiffCommand>().RunAsync(rest, cts.Token),
                _ => UnknownCommand(command),
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Cancelled.");
            return 130;
        }
    }

    private static int UnknownCommand(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}");
        Console.Error.WriteLine();
        PrintTopHelp();
        return 1;
    }

    private static void PrintTopHelp()
    {
        Console.WriteLine("""
            CU_ReproRunner — automate the FCT Canada multiline-extraction repro workflow.

            Usage:
              CU_ReproRunner <command> [options]

            Commands:
              preflight       Probe the configured CU endpoint and report defaults / analyzers.
              run-repro       Create/update an analyzer and analyze a folder of PDFs against it.
              run-compare     Analyze each PDF against multiple analyzers; emit summary table.
              run-diff        Compare actual results vs expected/<doc>.expected.json files.

            Run `CU_ReproRunner <command> --help` for command-specific options.

            Configuration:
              appsettings.json (alongside the binary), overridable via env vars:
                ContentUnderstanding__Endpoint
                ContentUnderstanding__ModelsEndpoint
                ModelDeployments__DefaultCompletion
                ModelDeployments__DefaultEmbedding

            Auth: DefaultAzureCredential (Entra). Run `az login` first.
            """);
    }
}

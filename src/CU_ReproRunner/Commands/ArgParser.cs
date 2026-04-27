namespace CU_ReproRunner.Commands;

/// <summary>
/// Tiny hand-rolled arg parser. Avoids the System.CommandLine prerelease churn.
/// Supports `--name value` and `--flag` forms. Unknown args are ignored
/// (the per-command Help text documents the supported set).
/// </summary>
public static class ArgParser
{
    public static string? GetValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
                return args[i + 1];
        }
        return null;
    }

    public static bool HasFlag(string[] args, string name)
    {
        return args.Any(a => string.Equals(a, name, StringComparison.Ordinal));
    }
}

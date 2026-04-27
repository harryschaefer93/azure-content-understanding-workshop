namespace CU_TestHarness.Models;

/// <summary>
/// Static registry mapping prebuilt analyzers to their model requirements
/// and the server-side default key they resolve to.
/// </summary>
public static class AnalyzerMetadata
{
    public enum ModelRequirement
    {
        /// <summary>No model needed — pure OCR / content extraction.</summary>
        None,
        /// <summary>Uses pretrained ML models — no configurable model deployment.</summary>
        Pretrained,
        /// <summary>Requires a full completion model (e.g. gpt-4.1, gpt-4o). Mini models NOT supported.</summary>
        Completion,
        /// <summary>Requires a completion-mini model.</summary>
        CompletionMini,
        /// <summary>Requires both completion and embedding models.</summary>
        CompletionAndEmbedding
    }

    public record AnalyzerInfo
    {
        public required ModelRequirement Requirement { get; init; }
        /// <summary>The server-side default key(s) this analyzer resolves to, if any.</summary>
        public string[] DefaultKeys { get; init; } = [];
        /// <summary>Human-readable description of the model requirement.</summary>
        public required string Description { get; init; }
    }

    public static readonly Dictionary<string, AnalyzerInfo> Registry = new()
    {
        // Content extraction — no model needed
        ["prebuilt-read"] = new()
        {
            Requirement = ModelRequirement.None,
            Description = "OCR only — no model deployment needed"
        },
        ["prebuilt-layout"] = new()
        {
            Requirement = ModelRequirement.None,
            Description = "OCR + layout — no model deployment needed"
        },

        // Base document — pretrained, no configurable model
        ["prebuilt-document"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained document model — no configurable deployment"
        },

        // Domain-specific — pretrained ML
        ["prebuilt-invoice"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained invoice model — no configurable deployment"
        },
        ["prebuilt-receipt"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained receipt model — no configurable deployment"
        },
        ["prebuilt-idDocument"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained ID document model — no configurable deployment"
        },
        ["prebuilt-creditCard"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained credit card model — no configurable deployment"
        },
        ["prebuilt-contract"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained contract model — no configurable deployment"
        },
        ["prebuilt-utilityBill"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained utility bill model — no configurable deployment"
        },
        ["prebuilt-payStub.us"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained pay stub model — no configurable deployment"
        },
        ["prebuilt-healthInsuranceCard.us"] = new()
        {
            Requirement = ModelRequirement.Pretrained,
            Description = "Pretrained health insurance card model — no configurable deployment"
        },

        // Utility — requires full completion model (NOT mini)
        ["prebuilt-documentFields"] = new()
        {
            Requirement = ModelRequirement.Completion,
            DefaultKeys = ["prebuilt-analyzer-completion"],
            Description = "Requires full completion model (gpt-4.1 or gpt-4o) — mini NOT supported"
        },
        ["prebuilt-documentFieldSchema"] = new()
        {
            Requirement = ModelRequirement.Completion,
            DefaultKeys = ["prebuilt-analyzer-completion"],
            Description = "Requires full completion model (gpt-4.1 or gpt-4o) — mini NOT supported"
        },

        // RAG — requires completion + embedding
        ["prebuilt-documentSearch"] = new()
        {
            Requirement = ModelRequirement.CompletionAndEmbedding,
            DefaultKeys = ["prebuilt-analyzer-completion", "prebuilt-analyzer-embedding"],
            Description = "Requires completion + embedding models"
        },
        ["prebuilt-imageSearch"] = new()
        {
            Requirement = ModelRequirement.CompletionAndEmbedding,
            DefaultKeys = ["prebuilt-analyzer-completion", "prebuilt-analyzer-embedding"],
            Description = "Requires completion + embedding models"
        },
        ["prebuilt-audioSearch"] = new()
        {
            Requirement = ModelRequirement.CompletionAndEmbedding,
            DefaultKeys = ["prebuilt-analyzer-completion", "prebuilt-analyzer-embedding"],
            Description = "Requires completion + embedding models"
        },
        ["prebuilt-videoSearch"] = new()
        {
            Requirement = ModelRequirement.CompletionAndEmbedding,
            DefaultKeys = ["prebuilt-analyzer-completion", "prebuilt-analyzer-embedding"],
            Description = "Requires completion + embedding models"
        }
    };

    /// <summary>
    /// Get metadata for an analyzer, returning a generic "custom analyzer" entry for unknown IDs.
    /// </summary>
    public static AnalyzerInfo GetInfo(string analyzerId)
    {
        if (Registry.TryGetValue(analyzerId, out var info))
            return info;

        return new AnalyzerInfo
        {
            Requirement = ModelRequirement.CompletionAndEmbedding,
            Description = "Custom analyzer — uses model block from analyzer definition"
        };
    }

    /// <summary>
    /// Check if a server-side default mapping may cause a compatibility issue for this analyzer.
    /// Returns a warning message or null if OK.
    /// </summary>
    public static string? CheckCompatibility(string analyzerId, Dictionary<string, string>? defaults)
    {
        if (defaults is null) return null;

        var info = GetInfo(analyzerId);
        if (info.Requirement == ModelRequirement.None || info.Requirement == ModelRequirement.Pretrained)
            return null;

        if (info.Requirement == ModelRequirement.Completion)
        {
            // prebuilt-documentFields/Schema require a full completion model — mini is NOT supported
            if (defaults.TryGetValue("prebuilt-analyzer-completion", out var mapping))
            {
                var deploymentName = mapping.Contains('/') ? mapping.Split('/').Last() : mapping;
                if (deploymentName.Contains("mini", StringComparison.OrdinalIgnoreCase))
                {
                    return $"⚠️ Server default 'prebuilt-analyzer-completion' maps to '{deploymentName}' — " +
                           $"this analyzer requires a full completion model (gpt-4.1 or gpt-4o). Mini models are NOT supported.";
                }
            }
        }

        return null;
    }
}

namespace CU_TestHarness.Models;

/// <summary>
/// A completion model deployment (GPT).
/// </summary>
public record CompletionProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string DeploymentName { get; init; }
    public required string ModelName { get; init; }
    public required string DeploymentType { get; init; }  // "Global Standard" or "Standard"
    public required bool DataResidencyGuaranteed { get; init; }
    public required string Region { get; init; }
    public required int ContextWindow { get; init; }
    public required int MaxOutputTokens { get; init; }
    public string? TrainingData { get; init; }

    public static IReadOnlyList<CompletionProfile> Available { get; } =
    [
        new()
        {
            Id = "gpt41-global",
            DisplayName = "GPT-4.1",
            DeploymentName = "gpt-41",
            ModelName = "gpt-4.1",
            DeploymentType = "Global Standard",
            DataResidencyGuaranteed = false,
            Region = "Canada East",
            ContextWindow = 1_048_576,
            MaxOutputTokens = 32_768,
            TrainingData = "Jun 2024"
        },
        new()
        {
            Id = "gpt41-mini-global",
            DisplayName = "GPT-4.1 Mini",
            DeploymentName = "gpt-41-mini",
            ModelName = "gpt-4.1-mini",
            DeploymentType = "Global Standard",
            DataResidencyGuaranteed = false,
            Region = "Canada East",
            ContextWindow = 1_048_576,
            MaxOutputTokens = 32_768,
            TrainingData = "Jun 2024"
        },
        new()
        {
            Id = "gpt41-mini-canada",
            DisplayName = "GPT-4.1 Mini",
            DeploymentName = "gpt-41-mini-ca",
            ModelName = "gpt-4.1-mini",
            DeploymentType = "Standard",
            DataResidencyGuaranteed = true,
            Region = "Canada East",
            ContextWindow = 1_048_576,
            MaxOutputTokens = 32_768,
            TrainingData = "Jun 2024"
        },
        new()
        {
            Id = "gpt4o-canada",
            DisplayName = "GPT-4o",
            DeploymentName = "gpt-4o-ca",
            ModelName = "gpt-4o",
            DeploymentType = "Standard",
            DataResidencyGuaranteed = true,
            Region = "Canada East",
            ContextWindow = 128_000,
            MaxOutputTokens = 16_384,
            TrainingData = "Oct 2023"
        }
    ];
}

/// <summary>
/// An embedding model deployment.
/// </summary>
public record EmbeddingProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string DeploymentName { get; init; }
    public required string ModelName { get; init; }
    public required string DeploymentType { get; init; }
    public required string Region { get; init; }
    public required int MaxTokens { get; init; }
    public required int Dimensions { get; init; }

    public static IReadOnlyList<EmbeddingProfile> Available { get; } =
    [
        new()
        {
            Id = "ada-002",
            DisplayName = "Ada 002",
            DeploymentName = "text-embedding-ada-002",
            ModelName = "text-embedding-ada-002",
            DeploymentType = "Standard",
            Region = "Canada East",
            MaxTokens = 8_191,
            Dimensions = 1_536
        },
        new()
        {
            Id = "embed-3-large",
            DisplayName = "Embedding 3 Large",
            DeploymentName = "text-embedding-3-large",
            ModelName = "text-embedding-3-large",
            DeploymentType = "Standard",
            Region = "Canada East",
            MaxTokens = 8_191,
            Dimensions = 3_072
        },
        new()
        {
            Id = "embed-3-small",
            DisplayName = "Embedding 3 Small",
            DeploymentName = "text-embedding-3-small",
            ModelName = "text-embedding-3-small",
            DeploymentType = "Standard",
            Region = "Canada East",
            MaxTokens = 8_191,
            Dimensions = 1_536
        }
    ];
}

/// <summary>
/// Composed view of a completion + embedding selection.
/// Provides backward-compat properties used by SchemaBuilder, SchemaEditor, and Analyze pages.
/// </summary>
public record ModelProfile
{
    public required CompletionProfile Completion { get; init; }
    public required EmbeddingProfile Embedding { get; init; }

    public string Id => $"{Completion.Id}+{Embedding.Id}";
    public string DisplayName => $"{Completion.DisplayName} + {Embedding.DisplayName}";

    // Compat shims used by SchemaBuilder/SchemaEditor/Analyze
    public string CompletionDeploymentName => Completion.DeploymentName;
    public string CompletionModelName => Completion.ModelName;
    public string EmbeddingDeploymentName => Embedding.DeploymentName;
    public string EmbeddingModelName => Embedding.ModelName;
    public string DeploymentType => Completion.DeploymentType;
    public bool DataResidencyGuaranteed => Completion.DataResidencyGuaranteed;
    public string Region => Completion.Region;

    /// <summary>
    /// All available combinations (cross-product of completion × embedding profiles).
    /// Used by SchemaBuilder/SchemaEditor per-page profile selector.
    /// </summary>
    public static IReadOnlyList<ModelProfile> AvailableProfiles { get; } =
        (from c in CompletionProfile.Available
         from e in EmbeddingProfile.Available
         select new ModelProfile { Completion = c, Embedding = e })
        .ToList();
}

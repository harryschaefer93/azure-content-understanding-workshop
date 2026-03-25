namespace CU_TestHarness.Models;

/// <summary>
/// Runtime singleton holding the active completion and embedding selections.
/// Injected across pages so model choice propagates without restart.
/// </summary>
public class ModelProfileState
{
    private CompletionProfile _completion;
    private EmbeddingProfile _embedding;

    public ModelProfileState(string defaultCompletionId, string defaultEmbeddingId)
    {
        _completion = CompletionProfile.Available.FirstOrDefault(p => p.Id == defaultCompletionId)
                      ?? CompletionProfile.Available[0];
        _embedding = EmbeddingProfile.Available.FirstOrDefault(p => p.Id == defaultEmbeddingId)
                     ?? EmbeddingProfile.Available[0];
    }

    public CompletionProfile ActiveCompletion => _completion;
    public EmbeddingProfile ActiveEmbedding => _embedding;

    /// <summary>Composed view for backward compatibility.</summary>
    public ModelProfile ActiveProfile => new()
    {
        Completion = _completion,
        Embedding = _embedding
    };

    public void SetCompletion(string id)
    {
        var p = CompletionProfile.Available.FirstOrDefault(x => x.Id == id);
        if (p is not null) _completion = p;
    }

    public void SetEmbedding(string id)
    {
        var p = EmbeddingProfile.Available.FirstOrDefault(x => x.Id == id);
        if (p is not null) _embedding = p;
    }
}

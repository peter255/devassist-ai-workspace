namespace DevAssist.Infrastructure.Options;

public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAi";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Chat completion deployment name (e.g. gpt-4o, gpt-35-turbo).</summary>
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Embedding deployment name (e.g. text-embedding-3-small, text-embedding-ada-002).
    /// When empty, falls back to <see cref="DeploymentName"/> for backward compat.
    /// </summary>
    public string EmbeddingDeploymentName { get; set; } = string.Empty;

    /// <summary>Effective embedding deployment — falls back to chat DeploymentName when not set.</summary>
    public string EffectiveEmbeddingDeployment =>
        string.IsNullOrWhiteSpace(EmbeddingDeploymentName) ? DeploymentName : EmbeddingDeploymentName;
}

public sealed class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Semantic ranker configuration name defined on the Azure AI Search index.
    /// When non-empty, semantic search is enabled alongside vector/hybrid retrieval.
    /// Leave empty to use keyword + vector hybrid search without semantic ranking.
    /// </summary>
    public string SemanticConfigurationName { get; set; } = string.Empty;

    /// <summary>
    /// Dimension of the embedding vectors stored in the index.
    /// Must match the model's output dimension (text-embedding-3-small = 1536, ada-002 = 1536).
    /// </summary>
    public int VectorDimensions { get; set; } = 1536;
}

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
}

/// <summary>
/// Azure Document Intelligence (formerly Form Recognizer) options.
/// When Endpoint and ApiKey are set, scanned/image PDFs are processed via the
/// prebuilt-read model instead of falling back to PdfPig text extraction.
/// </summary>
public sealed class DocumentIntelligenceOptions
{
    public const string SectionName = "DocumentIntelligence";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "devassist-indexing";
}

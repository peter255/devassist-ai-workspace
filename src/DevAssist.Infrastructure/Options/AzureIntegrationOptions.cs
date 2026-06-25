namespace DevAssist.Infrastructure.Options;

public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAi";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}

public sealed class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
}

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
}

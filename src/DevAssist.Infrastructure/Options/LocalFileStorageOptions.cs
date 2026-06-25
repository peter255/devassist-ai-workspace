namespace DevAssist.Infrastructure.Options;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "LocalFileStorage";
    public string RootPath { get; set; } = "./data/documents";
}

using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Application.Interfaces.Requirements;
using DevAssist.Application.Interfaces.Tickets;
using DevAssist.Infrastructure.Copilot;
using DevAssist.Infrastructure.Copilot.OpenAi;
using DevAssist.Infrastructure.Copilot.Prompting;
using DevAssist.Infrastructure.Copilot.Search;
using DevAssist.Infrastructure.Documents;
using DevAssist.Infrastructure.Documents.Chunking;
using DevAssist.Infrastructure.Documents.Embeddings;
using DevAssist.Infrastructure.Documents.Extraction;
using DevAssist.Infrastructure.Documents.Indexing;
using DevAssist.Infrastructure.Documents.Search;
using DevAssist.Infrastructure.Documents.Storage;
using DevAssist.Infrastructure.Tickets;
using DevAssist.Infrastructure.Tickets.Analysis;
using DevAssist.Infrastructure.Tickets.Prompting;
using DevAssist.Infrastructure.Requirements;
using DevAssist.Infrastructure.Requirements.Analysis;
using DevAssist.Infrastructure.Requirements.Prompting;
using DevAssist.Infrastructure.Options;
using DevAssist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevAssist.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DevAssistDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DevAssistDb")));

        services.Configure<AzureOpenAiOptions>(options =>
        {
            options.Endpoint = configuration[$"{AzureOpenAiOptions.SectionName}:Endpoint"] ?? string.Empty;
            options.ApiKey = configuration[$"{AzureOpenAiOptions.SectionName}:ApiKey"] ?? string.Empty;
            options.DeploymentName = configuration[$"{AzureOpenAiOptions.SectionName}:DeploymentName"] ?? string.Empty;
        });
        services.Configure<AzureSearchOptions>(options =>
        {
            options.Endpoint = configuration[$"{AzureSearchOptions.SectionName}:Endpoint"] ?? string.Empty;
            options.ApiKey = configuration[$"{AzureSearchOptions.SectionName}:ApiKey"] ?? string.Empty;
            options.IndexName = configuration[$"{AzureSearchOptions.SectionName}:IndexName"] ?? "devassist-documents";
        });
        services.Configure<BlobStorageOptions>(options =>
        {
            options.ConnectionString = configuration[$"{BlobStorageOptions.SectionName}:ConnectionString"] ?? string.Empty;
            options.ContainerName = configuration[$"{BlobStorageOptions.SectionName}:ContainerName"] ?? "documents";
        });
        services.Configure<LocalFileStorageOptions>(options =>
        {
            options.RootPath = configuration[$"{LocalFileStorageOptions.SectionName}:RootPath"] ?? "./data/documents";
        });

        RegisterDocumentStorage(services, configuration);
        RegisterSearchIndexer(services, configuration);
        RegisterSearchRetriever(services, configuration);
        RegisterEmbeddingService(services, configuration);
        RegisterChatService(services, configuration);
        RegisterTicketAnalyzer(services, configuration);
        RegisterRequirementBreakdown(services, configuration);

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ITicketAnalysisRepository, TicketAnalysisRepository>();
        services.AddScoped<ITicketAnalyzerPromptBuilder, TicketAnalyzerPromptBuilder>();
        services.AddScoped<IRequirementAnalysisRepository, RequirementAnalysisRepository>();
        services.AddScoped<IRequirementBreakdownPromptBuilder, RequirementBreakdownPromptBuilder>();
        services.AddScoped<ITextChunkingService, TextChunkingService>();
        services.AddScoped<IDocumentTextExtractor, PlainTextDocumentExtractor>();
        services.AddScoped<IDocumentTextExtractor, UnsupportedDocumentExtractor>();
        services.AddScoped<DocumentTextExtractionService>();
        services.AddScoped<IDocumentIndexingOrchestrator, DocumentIndexingOrchestrator>();
        services.AddScoped<IDocumentIndexingService, DocumentIndexingServiceAdapter>();
        services.AddScoped<ICopilotPromptBuilder, CopilotPromptBuilder>();
        services.AddScoped<IKnowledgeCopilotService, KnowledgeCopilotService>();

        return services;
    }

    private static void RegisterDocumentStorage(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration[$"{BlobStorageOptions.SectionName}:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddScoped<IDocumentStorageService, LocalFileDocumentStorageService>();
            return;
        }

        services.AddScoped<IDocumentStorageService, AzureBlobDocumentStorageService>();
    }

    private static void RegisterSearchIndexer(IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration[$"{AzureSearchOptions.SectionName}:Endpoint"];
        var apiKey = configuration[$"{AzureSearchOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IDocumentSearchIndexer, NoOpDocumentSearchIndexer>();
            return;
        }

        services.AddScoped<IDocumentSearchIndexer, AzureSearchDocumentIndexer>();
    }

    private static void RegisterSearchRetriever(IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration[$"{AzureSearchOptions.SectionName}:Endpoint"];
        var apiKey = configuration[$"{AzureSearchOptions.SectionName}:ApiKey"];

        // Always register the SQL retriever as the primary retriever because document chunks
        // are stored in SQL by DocumentIndexingOrchestrator. AzureSearchDocumentRetriever is
        // also registered and is used when Azure Search is configured AND documents have been
        // indexed there. The SQL retriever ensures the Copilot always has access to locally
        // indexed documents regardless of Azure Search availability.
        services.AddScoped<SqlDocumentSearchRetriever>();

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IDocumentSearchRetriever, SqlDocumentSearchRetriever>();
            return;
        }

        services.AddScoped<AzureSearchDocumentRetriever>();
        services.AddScoped<IDocumentSearchRetriever, HybridDocumentSearchRetriever>();
    }

    private static void RegisterEmbeddingService(IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration[$"{AzureOpenAiOptions.SectionName}:Endpoint"];
        var apiKey = configuration[$"{AzureOpenAiOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IEmbeddingService, PlaceholderEmbeddingService>();
            return;
        }

        services.AddScoped<IEmbeddingService, AzureOpenAiEmbeddingService>();
    }

    private static void RegisterChatService(IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration[$"{AzureOpenAiOptions.SectionName}:Endpoint"];
        var apiKey = configuration[$"{AzureOpenAiOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IAzureOpenAiChatService, LocalGroundedChatService>();
            return;
        }

        services.AddScoped<IAzureOpenAiChatService, AzureOpenAiChatService>();
    }

    private static void RegisterTicketAnalyzer(IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration[$"{AzureOpenAiOptions.SectionName}:Endpoint"];
        var apiKey = configuration[$"{AzureOpenAiOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<ITicketAnalyzerService, LocalTicketAnalyzerService>();
            return;
        }

        services.AddScoped<ITicketAnalyzerService, AzureOpenAiTicketAnalyzerService>();
    }

    private static void RegisterRequirementBreakdown(IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration[$"{AzureOpenAiOptions.SectionName}:Endpoint"];
        var apiKey = configuration[$"{AzureOpenAiOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IRequirementBreakdownService, LocalRequirementBreakdownService>();
            return;
        }

        services.AddScoped<IRequirementBreakdownService, AzureOpenAiRequirementBreakdownService>();
    }
}

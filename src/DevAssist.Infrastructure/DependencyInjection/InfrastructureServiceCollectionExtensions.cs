using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Application.Interfaces.Requirements;
using DevAssist.Application.Interfaces.Tickets;
using DevAssist.Infrastructure.Agents;
using DevAssist.Infrastructure.Copilot;
using DevAssist.Infrastructure.Copilot.OpenAi;
using DevAssist.Infrastructure.Copilot.Prompting;
using DevAssist.Infrastructure.Copilot.Search;
using DevAssist.Infrastructure.Documents;
using DevAssist.Infrastructure.Documents.BackgroundIndexing;
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

        // Bind configuration sections to typed options.
        services.Configure<AzureOpenAiOptions>(options =>
        {
            options.Endpoint = configuration[$"{AzureOpenAiOptions.SectionName}:Endpoint"] ?? string.Empty;
            options.ApiKey = configuration[$"{AzureOpenAiOptions.SectionName}:ApiKey"] ?? string.Empty;
            options.DeploymentName = configuration[$"{AzureOpenAiOptions.SectionName}:DeploymentName"] ?? string.Empty;
            options.EmbeddingDeploymentName = configuration[$"{AzureOpenAiOptions.SectionName}:EmbeddingDeploymentName"] ?? string.Empty;
        });
        services.Configure<AzureSearchOptions>(options =>
        {
            options.Endpoint = configuration[$"{AzureSearchOptions.SectionName}:Endpoint"] ?? string.Empty;
            options.ApiKey = configuration[$"{AzureSearchOptions.SectionName}:ApiKey"] ?? string.Empty;
            options.IndexName = configuration[$"{AzureSearchOptions.SectionName}:IndexName"] ?? "devassist-documents";
            options.SemanticConfigurationName = configuration[$"{AzureSearchOptions.SectionName}:SemanticConfigurationName"] ?? string.Empty;
            if (int.TryParse(configuration[$"{AzureSearchOptions.SectionName}:VectorDimensions"], out var dims) && dims > 0)
                options.VectorDimensions = dims;
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

        RegisterAiAgent(services, configuration);
        RegisterDocumentStorage(services, configuration);
        RegisterSearchIndexer(services, configuration);
        RegisterSearchRetriever(services, configuration);
        RegisterEmbeddingService(services, configuration);
        RegisterTicketAnalyzer(services, configuration);
        RegisterRequirementBreakdown(services, configuration);

        // Repositories & shared services.
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

        // Background indexing — singleton queue shared between HTTP pipeline and background service.
        services.AddSingleton<IDocumentIndexingQueue, DocumentIndexingQueue>();
        services.AddHostedService<BackgroundDocumentIndexingService>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="IAiAgent"/> (Phase 2 abstraction) alongside the legacy
    /// <see cref="IAzureOpenAiChatService"/> used by copilot and module analyzers.
    ///
    /// When Azure OpenAI is configured:
    ///   • <see cref="AzureFoundryAgent"/> is registered for both IAiAgent and IAzureOpenAiChatService.
    ///
    /// When Azure OpenAI is NOT configured:
    ///   • <see cref="LocalFallbackAgent"/> handles IAiAgent.
    ///   • <see cref="LocalGroundedChatService"/> handles IAzureOpenAiChatService (copilot grounding logic).
    /// </summary>
    private static void RegisterAiAgent(IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration[$"{AzureOpenAiOptions.SectionName}:Endpoint"];
        var apiKey = configuration[$"{AzureOpenAiOptions.SectionName}:ApiKey"];
        var isAzureConfigured = !string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey);

        if (isAzureConfigured)
        {
            // AzureFoundryAgent implements both IAiAgent and IAzureOpenAiChatService.
            services.AddScoped<AzureFoundryAgent>();
            services.AddScoped<IAiAgent>(sp => sp.GetRequiredService<AzureFoundryAgent>());
            services.AddScoped<IAzureOpenAiChatService>(sp => sp.GetRequiredService<AzureFoundryAgent>());
        }
        else
        {
            services.AddScoped<IAiAgent, LocalFallbackAgent>();
            services.AddScoped<IAzureOpenAiChatService, LocalGroundedChatService>();
        }
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

        // SqlDocumentSearchRetriever is always registered because chunks are stored in SQL by
        // DocumentIndexingOrchestrator regardless of Azure Search availability.
        services.AddScoped<SqlDocumentSearchRetriever>();

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IDocumentSearchRetriever, SqlDocumentSearchRetriever>();
            return;
        }

        // AzureSearchDocumentRetriever now also receives IEmbeddingService for vector queries.
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

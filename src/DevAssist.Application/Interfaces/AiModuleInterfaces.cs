using DevAssist.Application.Interfaces.Requirements;
using DevAssist.Application.Interfaces.Tickets;
using DevAssist.Contracts.Copilot;

namespace DevAssist.Application.Interfaces;

public interface IDocumentStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string blobPath, CancellationToken cancellationToken);
}

public interface IDocumentIndexingService
{
    Task IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken);
}

public interface IKnowledgeCopilotService
{
    Task<AskCopilotResponse> AskAsync(Guid sessionId, string question, CancellationToken cancellationToken);
}

public interface ITicketAnalyzerService
{
    Task<TicketAnalysisOutput> AnalyzeAsync(string text, CancellationToken cancellationToken);
}

public interface IRequirementBreakdownService
{
    Task<RequirementBreakdownOutput> AnalyzeAsync(string text, CancellationToken cancellationToken);
}

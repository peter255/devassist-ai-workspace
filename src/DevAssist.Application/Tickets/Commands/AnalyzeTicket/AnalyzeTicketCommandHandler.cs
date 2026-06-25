using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Tickets;
using DevAssist.Application.Tickets.Mappings;
using DevAssist.Contracts.Tickets;
using DevAssist.Domain.Entities;
using DevAssist.Domain.Enums;
using FluentValidation;
using MediatR;

namespace DevAssist.Application.Tickets.Commands.AnalyzeTicket;

public sealed class AnalyzeTicketCommandValidator : AbstractValidator<AnalyzeTicketCommand>
{
    public AnalyzeTicketCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(12000);
    }
}

public sealed class AnalyzeTicketCommandHandler(
    ITicketAnalyzerService ticketAnalyzerService,
    ITicketAnalysisRepository ticketAnalysisRepository) : IRequestHandler<AnalyzeTicketCommand, AnalyzeTicketResponse>
{
    public async Task<AnalyzeTicketResponse> Handle(AnalyzeTicketCommand request, CancellationToken cancellationToken)
    {
        var output = await ticketAnalyzerService.AnalyzeAsync(request.Text, cancellationToken);

        var analysis = new TicketAnalysis
        {
            Id = Guid.NewGuid(),
            OriginalText = request.Text.Trim(),
            Summary = output.Summary,
            Severity = TicketMapper.ParseSeverity(output.Severity),
            Category = output.Category,
            ImpactedModule = output.ImpactedModule,
            SuggestedAction = output.SuggestedAction,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await ticketAnalysisRepository.AddAsync(analysis, cancellationToken);
        await ticketAnalysisRepository.SaveChangesAsync(cancellationToken);

        return TicketMapper.ToAnalyzeResponse(analysis);
    }
}

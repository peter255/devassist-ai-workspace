using DevAssist.Application.Interfaces;
using DevAssist.Contracts.Copilot;
using FluentValidation;
using MediatR;

namespace DevAssist.Application.Copilot.Commands.AskCopilotQuestion;

public sealed class AskCopilotQuestionCommandValidator : AbstractValidator<AskCopilotQuestionCommand>
{
    public AskCopilotQuestionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Question).NotEmpty().MaximumLength(4000);
    }
}

public sealed class AskCopilotQuestionCommandHandler(IKnowledgeCopilotService copilotService)
    : IRequestHandler<AskCopilotQuestionCommand, AskCopilotResponse>
{
    public Task<AskCopilotResponse> Handle(AskCopilotQuestionCommand request, CancellationToken cancellationToken) =>
        copilotService.AskAsync(request.SessionId, request.Question, cancellationToken);
}

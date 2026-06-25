using DevAssist.Contracts.Copilot;
using MediatR;

namespace DevAssist.Application.Copilot.Commands.AskCopilotQuestion;

public sealed record AskCopilotQuestionCommand(Guid SessionId, string Question) : IRequest<AskCopilotResponse>;

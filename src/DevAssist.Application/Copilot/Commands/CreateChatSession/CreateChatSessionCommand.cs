using DevAssist.Contracts.Copilot;
using MediatR;

namespace DevAssist.Application.Copilot.Commands.CreateChatSession;

public sealed record CreateChatSessionCommand(string? Title, string CreatedBy) : IRequest<CreateChatSessionResponse>;

using DevAssist.Application.Interfaces.Copilot;
using DevAssist.Contracts.Copilot;
using DevAssist.Domain.Entities;
using FluentValidation;
using MediatR;

namespace DevAssist.Application.Copilot.Commands.CreateChatSession;

public sealed class CreateChatSessionCommandValidator : AbstractValidator<CreateChatSessionCommand>
{
    public CreateChatSessionCommandValidator()
    {
        RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title is not null);
    }
}

public sealed class CreateChatSessionCommandHandler(IChatRepository chatRepository)
    : IRequestHandler<CreateChatSessionCommand, CreateChatSessionResponse>
{
    public async Task<CreateChatSessionResponse> Handle(CreateChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(request.Title)
                ? $"Session {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}"
                : request.Title.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = request.CreatedBy
        };

        await chatRepository.CreateSessionAsync(session, cancellationToken);
        await chatRepository.SaveChangesAsync(cancellationToken);

        return new CreateChatSessionResponse(session.Id, session.Title, session.CreatedAt);
    }
}

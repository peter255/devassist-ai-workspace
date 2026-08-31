using DevAssist.Application.Interfaces.Auth;
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
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Authentication required.");
    }
}

public sealed class CreateChatSessionCommandHandler(
    IChatRepository chatRepository,
    IUserRepository userRepository)
    : IRequestHandler<CreateChatSessionCommand, CreateChatSessionResponse>
{
    public async Task<CreateChatSessionResponse> Handle(CreateChatSessionCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId is not Guid userId)
            throw new UnauthorizedAccessException("Authentication required.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            throw new UnauthorizedAccessException("Your session expired. Please sign in again.");

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(request.Title)
                ? "New chat"
                : request.Title.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = user.Username,
            UserId = user.Id
        };

        await chatRepository.CreateSessionAsync(session, cancellationToken);
        await chatRepository.SaveChangesAsync(cancellationToken);

        return new CreateChatSessionResponse(session.Id, session.Title, session.CreatedAt);
    }
}

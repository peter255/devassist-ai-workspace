using DevAssist.Application.Copilot.Commands.AskCopilotQuestion;
using DevAssist.Application.Copilot.Commands.CreateChatSession;
using DevAssist.Application.Copilot.Queries.GetSessionMessages;
using DevAssist.Application.Copilot.Queries.GetUserSessions;
using DevAssist.Application.Interfaces;
using DevAssist.Application.Interfaces.Auth;
using DevAssist.Contracts.Common;
using DevAssist.Contracts.Copilot;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevAssist.Api.Controllers;

[ApiController]
[Route("api/copilot")]
[EnableRateLimiting("ai")]
public sealed class CopilotController(
    IMediator mediator,
    IKnowledgeCopilotService copilotService,
    ICurrentUserService currentUser,
    IValidator<CreateChatSessionCommand> createSessionValidator,
    IValidator<AskCopilotQuestionCommand> askValidator) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ChatSessionSummaryDto>>>> ListSessions(
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return Unauthorized(ApiResponse<IReadOnlyList<ChatSessionSummaryDto>>.Fail("Authentication required."));

        var result = await mediator.Send(new GetUserSessionsQuery(userId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ChatSessionSummaryDto>>.Ok(result));
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<ApiResponse<CreateChatSessionResponse>>> CreateSession(
        [FromBody] CreateChatSessionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateChatSessionCommand(request.Title, request.CreatedBy, currentUser.UserId);
        var validation = await createSessionValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<CreateChatSessionResponse>.Fail(
                string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));
        }

        var result = await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<CreateChatSessionResponse>.Ok(result));
    }

    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ChatMessageDto>>>> GetMessages(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(
                new GetSessionMessagesQuery(sessionId, currentUser.UserId),
                cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<ChatMessageDto>>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<IReadOnlyList<ChatMessageDto>>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<ChatMessageDto>>.Fail(ex.Message));
        }
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ApiResponse<AskCopilotResponse>>> Ask(
        [FromBody] AskCopilotRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AskCopilotQuestionCommand(request.SessionId, request.Question, currentUser.UserId);
        var validation = await askValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AskCopilotResponse>.Fail(
                string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));
        }

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<AskCopilotResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AskCopilotResponse>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<AskCopilotResponse>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Streams the copilot answer token-by-token as Server-Sent Events.
    ///
    /// Event format:
    ///   data: {"token":"..."}\n\n   — one per LLM token
    ///   data: {"citations":[...]}\n\n — emitted after the last token
    ///   data: [DONE]\n\n
    /// </summary>
    [HttpPost("ask-stream")]
    public async Task AskStream(
        [FromBody] AskCopilotRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var eventData in copilotService.AskStreamAsync(
                request.SessionId, request.Question, currentUser.UserId, cancellationToken))
            {
                await Response.WriteAsync($"data: {eventData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (KeyNotFoundException ex)
        {
            await Response.WriteAsync($"data: {{\"error\":\"{ex.Message}\"}}\n\n", cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Response.WriteAsync($"data: {{\"error\":\"{ex.Message}\"}}\n\n", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await Response.WriteAsync("data: {\"error\":\"An error occurred while streaming the response.\"}\n\n", cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}

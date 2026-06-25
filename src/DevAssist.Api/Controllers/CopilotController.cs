using DevAssist.Application.Copilot.Commands.AskCopilotQuestion;
using DevAssist.Application.Copilot.Commands.CreateChatSession;
using DevAssist.Contracts.Common;
using DevAssist.Contracts.Copilot;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DevAssist.Api.Controllers;

[ApiController]
[Route("api/copilot")]
public sealed class CopilotController(
    IMediator mediator,
    IValidator<CreateChatSessionCommand> createSessionValidator,
    IValidator<AskCopilotQuestionCommand> askValidator) : ControllerBase
{
    [HttpPost("sessions")]
    public async Task<ActionResult<ApiResponse<CreateChatSessionResponse>>> CreateSession(
        [FromBody] CreateChatSessionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateChatSessionCommand(request.Title, request.CreatedBy);
        var validation = await createSessionValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<CreateChatSessionResponse>.Fail(
                string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));
        }

        var result = await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<CreateChatSessionResponse>.Ok(result));
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ApiResponse<AskCopilotResponse>>> Ask(
        [FromBody] AskCopilotRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AskCopilotQuestionCommand(request.SessionId, request.Question);
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
    }
}

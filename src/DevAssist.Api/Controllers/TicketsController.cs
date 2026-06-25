using DevAssist.Application.Tickets.Commands.AnalyzeTicket;
using DevAssist.Application.Tickets.Queries.GetTicketAnalyses;
using DevAssist.Contracts.Common;
using DevAssist.Contracts.Tickets;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DevAssist.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(
    IMediator mediator,
    IValidator<AnalyzeTicketCommand> analyzeValidator) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<ActionResult<ApiResponse<AnalyzeTicketResponse>>> Analyze(
        [FromBody] AnalyzeTicketRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AnalyzeTicketCommand(request.Text);
        var validation = await analyzeValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AnalyzeTicketResponse>.Fail(
                string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));
        }

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<AnalyzeTicketResponse>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AnalyzeTicketResponse>.Fail(ex.Message));
        }
    }

    [HttpGet("analyses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketAnalysisListItemDto>>>> List(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetTicketAnalysesQuery(limit), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TicketAnalysisListItemDto>>.Ok(result));
    }
}

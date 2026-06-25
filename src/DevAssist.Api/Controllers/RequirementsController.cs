using DevAssist.Application.Requirements.Commands.BreakdownRequirement;
using DevAssist.Application.Requirements.Queries.GetRequirementAnalyses;
using DevAssist.Application.Requirements.Queries.GetRequirementAnalysisById;
using DevAssist.Contracts.Common;
using DevAssist.Contracts.Requirements;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DevAssist.Api.Controllers;

[ApiController]
[Route("api/requirements")]
public sealed class RequirementsController(
    IMediator mediator,
    IValidator<BreakdownRequirementCommand> breakdownValidator) : ControllerBase
{
    [HttpPost("breakdown")]
    public async Task<ActionResult<ApiResponse<BreakdownRequirementResponse>>> Breakdown(
        [FromBody] BreakdownRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BreakdownRequirementCommand(request.Text);
        var validation = await breakdownValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<BreakdownRequirementResponse>.Fail(
                string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));
        }

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<BreakdownRequirementResponse>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BreakdownRequirementResponse>.Fail(ex.Message));
        }
    }

    [HttpGet("analyses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RequirementAnalysisListItemDto>>>> List(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetRequirementAnalysesQuery(limit), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RequirementAnalysisListItemDto>>.Ok(result));
    }

    [HttpGet("analyses/{id:guid}")]
    public async Task<ActionResult<ApiResponse<BreakdownRequirementResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRequirementAnalysisByIdQuery(id), cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<BreakdownRequirementResponse>.Fail("Requirement analysis not found."));
        }

        return Ok(ApiResponse<BreakdownRequirementResponse>.Ok(result));
    }
}

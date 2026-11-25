using Microsoft.AspNetCore.Mvc;
using SettleTrack.Api.Dtos;
using SettleTrack.Api.Models;
using SettleTrack.Api.Services;

namespace SettleTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController(ITradeService trades, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TradeDto>>> List(
        [FromQuery] TradeStatus? status,
        [FromQuery] string? isin,
        CancellationToken ct)
    {
        var result = await trades.ListAsync(status, isin, ct);
        return Ok(result.Select(t => t.ToDto()));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<TradeSummaryDto>> Summary(CancellationToken ct) =>
        Ok(await trades.GetSummaryAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TradeDto>> GetById(int id, CancellationToken ct)
    {
        var trade = await trades.GetAsync(id, ct);
        return trade is null ? NotFound() : Ok(trade.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<TradeDto>> Create(CreateTradeRequest request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (request.SettlementDate < today)
        {
            ModelState.AddModelError(nameof(request.SettlementDate), "Settlement date cannot be in the past.");
            return ValidationProblem(ModelState);
        }

        var trade = await trades.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = trade.Id }, trade.ToDto());
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TradeDto>> UpdateStatus(
        int id,
        UpdateTradeStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var trade = await trades.ChangeStatusAsync(id, request.Status, ct);
            return trade is null ? NotFound() : Ok(trade.ToDto());
        }
        catch (InvalidTradeTransitionException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Invalid status transition",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
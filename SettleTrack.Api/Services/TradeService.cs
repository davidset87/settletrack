using Microsoft.EntityFrameworkCore;
using SettleTrack.Api.Data;
using SettleTrack.Api.Dtos;
using SettleTrack.Api.Models;

namespace SettleTrack.Api.Services;

public interface ITradeService
{
    Task<IReadOnlyList<Trade>> ListAsync(TradeStatus? status, string? isin, CancellationToken ct);
    Task<Trade?> GetAsync(int id, CancellationToken ct);
    Task<Trade> CreateAsync(CreateTradeRequest request, CancellationToken ct);
    Task<Trade?> ChangeStatusAsync(int id, TradeStatus newStatus, CancellationToken ct);
    Task<TradeSummaryDto> GetSummaryAsync(CancellationToken ct);
}

public class TradeService(SettleTrackDbContext db, TimeProvider clock) : ITradeService
{
    public async Task<IReadOnlyList<Trade>> ListAsync(TradeStatus? status, string? isin, CancellationToken ct)
    {
        var query = db.Trades.AsNoTracking();

        if (status is { } wanted)
        {
            query = query.Where(t => t.Status == wanted);
        }

        if (!string.IsNullOrWhiteSpace(isin))
        {
            var prefix = isin.Trim().ToUpperInvariant();
            query = query.Where(t => t.Isin.StartsWith(prefix));
        }

        return await query
            .OrderBy(t => t.SettlementDate)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);
    }

    public Task<Trade?> GetAsync(int id, CancellationToken ct) =>
        db.Trades.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Trade> CreateAsync(CreateTradeRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        var trade = new Trade
        {
            Isin = request.Isin,
            Counterparty = request.Counterparty.Trim(),
            Side = request.Side,
            Quantity = request.Quantity,
            Price = request.Price,
            Currency = request.Currency,
            SettlementDate = request.SettlementDate,
            Status = TradeStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Trades.Add(trade);
        await db.SaveChangesAsync(ct);
        return trade;
    }

    public async Task<Trade?> ChangeStatusAsync(int id, TradeStatus newStatus, CancellationToken ct)
    {
        var trade = await db.Trades.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (trade is null)
        {
            return null;
        }

        trade.TransitionTo(newStatus, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return trade;
    }

    public async Task<TradeSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var counts = await db.Trades
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

        int CountOf(TradeStatus status) => counts.GetValueOrDefault(status);

        var settled = CountOf(TradeStatus.Settled);
        var failed = CountOf(TradeStatus.Failed);
        var finished = settled + failed;
        var rate = finished == 0 ? 0 : Math.Round(100.0 * settled / finished, 1);

        var dueToday = await db.Trades.CountAsync(t =>
            t.SettlementDate == today &&
            (t.Status == TradeStatus.Pending || t.Status == TradeStatus.Matched), ct);

        var overdue = await db.Trades.CountAsync(t =>
            t.SettlementDate < today &&
            (t.Status == TradeStatus.Pending || t.Status == TradeStatus.Matched), ct);

        return new TradeSummaryDto(
            counts.Values.Sum(),
            CountOf(TradeStatus.Pending),
            CountOf(TradeStatus.Matched),
            settled,
            failed,
            rate,
            dueToday,
            overdue);
    }
}
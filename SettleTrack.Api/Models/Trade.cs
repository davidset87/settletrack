namespace SettleTrack.Api.Models;

public class InvalidTradeTransitionException(TradeStatus from, TradeStatus to)
    : Exception($"A trade cannot move from {from} to {to}.");

public class Trade
{
    public int Id { get; set; }
    public string Isin { get; set; } = string.Empty;
    public string Counterparty { get; set; } = string.Empty;
    public TradeSide Side { get; set; }
    public long Quantity { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateOnly SettlementDate { get; set; }
    public TradeStatus Status { get; set; } = TradeStatus.Pending;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public static IReadOnlyList<TradeStatus> GetAllowedTransitions(TradeStatus from) => from switch
    {
        TradeStatus.Pending => new[] { TradeStatus.Matched, TradeStatus.Failed },
        TradeStatus.Matched => new[] { TradeStatus.Settled, TradeStatus.Failed },
        _ => Array.Empty<TradeStatus>()
    };

    public static bool CanTransition(TradeStatus from, TradeStatus to) =>
        GetAllowedTransitions(from).Contains(to);

    public void TransitionTo(TradeStatus next, DateTimeOffset nowUtc)
    {
        if (!CanTransition(Status, next))
        {
            throw new InvalidTradeTransitionException(Status, next);
        }

        Status = next;
        UpdatedAtUtc = nowUtc;
    }
}
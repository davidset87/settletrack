namespace SettleTrack.Api.Models;

public enum TradeSide
{
    Buy,
    Sell
}

public enum TradeStatus
{
    Pending,
    Matched,
    Settled,
    Failed
}
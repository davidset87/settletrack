using Microsoft.EntityFrameworkCore;
using SettleTrack.Api.Models;

namespace SettleTrack.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(SettleTrackDbContext db, TimeProvider clock)
    {
        if (await db.Trades.AnyAsync())
        {
            return;
        }

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        Trade Make(string isin, string counterparty, TradeSide side, long quantity, decimal price,
            string currency, int dayOffset, TradeStatus status) => new()
        {
            Isin = isin,
            Counterparty = counterparty,
            Side = side,
            Quantity = quantity,
            Price = price,
            Currency = currency,
            SettlementDate = today.AddDays(dayOffset),
            Status = status,
            CreatedAtUtc = now.AddDays(-3),
            UpdatedAtUtc = now
        };

        db.Trades.AddRange(
            Make("US0378331005", "Northbridge Capital", TradeSide.Buy, 1500, 189.40m, "USD", -2, TradeStatus.Settled),
            Make("US5949181045", "Alder Securities", TradeSide.Sell, 800, 412.15m, "USD", -1, TradeStatus.Settled),
            Make("DE0007164600", "Meridian Prime", TradeSide.Buy, 2500, 148.20m, "EUR", -1, TradeStatus.Failed),
            Make("US0378331005", "Kestrel Markets", TradeSide.Sell, 900, 190.05m, "USD", -1, TradeStatus.Pending),
            Make("FR0000120271", "Harbor & Finch", TradeSide.Buy, 4000, 58.75m, "EUR", 0, TradeStatus.Matched),
            Make("NL0010273215", "Lumen Clearing", TradeSide.Sell, 300, 702.30m, "EUR", 0, TradeStatus.Pending),
            Make("CH0012005267", "Northbridge Capital", TradeSide.Buy, 1200, 96.80m, "CHF", 1, TradeStatus.Pending),
            Make("GB0002634946", "Alder Securities", TradeSide.Sell, 5000, 12.45m, "GBP", 1, TradeStatus.Matched),
            Make("PLPKO0000016", "Meridian Prime", TradeSide.Buy, 10000, 61.90m, "PLN", 2, TradeStatus.Pending),
            Make("IE00BZ12WP82", "Kestrel Markets", TradeSide.Buy, 650, 455.10m, "EUR", 2, TradeStatus.Pending),
            Make("ES0113900J37", "Harbor & Finch", TradeSide.Sell, 7500, 9.12m, "EUR", 3, TradeStatus.Pending),
            Make("DE0007164600", "Lumen Clearing", TradeSide.Buy, 1800, 149.00m, "EUR", 3, TradeStatus.Matched));

        await db.SaveChangesAsync();
    }
}
using System.ComponentModel.DataAnnotations;
using SettleTrack.Api.Models;

namespace SettleTrack.Api.Dtos;

public record CreateTradeRequest(
    [property: Required, RegularExpression("^[A-Z]{2}[A-Z0-9]{9}[0-9]$",
        ErrorMessage = "ISIN must be 12 characters (for example US0378331005).")]
    string Isin,
    [property: Required, StringLength(100, MinimumLength = 2)]
    string Counterparty,
    TradeSide Side,
    [property: Range(1, 1_000_000_000)]
    long Quantity,
    [property: Range(0.0001, 1_000_000)]
    decimal Price,
    [property: Required, RegularExpression("^[A-Z]{3}$",
        ErrorMessage = "Currency must be a 3-letter code (for example EUR).")]
    string Currency,
    DateOnly SettlementDate);

public record UpdateTradeStatusRequest(TradeStatus Status);

public record TradeDto(
    int Id,
    string Isin,
    string Counterparty,
    TradeSide Side,
    long Quantity,
    decimal Price,
    string Currency,
    DateOnly SettlementDate,
    TradeStatus Status,
    IReadOnlyList<TradeStatus> AllowedTransitions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public record TradeSummaryDto(
    int Total,
    int Pending,
    int Matched,
    int Settled,
    int Failed,
    double SettlementRatePercent,
    int DueToday,
    int Overdue);

public static class TradeMappings
{
    public static TradeDto ToDto(this Trade t) => new(
        t.Id,
        t.Isin,
        t.Counterparty,
        t.Side,
        t.Quantity,
        t.Price,
        t.Currency,
        t.SettlementDate,
        t.Status,
        Trade.GetAllowedTransitions(t.Status),
        t.CreatedAtUtc,
        t.UpdatedAtUtc);
}
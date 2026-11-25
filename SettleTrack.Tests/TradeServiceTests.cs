using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SettleTrack.Api.Data;
using SettleTrack.Api.Dtos;
using SettleTrack.Api.Models;
using SettleTrack.Api.Services;
using Xunit;

namespace SettleTrack.Tests;

public sealed class TradeServiceTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    private readonly SqliteConnection _connection;
    private readonly SettleTrackDbContext _db;
    private readonly FakeTimeProvider _clock =
        new(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
    private readonly TradeService _service;

    public TradeServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SettleTrackDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SettleTrackDbContext(options);
        _db.Database.EnsureCreated();
        _service = new TradeService(_db, _clock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Trade> AddTradeAsync(string isin, TradeStatus status, DateOnly settlementDate)
    {
        var trade = new Trade
        {
            Isin = isin,
            Counterparty = "Test Counterparty",
            Side = TradeSide.Buy,
            Quantity = 100,
            Price = 10m,
            Currency = "EUR",
            SettlementDate = settlementDate,
            Status = status,
            CreatedAtUtc = _clock.GetUtcNow(),
            UpdatedAtUtc = _clock.GetUtcNow()
        };

        _db.Trades.Add(trade);
        await _db.SaveChangesAsync();
        return trade;
    }

    [Fact]
    public async Task CreateAsync_StartsAsPending_AndSetsTimestamps()
    {
        var request = new CreateTradeRequest(
            "US0378331005", "  Test Bank  ", TradeSide.Buy, 100, 10.5m, "USD", Today.AddDays(1));

        var trade = await _service.CreateAsync(request, CancellationToken.None);

        Assert.True(trade.Id > 0);
        Assert.Equal(TradeStatus.Pending, trade.Status);
        Assert.Equal("Test Bank", trade.Counterparty);
        Assert.Equal(_clock.GetUtcNow(), trade.CreatedAtUtc);
        Assert.Equal(_clock.GetUtcNow(), trade.UpdatedAtUtc);
    }

    [Fact]
    public async Task ChangeStatusAsync_MovesPendingToMatched()
    {
        var trade = await AddTradeAsync("US0378331005", TradeStatus.Pending, Today);
        _clock.Advance(TimeSpan.FromHours(1));

        var updated = await _service.ChangeStatusAsync(trade.Id, TradeStatus.Matched, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(TradeStatus.Matched, updated.Status);
        Assert.Equal(_clock.GetUtcNow(), updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task ChangeStatusAsync_ReturnsNull_WhenTradeDoesNotExist()
    {
        var result = await _service.ChangeStatusAsync(999, TradeStatus.Matched, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ChangeStatusAsync_Throws_WhenTransitionIsInvalid()
    {
        var trade = await AddTradeAsync("US0378331005", TradeStatus.Settled, Today);

        await Assert.ThrowsAsync<InvalidTradeTransitionException>(() =>
            _service.ChangeStatusAsync(trade.Id, TradeStatus.Pending, CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_FiltersByStatus()
    {
        await AddTradeAsync("US0378331005", TradeStatus.Pending, Today);
        await AddTradeAsync("DE0007164600", TradeStatus.Matched, Today);
        await AddTradeAsync("FR0000120271", TradeStatus.Pending, Today);

        var result = await _service.ListAsync(TradeStatus.Pending, null, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(TradeStatus.Pending, t.Status));
    }

    [Fact]
    public async Task ListAsync_FiltersByIsinPrefix_IgnoringCase()
    {
        await AddTradeAsync("US0378331005", TradeStatus.Pending, Today);
        await AddTradeAsync("US5949181045", TradeStatus.Pending, Today);
        await AddTradeAsync("DE0007164600", TradeStatus.Pending, Today);

        var result = await _service.ListAsync(null, "us", CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.StartsWith("US", t.Isin));
    }

    [Fact]
    public async Task GetSummaryAsync_CountsStatusesDueTodayAndOverdue()
    {
        await AddTradeAsync("US0378331005", TradeStatus.Pending, Today);               // due today
        await AddTradeAsync("DE0007164600", TradeStatus.Matched, Today.AddDays(-1));   // overdue
        await AddTradeAsync("FR0000120271", TradeStatus.Settled, Today.AddDays(-2));
        await AddTradeAsync("NL0010273215", TradeStatus.Settled, Today.AddDays(-1));
        await AddTradeAsync("CH0012005267", TradeStatus.Failed, Today.AddDays(-1));
        await AddTradeAsync("GB0002634946", TradeStatus.Pending, Today.AddDays(1));

        var summary = await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(6, summary.Total);
        Assert.Equal(2, summary.Pending);
        Assert.Equal(1, summary.Matched);
        Assert.Equal(2, summary.Settled);
        Assert.Equal(1, summary.Failed);
        Assert.Equal(66.7, summary.SettlementRatePercent);
        Assert.Equal(1, summary.DueToday);
        Assert.Equal(1, summary.Overdue);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsZeroRate_WhenNothingIsFinished()
    {
        await AddTradeAsync("US0378331005", TradeStatus.Pending, Today);

        var summary = await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(0, summary.SettlementRatePercent);
    }
}
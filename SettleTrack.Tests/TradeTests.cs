using SettleTrack.Api.Models;
using Xunit;

namespace SettleTrack.Tests;

public class TradeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TradeStatus.Pending, TradeStatus.Matched)]
    [InlineData(TradeStatus.Pending, TradeStatus.Failed)]
    [InlineData(TradeStatus.Matched, TradeStatus.Settled)]
    [InlineData(TradeStatus.Matched, TradeStatus.Failed)]
    public void CanTransition_AllowsValidMoves(TradeStatus from, TradeStatus to)
    {
        Assert.True(Trade.CanTransition(from, to));
    }

    [Theory]
    [InlineData(TradeStatus.Pending, TradeStatus.Settled)]
    [InlineData(TradeStatus.Pending, TradeStatus.Pending)]
    [InlineData(TradeStatus.Matched, TradeStatus.Pending)]
    [InlineData(TradeStatus.Settled, TradeStatus.Failed)]
    [InlineData(TradeStatus.Settled, TradeStatus.Pending)]
    [InlineData(TradeStatus.Failed, TradeStatus.Matched)]
    [InlineData(TradeStatus.Failed, TradeStatus.Settled)]
    public void CanTransition_RejectsInvalidMoves(TradeStatus from, TradeStatus to)
    {
        Assert.False(Trade.CanTransition(from, to));
    }

    [Theory]
    [InlineData(TradeStatus.Settled)]
    [InlineData(TradeStatus.Failed)]
    public void GetAllowedTransitions_IsEmpty_ForFinalStatuses(TradeStatus status)
    {
        Assert.Empty(Trade.GetAllowedTransitions(status));
    }

    [Fact]
    public void TransitionTo_UpdatesStatusAndTimestamp()
    {
        var trade = new Trade { Status = TradeStatus.Pending, UpdatedAtUtc = Now.AddDays(-1) };

        trade.TransitionTo(TradeStatus.Matched, Now);

        Assert.Equal(TradeStatus.Matched, trade.Status);
        Assert.Equal(Now, trade.UpdatedAtUtc);
    }

    [Fact]
    public void TransitionTo_Throws_AndKeepsStatus_WhenMoveIsInvalid()
    {
        var originalTimestamp = Now.AddDays(-1);
        var trade = new Trade { Status = TradeStatus.Settled, UpdatedAtUtc = originalTimestamp };

        Assert.Throws<InvalidTradeTransitionException>(() => trade.TransitionTo(TradeStatus.Pending, Now));

        Assert.Equal(TradeStatus.Settled, trade.Status);
        Assert.Equal(originalTimestamp, trade.UpdatedAtUtc);
    }
}
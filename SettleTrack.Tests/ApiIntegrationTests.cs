using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using SettleTrack.Api;
using SettleTrack.Api.Dtos;
using SettleTrack.Api.Models;

namespace SettleTrack.Tests;

public sealed class ApiFactory : WebApplicationFactory<ApiMarker>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"settletrack-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup of the temporary database file.
        }
    }
}

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client;

    public ApiIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CreateTradeRequest ValidRequest() => new(
        "US88160R1014",
        "Test Capital",
        TradeSide.Buy,
        100,
        10m,
        "EUR",
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30));

    private async Task<TradeDto> CreateTradeAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/trades", ValidRequest(), Json);
        response.EnsureSuccessStatusCode();
        var trade = await response.Content.ReadFromJsonAsync<TradeDto>(Json);
        return trade!;
    }

    [Fact]
    public async Task GetTrades_ReturnsSeededTrades()
    {
        var trades = await _client.GetFromJsonAsync<List<TradeDto>>("/api/trades", Json);

        Assert.NotNull(trades);
        Assert.NotEmpty(trades);
    }

    [Fact]
    public async Task GetSummary_CountsAddUpToTotal()
    {
        var summary = await _client.GetFromJsonAsync<TradeSummaryDto>("/api/trades/summary", Json);

        Assert.NotNull(summary);
        Assert.Equal(summary.Total, summary.Pending + summary.Matched + summary.Settled + summary.Failed);
    }

    [Fact]
    public async Task CreateTrade_WithValidRequest_CreatesPendingTrade()
    {
        var trade = await CreateTradeAsync();

        Assert.True(trade.Id > 0);
        Assert.Equal(TradeStatus.Pending, trade.Status);
        Assert.Contains(TradeStatus.Matched, trade.AllowedTransitions);
    }

    [Fact]
    public async Task CreateTrade_WithInvalidIsin_ReturnsBadRequest()
    {
        var request = ValidRequest() with { Isin = "ABC" };

        var response = await _client.PostAsJsonAsync("/api/trades", request, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrade_WithPastSettlementDate_ReturnsBadRequest()
    {
        var request = ValidRequest() with
        {
            SettlementDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2),
        };

        var response = await _client.PostAsJsonAsync("/api/trades", request, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WithValidTransition_UpdatesTrade()
    {
        var trade = await CreateTradeAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/trades/{trade.Id}/status",
            new UpdateTradeStatusRequest(TradeStatus.Matched),
            Json);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<TradeDto>(Json);
        Assert.Equal(TradeStatus.Matched, updated!.Status);
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidTransition_ReturnsConflict()
    {
        var trade = await CreateTradeAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/trades/{trade.Id}/status",
            new UpdateTradeStatusRequest(TradeStatus.Settled),
            Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetTrade_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/trades/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.AI;
using TradingPlatform.Application.Interfaces.Approvals;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ai")]
public class AiAdvisorController(
    IAiMarketAdvisorService aiAdvisorService,
    ITradeApprovalService tradeApprovalService,
    ITradingDbContext dbContext,
    ICurrentUserService currentUserService,
    IMarketDataService marketDataService,
    ILogger<AiAdvisorController> logger) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AiAnalysisResult>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAiMarketOverview(CancellationToken ct)
    {
        var activeAccount = await dbContext.TradingAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IsActive, ct);

        var results = await aiAdvisorService.ScanHoldingsAndWatchlistAsync(activeAccount?.Id, ct);
        return Ok(ApiResponse<IReadOnlyList<AiAnalysisResult>>.Ok(results, "Market AI scan completed"));
    }

    [HttpGet("special-monitor")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialMonitorList(CancellationToken ct)
    {
        var universe = await dbContext.InstrumentUniverses
            .AsNoTracking()
            .Include(u => u.Items)
                .ThenInclude(i => i.Instrument)
            .FirstOrDefaultAsync(u => u.Name == "Special Monitor", ct);

        var symbols = universe?.Items.Select(i => i.Instrument.TradingSymbol).ToList() 
            ?? new List<string> { "RELIANCE", "TCS", "INFY", "HDFCBANK", "ICICIBANK" };

        return Ok(ApiResponse<IReadOnlyList<string>>.Ok(symbols));
    }

    [HttpPost("special-monitor/add")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddToSpecialMonitor([FromBody] AddWatchlistRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            return BadRequest(ApiResponse<bool>.Fail("Symbol is required"));
        }

        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var userId = currentUserService.UserId ?? Guid.Empty;

        var universe = await dbContext.InstrumentUniverses
            .Include(u => u.Items)
            .FirstOrDefaultAsync(u => u.Name == "Special Monitor", ct);

        if (universe == null)
        {
            universe = new InstrumentUniverse
            {
                UserId = userId,
                Name = "Special Monitor",
                Description = "Custom user priority watchlist for AI technical analysis",
                IsActive = true
            };
            dbContext.InstrumentUniverses.Add(universe);
        }

        var instrument = await dbContext.Instruments
            .FirstOrDefaultAsync(i => i.TradingSymbol == symbol, ct);

        if (instrument == null)
        {
            // Auto-provision instrument if not already present
            instrument = new Instrument
            {
                TradingSymbol = symbol,
                CompanyName = $"{symbol} Equities Ltd",
                Exchange = "NSE",
                InstrumentToken = $"{Math.Abs(symbol.GetHashCode()) % 900000 + 100000}",
                LotSize = 1,
                TickSize = 0.05m,
                Segment = "NSE_EQ",
                IsActive = true
            };
            dbContext.Instruments.Add(instrument);
            await dbContext.SaveChangesAsync(ct);
        }

        if (!universe.Items.Any(i => i.InstrumentId == instrument.Id))
        {
            universe.Items.Add(new InstrumentUniverseItem
            {
                InstrumentUniverseId = universe.Id,
                InstrumentId = instrument.Id
            });
            await dbContext.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<bool>.Ok(true, $"Added {symbol} to Special Monitor"));
    }

    [HttpDelete("special-monitor/{symbol}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveFromSpecialMonitor(string symbol, CancellationToken ct)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var universe = await dbContext.InstrumentUniverses
            .Include(u => u.Items)
                .ThenInclude(i => i.Instrument)
            .FirstOrDefaultAsync(u => u.Name == "Special Monitor", ct);

        if (universe != null)
        {
            var item = universe.Items.FirstOrDefault(i => i.Instrument.TradingSymbol.Equals(sym, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                universe.Items.Remove(item);
                await dbContext.SaveChangesAsync(ct);
            }
        }

        return Ok(ApiResponse<bool>.Ok(true, $"Removed {sym} from Special Monitor"));
    }

    [HttpPost("simulate-buy-signal")]
    [ProducesResponseType(typeof(ApiResponse<PendingTradeApprovalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SimulateBuySignal([FromBody] SimulateBuyRequest? request, CancellationToken ct)
    {
        var symbol = request?.Symbol?.Trim().ToUpperInvariant() ?? "INFY";
        var quantity = request?.Quantity > 0 ? request.Quantity : 10;

        var account = await dbContext.TradingAccounts
            .Include(a => a.Configuration)
            .FirstOrDefaultAsync(a => a.IsActive, ct);

        if (account == null)
        {
            return BadRequest(ApiResponse<PendingTradeApprovalDto>.Fail("No active trading account found."));
        }

        var inst = await dbContext.Instruments.FirstOrDefaultAsync(i => i.TradingSymbol == symbol, ct)
            ?? await dbContext.Instruments.FirstAsync(ct);

        var quote = await marketDataService.GetQuoteAsync(symbol, "NSE", ct);
        decimal price = quote.LastPrice > 0 ? quote.LastPrice : 1850.50m;
        decimal stopLoss = Math.Round(price * 0.98m, 2);
        decimal target = Math.Round(price * 1.04m, 2);

        var approvalRequest = new CreateTradeApprovalRequest(
            TradingAccountId: account.Id,
            InstrumentId: inst.Id,
            TradingSignalId: null,
            Symbol: symbol,
            CompanyName: inst.CompanyName,
            Side: "Buy",
            Quantity: quantity,
            IndicativePrice: price,
            StopLoss: stopLoss,
            Target: target,
            RiskAmount: Math.Round((price - stopLoss) * quantity, 2),
            Reason: $"AI Gemini Pro High Conviction BUY setup: Bullish bounce at 50 EMA with oversold RSI recovery.",
            ApprovalDurationSeconds: 2
        );

        var approval = await tradeApprovalService.CreateApprovalAsync(approvalRequest, ct);

        logger.LogInformation("Simulation: Triggered 2-second BUY approval window for {Quantity} {Symbol} @ ₹{Price}",
            quantity, symbol, price);

        return Ok(ApiResponse<PendingTradeApprovalDto>.Ok(approval, "2-Second BUY approval window triggered. Observe live countdown toast in UI."));
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<AiAdvisorConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var config = await aiAdvisorService.GetConfigurationAsync(ct);
        return Ok(ApiResponse<AiAdvisorConfigDto>.Ok(config, "AI Advisor configuration retrieved"));
    }

    [HttpPost("config")]
    [ProducesResponseType(typeof(ApiResponse<AiAdvisorConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConfig([FromBody] AiAdvisorConfigDto request, CancellationToken ct)
    {
        var updated = await aiAdvisorService.UpdateConfigurationAsync(request, ct);
        return Ok(ApiResponse<AiAdvisorConfigDto>.Ok(updated, "AI Advisor configuration updated successfully"));
    }

    [HttpPost("config/reset")]
    [ProducesResponseType(typeof(ApiResponse<AiAdvisorConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetConfig(CancellationToken ct)
    {
        var reset = await aiAdvisorService.ResetConfigurationAsync(ct);
        return Ok(ApiResponse<AiAdvisorConfigDto>.Ok(reset, "AI Advisor configuration restored to appsettings.json defaults"));
    }
}

public record AddWatchlistRequest(string Symbol);
public record SimulateBuyRequest(string? Symbol, int Quantity = 10);

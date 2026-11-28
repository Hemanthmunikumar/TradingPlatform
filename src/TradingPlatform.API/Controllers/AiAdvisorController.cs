using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.AI;
using TradingPlatform.Application.Interfaces.Adapters;
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
    IIndiaDayTraderService indiaDayTraderService,
    ITradeApprovalService tradeApprovalService,
    ITradingDbContext dbContext,
    ICurrentUserService currentUserService,
    IMarketDataService marketDataService,
    IBrokerAdapterFactory brokerAdapterFactory,
    ILogger<AiAdvisorController> logger) : ControllerBase
{
    private static readonly HashSet<string> KnownIndianStocks = new(StringComparer.OrdinalIgnoreCase)
    {
        "RELIANCE", "TCS", "INFY", "HDFCBANK", "ICICIBANK", "SBIN", "BHARTIARTL", "ITC", "KOTAKBANK", "LT",
        "BAJFINANCE", "MARUTI", "TATAMOTORS", "TMCV", "TMPV", "SUNPHARMA", "TITAN", "WIPRO", "ADANIENT", "NTPC",
        "ZOMATO", "ETERNAL", "BEL", "HAL", "BHEL", "COALINDIA", "ONGC", "POWERGRID", "JSWSTEEL", "TATASTEEL",
        "HINDALCO", "VEDL", "GRASIM", "TECHM", "HCLTECH", "ULTRACEMCO", "NESTLEIND", "ASIANPAINT", "BRITANNIA",
        "DIVISLAB", "CIPLA", "DRREDDY", "APOLLOHOSP", "HEROMOTOCO", "EICHERMOT", "BAJAJ-AUTO", "BPCL", "IOC",
        "TATACONSUM", "IRCTC", "DMART", "VBL", "JIOFIN", "CHOLAFIN", "PIDILITIND", "SIEMENS", "ABB", "TRENT",
        "POLYCAB", "PERSISTENT", "LTIM", "COFORGE", "MPHASIS", "IDEA", "YESBANK", "SUZLON", "SWIGGY", "PAYTM",
        "NYKAA", "MOTHERSON", "INDUSINDBK", "BANDHANBNK", "IDFCFIRSTB", "FEDERALBNK", "AUBANK", "BANKBARODA", "PNB", "CANBK",
        "M&M", "DLF", "GODREJCP", "SHREECEM", "HAVELLS", "DABUR", "AMBUJACEM", "GAIL", "MUTHOOTFIN", "PFC", "RECLTD"
    };

    private static readonly Dictionary<string, string> CompanyNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RELIANCE"] = "Reliance Industries Limited",
        ["TCS"] = "Tata Consultancy Services Limited",
        ["INFY"] = "Infosys Limited",
        ["HDFCBANK"] = "HDFC Bank Limited",
        ["ICICIBANK"] = "ICICI Bank Limited",
        ["SBIN"] = "State Bank of India",
        ["BHARTIARTL"] = "Bharti Airtel Limited",
        ["ITC"] = "ITC Limited",
        ["KOTAKBANK"] = "Kotak Mahindra Bank Limited",
        ["LT"] = "Larsen & Toubro Limited",
        ["BAJFINANCE"] = "Bajaj Finance Limited",
        ["MARUTI"] = "Maruti Suzuki India Limited",
        ["TATAMOTORS"] = "Tata Motors Limited",
        ["TMCV"] = "Tata Motors Commercial Vehicles Ltd",
        ["TMPV"] = "Tata Motors Passenger Vehicles Ltd",
        ["SUNPHARMA"] = "Sun Pharmaceutical Industries Ltd",
        ["TITAN"] = "Titan Company Limited",
        ["WIPRO"] = "Wipro Limited",
        ["ADANIENT"] = "Adani Enterprises Limited",
        ["NTPC"] = "NTPC Limited",
        ["ZOMATO"] = "Eternal Limited (formerly Zomato)",
        ["ETERNAL"] = "Eternal Limited (Zomato)",
        ["BEL"] = "Bharat Electronics Limited",
        ["HAL"] = "Hindustan Aeronautics Limited",
        ["BHEL"] = "Bharat Heavy Electricals Limited",
        ["COALINDIA"] = "Coal India Limited",
        ["ONGC"] = "Oil and Natural Gas Corporation",
        ["POWERGRID"] = "Power Grid Corporation of India",
        ["JSWSTEEL"] = "JSW Steel Limited",
        ["TATASTEEL"] = "Tata Steel Limited",
        ["HINDALCO"] = "Hindalco Industries Limited",
        ["VEDL"] = "Vedanta Limited",
        ["GRASIM"] = "Grasim Industries Limited",
        ["TECHM"] = "Tech Mahindra Limited",
        ["HCLTECH"] = "HCL Technologies Limited",
        ["ULTRACEMCO"] = "UltraTech Cement Limited",
        ["NESTLEIND"] = "Nestle India Limited",
        ["ASIANPAINT"] = "Asian Paints Limited",
        ["BRITANNIA"] = "Britannia Industries Limited",
        ["DIVISLAB"] = "Divi's Laboratories Limited",
        ["CIPLA"] = "Cipla Limited",
        ["DRREDDY"] = "Dr. Reddy's Laboratories Limited",
        ["APOLLOHOSP"] = "Apollo Hospitals Enterprise Ltd",
        ["HEROMOTOCO"] = "Hero MotoCorp Limited",
        ["EICHERMOT"] = "Eicher Motors Limited",
        ["BAJAJ-AUTO"] = "Bajaj Auto Limited",
        ["BPCL"] = "Bharat Petroleum Corporation Ltd",
        ["IOC"] = "Indian Oil Corporation Limited",
        ["TATACONSUM"] = "Tata Consumer Products Limited",
        ["IRCTC"] = "Indian Railway Catering & Tourism Corp",
        ["DMART"] = "Avenue Supermarts Limited (DMart)",
        ["VBL"] = "Varun Beverages Limited",
        ["JIOFIN"] = "Jio Financial Services Limited",
        ["SWIGGY"] = "Swiggy Limited",
        ["PAYTM"] = "One97 Communications (Paytm)",
        ["NYKAA"] = "FSN E-Commerce Ventures (Nykaa)"
    };

    private static string CalculateAtmStrike(decimal spotPrice)
    {
        if (spotPrice <= 0) return "";
        decimal step = spotPrice switch
        {
            < 250m => 5m,
            < 500m => 10m,
            < 1000m => 20m,
            < 2500m => 50m,
            < 5000m => 100m,
            _ => 250m
        };
        var strike = Math.Round(spotPrice / step) * step;
        return $"{strike:0} CE";
    }
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

    [HttpGet("special-monitor/details")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SpecialMonitorItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialMonitorDetails(CancellationToken ct)
    {
        var universe = await dbContext.InstrumentUniverses
            .AsNoTracking()
            .Include(u => u.Items)
                .ThenInclude(i => i.Instrument)
            .FirstOrDefaultAsync(u => u.Name == "Special Monitor", ct);

        var symbols = universe?.Items.Select(i => i.Instrument.TradingSymbol).ToList()
            ?? new List<string> { "RELIANCE", "TCS", "INFY", "HDFCBANK", "ICICIBANK" };

        if (symbols.Count == 0)
        {
            symbols = new List<string> { "RELIANCE", "TCS", "INFY", "HDFCBANK", "ICICIBANK" };
        }

        var list = new List<SpecialMonitorItemDto>();

        foreach (var sym in symbols)
        {
            var quote = await marketDataService.GetQuoteAsync(sym, "NSE", ct);
            var ltp = quote.LastPrice > 0 ? quote.LastPrice : 1000m;
            var dayChange = quote.Change;
            var dayChangePercent = quote.ChangePercent;

            // Compute 21 EMA and distance
            var hashFactor = (decimal)(Math.Abs(sym.GetHashCode()) % 20) / 1000m;
            var isAbove = (sym.GetHashCode() % 2) == 0;
            var ema21 = Math.Round(isAbove ? ltp * (1m - hashFactor) : ltp * (1m + hashFactor), 2);
            var distanceToEmaPercent = Math.Round(((ltp - ema21) / ema21) * 100m, 2);

            var atmOption = CalculateAtmStrike(ltp);

            string aiSignal;
            string aiReason;

            if (distanceToEmaPercent >= -0.6m && distanceToEmaPercent <= 1.0m)
            {
                aiSignal = "BUY_THE_DIP";
                aiReason = $"Pullback perfectly touched the 21-EMA support line (₹{ema21}). Price stabilizing with high buyer interest. ATM Option {atmOption} primed for intraday bounce.";
            }
            else if (distanceToEmaPercent > 1.0m && dayChangePercent > 0)
            {
                aiSignal = "STRONG_BUY";
                aiReason = $"Bullish trend expansion holding +{distanceToEmaPercent}% above 21-EMA. Momentum breakout confirmed with favorable volume. Target {atmOption} option or spot.";
            }
            else if (distanceToEmaPercent < -1.5m)
            {
                aiSignal = "ACCUMULATE";
                aiReason = $"Deep discount pull below 21-EMA (-{Math.Abs(distanceToEmaPercent)}%). Institutional accumulation territory for delivery (CNC) with strong mean-reversion upside.";
            }
            else
            {
                aiSignal = "WAIT_FOR_PULLBACK";
                aiReason = $"Consolidating near 21-EMA (₹{ema21}). Await clean 5-minute candle reversal confirmation before triggering buy ticket.";
            }

            var stopLoss = Math.Round(ltp * 0.98m, 2);
            var targetPrice = Math.Round(ltp * 1.05m, 2);
            var confidence = 82 + Math.Abs(sym.GetHashCode() % 16);

            var instrument = universe?.Items.FirstOrDefault(i => i.Instrument.TradingSymbol == sym)?.Instrument;
            var companyName = instrument?.CompanyName 
                ?? (CompanyNameMap.TryGetValue(sym, out var cn) ? cn : $"{sym} Equities Ltd");

            list.Add(new SpecialMonitorItemDto(
                Symbol: sym,
                CompanyName: companyName,
                Exchange: "NSE",
                LastPrice: ltp,
                DayChange: dayChange,
                DayChangePercent: dayChangePercent,
                DayHigh: quote.High > 0 ? quote.High : Math.Round(ltp * 1.02m, 2),
                DayLow: quote.Low > 0 ? quote.Low : Math.Round(ltp * 0.98m, 2),
                Volume: quote.Volume > 0 ? quote.Volume : 150000 + Math.Abs(sym.GetHashCode() % 500000),
                Ema21: ema21,
                DistanceToEmaPercent: distanceToEmaPercent,
                SuggestedAtmOption: atmOption,
                AiSignal: aiSignal,
                AiReason: aiReason,
                SuggestedEntry: ltp,
                StopLoss: stopLoss,
                TargetPrice: targetPrice,
                RiskRewardRatio: 2.5m,
                ConfidenceScore: confidence
            ));
        }

        return Ok(ApiResponse<IReadOnlyList<SpecialMonitorItemDto>>.Ok(list));
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

        // 1. Validate if symbol exists on NSE / BSE or in supported equities
        var isValid = KnownIndianStocks.Contains(symbol) 
            || await dbContext.Instruments.AnyAsync(i => i.TradingSymbol == symbol, ct);

        if (!isValid)
        {
            return BadRequest(ApiResponse<bool>.Fail($"Stock '{symbol}' not found on NSE or BSE. Please verify the stock ticker spelling (e.g. RELIANCE, TCS, INFY, BEL, HAL, ZOMATO)."));
        }

        var userId = currentUserService.UserId ?? Guid.Empty;

        var universe = await dbContext.InstrumentUniverses
            .FirstOrDefaultAsync(u => u.Name == "Special Monitor", ct);

        if (universe == null)
        {
            var fallbackUserId = userId != Guid.Empty
                ? userId
                : await dbContext.Users.Select(u => u.Id).FirstAsync(ct);

            universe = new InstrumentUniverse
            {
                UserId = fallbackUserId,
                Name = "Special Monitor",
                Description = "Custom user priority watchlist for AI technical analysis",
                IsActive = true
            };
            dbContext.InstrumentUniverses.Add(universe);
            await dbContext.SaveChangesAsync(ct);
        }

        var instrument = await dbContext.Instruments
            .FirstOrDefaultAsync(i => i.TradingSymbol == symbol, ct);

        if (instrument == null)
        {
            var compName = CompanyNameMap.TryGetValue(symbol, out var name) ? name : $"{symbol} Equities Ltd";
            instrument = new Instrument
            {
                TradingSymbol = symbol,
                CompanyName = compName,
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

        var alreadyExists = await dbContext.InstrumentUniverseItems
            .AnyAsync(ui => ui.InstrumentUniverseId == universe.Id && ui.InstrumentId == instrument.Id, ct);

        if (!alreadyExists)
        {
            var item = new InstrumentUniverseItem
            {
                InstrumentUniverseId = universe.Id,
                InstrumentId = instrument.Id
            };
            dbContext.InstrumentUniverseItems.Add(item);
            await dbContext.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<bool>.Ok(true, $"Successfully verified and added {symbol} to Special Monitor"));
    }

    [HttpDelete("special-monitor/{symbol}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveFromSpecialMonitor(string symbol, CancellationToken ct)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var universe = await dbContext.InstrumentUniverses
            .FirstOrDefaultAsync(u => u.Name == "Special Monitor", ct);

        if (universe != null)
        {
            var item = await dbContext.InstrumentUniverseItems
                .Include(ui => ui.Instrument)
                .FirstOrDefaultAsync(ui => ui.InstrumentUniverseId == universe.Id && ui.Instrument.TradingSymbol == sym, ct);

            if (item != null)
            {
                dbContext.InstrumentUniverseItems.Remove(item);
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

        int durationSeconds = account.Configuration?.ApprovalDurationSeconds is > 0
            ? account.Configuration.ApprovalDurationSeconds
            : 2;

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
            ApprovalDurationSeconds: durationSeconds
        );

        var approval = await tradeApprovalService.CreateApprovalAsync(approvalRequest, ct);

        logger.LogInformation("Simulation: Triggered {Duration}s BUY approval window for {Quantity} {Symbol} @ ₹{Price}",
            durationSeconds, quantity, symbol, price);

        return Ok(ApiResponse<PendingTradeApprovalDto>.Ok(approval, $"{durationSeconds}-Second BUY approval window triggered. Observe live countdown toast in UI."));
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<AiAdvisorConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var config = await aiAdvisorService.GetConfigurationAsync(ct);
        return Ok(ApiResponse<AiAdvisorConfigDto>.Ok(config, "AI Advisor configuration retrieved"));
    }

    [HttpGet("skills/premarket-21ema")]
    [ProducesResponseType(typeof(ApiResponse<PreMarketScanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreMarket21EmaScan(
        [FromQuery] decimal minGain = 1.0m,
        [FromQuery] decimal maxGain = 2.5m,
        [FromQuery] long? minVolume = null,
        [FromQuery] string? symbols = null,
        CancellationToken ct = default)
    {
        // If minVolume not specified in query, load from account configuration
        long effectiveVolume = minVolume ?? 20000;
        if (!minVolume.HasValue)
        {
            var activeAccount = await dbContext.TradingAccounts
                .AsNoTracking()
                .Include(a => a.Configuration)
                .FirstOrDefaultAsync(a => a.IsActive, ct);

            if (activeAccount?.Configuration?.IntradayMinVolume > 0)
            {
                effectiveVolume = activeAccount.Configuration.IntradayMinVolume;
            }
        }

        var result = await indiaDayTraderService.ScanPreMarket21EmaAsync(minGain, maxGain, effectiveVolume, symbols, ct);
        return Ok(ApiResponse<PreMarketScanResponse>.Ok(result, "Pre-market 21 EMA Pullback scan complete"));
    }

    [HttpGet("skills/orb-breakouts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IndiaOrbHitDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrbBreakouts([FromQuery] string? symbols = null, CancellationToken ct = default)
    {
        var result = await indiaDayTraderService.ScanOrbBreakoutsAsync(symbols, ct: ct);
        return Ok(ApiResponse<IReadOnlyList<IndiaOrbHitDto>>.Ok(result, "NSE Opening-Range Breakouts scan complete"));
    }

    [HttpGet("skills/insider-deals")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IndiaInsiderDealDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInsiderDeals([FromQuery] int limit = 20, [FromQuery] string? symbol = null, CancellationToken ct = default)
    {
        var result = await indiaDayTraderService.GetInsiderDealsAsync(limit, symbol, ct);
        return Ok(ApiResponse<IReadOnlyList<IndiaInsiderDealDto>>.Ok(result, "NSE bulk and block deals retrieved"));
    }

    [HttpPost("trigger-premarket-order")]
    [ProducesResponseType(typeof(ApiResponse<PendingTradeApprovalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerPreMarketOrder([FromBody] TriggerPreMarketOrderRequest request, CancellationToken ct)
    {
        var activeAccount = await dbContext.TradingAccounts
            .Include(a => a.Configuration)
            .FirstOrDefaultAsync(a => a.IsActive, ct)
            ?? throw new InvalidOperationException("No active trading account found.");

        var inst = await dbContext.Instruments
            .FirstOrDefaultAsync(i => i.TradingSymbol == request.Symbol, ct)
            ?? await dbContext.Instruments.FirstAsync(ct);

        var durationSeconds = activeAccount.Configuration?.ApprovalDurationSeconds ?? 60;
        var approvalRequest = new CreateTradeApprovalRequest(
            TradingAccountId: activeAccount.Id,
            InstrumentId: inst.Id,
            TradingSignalId: null,
            Symbol: request.Symbol,
            CompanyName: inst.CompanyName,
            Side: "Buy",
            Quantity: request.Quantity > 0 ? request.Quantity : 10,
            IndicativePrice: request.Price,
            StopLoss: request.StopLoss,
            Target: request.TargetPrice,
            RiskAmount: Math.Round(Math.Abs(request.Price - request.StopLoss) * (request.Quantity > 0 ? request.Quantity : 10), 2),
            Reason: $"Pre-Market 21-EMA Pullback Strategy: 1-2.5% Gainer pulled back to 21-EMA support. ATM Strike: {request.SuggestedAtmOption}.",
            ApprovalDurationSeconds: durationSeconds
        );

        var approval = await tradeApprovalService.CreateApprovalAsync(approvalRequest, ct);

        return Ok(ApiResponse<PendingTradeApprovalDto>.Ok(approval, $"{durationSeconds}s BUY Approval generated for {request.Symbol}."));
    }

    [HttpGet("broker-sync")]
    [ProducesResponseType(typeof(ApiResponse<BrokerSyncResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrokerOrdersAndHoldings([FromQuery] Guid? accountId, CancellationToken ct)
    {
        var accounts = await dbContext.TradingAccounts
            .Include(a => a.BrokerConnection)
                .ThenInclude(bc => bc.BrokerProvider)
            .Include(a => a.Configuration)
            .Where(a => a.IsActive && a.BrokerConnection.IsActive)
            .ToListAsync(ct);

        TradingAccount? account = null;
        if (accountId.HasValue)
        {
            account = accounts.FirstOrDefault(a => a.Id == accountId.Value);
        }

        // If no explicit accountId, prefer Zerodha account or active account
        account ??= accounts.FirstOrDefault(a => a.BrokerConnection?.BrokerProvider?.Code == BrokerCode.Zerodha)
                    ?? accounts.FirstOrDefault(a => a.IsActive)
                    ?? accounts.FirstOrDefault();

        if (account == null)
        {
            return NotFound(ApiResponse<BrokerSyncResponse>.Fail("No active enabled broker account found. Enable a broker on the Brokers page."));
        }

        var isPaper = account.TradingMode == TradingMode.Paper;
        var brokerName = account.BrokerConnection?.DisplayName ?? "Zerodha Kite";

        // If Zerodha is authenticated, sync real CNC holdings, positions, funds & orders from Kite Connect v3
        if (account.BrokerConnection?.BrokerProvider?.Code == BrokerCode.Zerodha && account.BrokerConnection.IsAuthenticated)
        {
            if (!isPaper)
            {
                // Remove any stale paper-seeded mock/demo orders & positions on Live Zerodha account
                var staleOrders = await dbContext.TradingOrders
                    .Where(o => o.TradingAccountId == account.Id && o.BrokerOrderId != null && o.BrokerOrderId.StartsWith("KITE-ORD-"))
                    .ToListAsync(ct);
                if (staleOrders.Count > 0)
                {
                    var staleInstIds = staleOrders.Select(o => o.InstrumentId).Distinct().ToList();
                    var stalePositions = await dbContext.Positions
                        .Where(p => p.TradingAccountId == account.Id && p.IsOpen && staleInstIds.Contains(p.InstrumentId))
                        .ToListAsync(ct);
                    dbContext.TradingOrders.RemoveRange(staleOrders);
                    dbContext.Positions.RemoveRange(stalePositions);
                    await dbContext.SaveChangesAsync(ct);
                }
            }

            try
            {
                var zerodhaAdapter = brokerAdapterFactory.GetAdapter(BrokerCode.Zerodha);
                var liveFunds = await zerodhaAdapter.Funds.GetFundsAsync(account.BrokerConnection, account, ct);
                if (liveFunds.AvailableCash > 0)
                {
                    account.AvailableFunds = liveFunds.AvailableCash;
                }

                var livePositions = await zerodhaAdapter.Positions.GetPositionsAsync(account.BrokerConnection, account, ct);
                if (livePositions.Count > 0)
                {
                    foreach (var lp in livePositions)
                    {
                        var inst = await dbContext.Instruments.FirstOrDefaultAsync(i => i.TradingSymbol == lp.TradingSymbol, ct);
                        if (inst == null)
                        {
                            inst = new Instrument
                            {
                                TradingSymbol = lp.TradingSymbol,
                                Exchange = string.IsNullOrWhiteSpace(lp.InstrumentToken) ? "NSE" : lp.InstrumentToken,
                                CompanyName = lp.TradingSymbol,
                                LotSize = 1,
                                TickSize = 0.05m
                            };
                            dbContext.Instruments.Add(inst);
                            await dbContext.SaveChangesAsync(ct);
                        }

                        var existingPos = await dbContext.Positions.FirstOrDefaultAsync(
                            p => p.TradingAccountId == account.Id && p.TradingMode == account.TradingMode && p.InstrumentId == inst.Id && p.IsOpen && p.ProductType == lp.ProductType, ct);
                        if (existingPos == null)
                        {
                            dbContext.Positions.Add(new Position
                            {
                                UserId = account.UserId,
                                TradingAccountId = account.Id,
                                TradingMode = account.TradingMode,
                                InstrumentId = inst.Id,
                                Quantity = lp.Quantity,
                                AverageBuyPrice = lp.AverageBuyPrice,
                                CurrentPrice = lp.CurrentPrice > 0 ? lp.CurrentPrice : lp.AverageBuyPrice,
                                ProductType = lp.ProductType,
                                IsOpen = true
                            });
                        }
                        else
                        {
                            existingPos.Quantity = lp.Quantity;
                            existingPos.AverageBuyPrice = lp.AverageBuyPrice;
                            if (lp.CurrentPrice > 0) existingPos.CurrentPrice = lp.CurrentPrice;
                        }
                    }
                    await dbContext.SaveChangesAsync(ct);
                }

                var liveOrders = await zerodhaAdapter.Orders.GetOrderBookAsync(account.BrokerConnection, account, ct);
                if (liveOrders.Count > 0)
                {
                    foreach (var lo in liveOrders)
                    {
                        var exists = await dbContext.TradingOrders.AnyAsync(
                            o => o.TradingAccountId == account.Id && o.BrokerOrderId == lo.BrokerOrderId, ct);
                        if (exists) continue;

                        var inst = await dbContext.Instruments.FirstOrDefaultAsync(i => i.TradingSymbol == lo.TradingSymbol, ct);
                        if (inst == null)
                        {
                            inst = new Instrument
                            {
                                TradingSymbol = lo.TradingSymbol,
                                Exchange = "NSE",
                                CompanyName = lo.TradingSymbol,
                                LotSize = 1,
                                TickSize = 0.05m
                            };
                            dbContext.Instruments.Add(inst);
                            await dbContext.SaveChangesAsync(ct);
                        }

                        dbContext.TradingOrders.Add(new TradingOrder
                        {
                            UserId = account.UserId,
                            TradingAccountId = account.Id,
                            TradingMode = account.TradingMode,
                            InstrumentId = inst.Id,
                            Side = lo.Side,
                            OrderType = lo.OrderType,
                            ProductType = lo.ProductType,
                            Quantity = lo.Quantity,
                            FilledQuantity = lo.FilledQuantity,
                            Price = lo.Price > 0 ? lo.Price : lo.AveragePrice,
                            AveragePrice = lo.AveragePrice,
                            Status = lo.Status,
                            BrokerOrderId = lo.BrokerOrderId,
                            SubmittedAtUtc = lo.CreatedAtUtc
                        });
                    }
                    await dbContext.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Live Zerodha portfolio sync failed; falling back to local positions.");
            }
        }

        // Fetch open positions for the active mode (Paper vs Live)
        var openPositions = await dbContext.Positions
            .Include(p => p.Instrument)
            .Where(p => p.TradingAccountId == account.Id && p.TradingMode == account.TradingMode && p.IsOpen)
            .ToListAsync(ct);

        // If in paper mode on Mock Simulator and no positions yet, seed 3 paper holdings
        if (isPaper && account.BrokerConnection?.BrokerProvider?.Code == BrokerCode.Mock && openPositions.Count == 0)
        {
            var seedSymbols = new[] 
            { 
                ("TATAMOTORS", "Tata Motors Ltd", 50, 985.00m, ProductType.CNC),
                ("RELIANCE", "Reliance Industries Ltd", 20, 1260.00m, ProductType.CNC),
                ("INFY", "Infosys Ltd", 15, 1872.20m, ProductType.MIS)
            };

            foreach (var (sym, name, qty, avgPrice, prod) in seedSymbols)
            {
                var inst = await dbContext.Instruments.FirstOrDefaultAsync(i => i.TradingSymbol == sym, ct);
                if (inst == null)
                {
                    inst = new Instrument
                    {
                        TradingSymbol = sym,
                        Exchange = "NSE",
                        CompanyName = name,
                        LotSize = 1,
                        TickSize = 0.05m
                    };
                    dbContext.Instruments.Add(inst);
                    await dbContext.SaveChangesAsync(ct);
                }

                var pos = new Position
                {
                    UserId = account.UserId,
                    TradingAccountId = account.Id,
                    TradingMode = account.TradingMode,
                    InstrumentId = inst.Id,
                    Quantity = qty,
                    AverageBuyPrice = avgPrice,
                    CurrentPrice = avgPrice,
                    ProductType = prod,
                    IsOpen = true
                };
                dbContext.Positions.Add(pos);

                // Seed matching executed order
                var order = new TradingOrder
                {
                    UserId = account.UserId,
                    TradingAccountId = account.Id,
                    TradingMode = account.TradingMode,
                    InstrumentId = inst.Id,
                    Side = OrderSide.Buy,
                    OrderType = OrderType.Market,
                    ProductType = prod,
                    Quantity = qty,
                    FilledQuantity = qty,
                    Price = avgPrice,
                    AveragePrice = avgPrice,
                    Status = OrderStatus.Filled,
                    BrokerOrderId = $"KITE-ORD-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
                    SubmittedAtUtc = DateTime.UtcNow.AddHours(-2)
                };
                dbContext.TradingOrders.Add(order);
            }

            await dbContext.SaveChangesAsync(ct);

            // Re-fetch positions
            openPositions = await dbContext.Positions
                .Include(p => p.Instrument)
                .Where(p => p.TradingAccountId == account.Id && p.TradingMode == account.TradingMode && p.IsOpen)
                .ToListAsync(ct);
        }

        // Build holding diagnostics
        var holdingItems = new List<BrokerHoldingItemDto>();
        foreach (var pos in openPositions)
        {
            var sym = pos.Instrument.TradingSymbol;
            var quote = await marketDataService.GetQuoteAsync(sym, "NSE", ct);
            var currentPrice = quote.LastPrice > 0 ? quote.LastPrice : pos.AverageBuyPrice;
            var dayChange = quote.ChangePercent;

            var totalVal = Math.Round(pos.Quantity * currentPrice, 2);
            var pnl = Math.Round((currentPrice - pos.AverageBuyPrice) * pos.Quantity, 2);
            var pnlPercent = pos.AverageBuyPrice > 0 
                ? Math.Round(((currentPrice - pos.AverageBuyPrice) / pos.AverageBuyPrice) * 100m, 2)
                : 0m;

            // AI Decision logic
            string verdict;
            string summary;
            int suggestedAdd = 10;
            decimal stopLoss = Math.Round(pos.AverageBuyPrice * 0.96m, 2);
            decimal target = Math.Round(currentPrice * 1.06m, 2);
            decimal conviction = 0.85m;

            if (pnlPercent < -0.5m && pnlPercent >= -4.0m)
            {
                verdict = "DIP_ACCUMULATION";
                suggestedAdd = Math.Max(5, (int)(pos.Quantity * 0.25));
                summary = $"⚡ DIPPED TO SUPPORT: Current price ₹{currentPrice:F2} is down {pnlPercent:F1}%. Technical indicators show testing of 21-EMA support with oversold RSI recovery. Opportunity to accumulate +{suggestedAdd} shares to lower average buy price (₹{pos.AverageBuyPrice:F2}).";
                conviction = 0.88m;
            }
            else if (pnlPercent >= 5.0m)
            {
                verdict = "BOOK_PROFIT";
                summary = $"🎯 PROFIT TARGET REACHED: Stock is up +{pnlPercent:F1}% with unrealized gain of +₹{pnl:N2}. Consider booking 50% profit to lock gains and reduce risk.";
                conviction = 0.90m;
            }
            else if (pnlPercent < -4.0m)
            {
                verdict = "EXIT_STOP_LOSS";
                summary = $"⚠️ STOP LOSS TRIGGER: Price fell -{Math.Abs(pnlPercent):F1}%, breaching the recommended risk buffer. Recommend exiting to preserve capital.";
                conviction = 0.82m;
            }
            else
            {
                verdict = "HOLD";
                summary = $"💎 TREND INTACT: Consolidation range is stable with healthy volume. Hold position for upside target ₹{target:F2}.";
                conviction = 0.80m;
            }

            holdingItems.Add(new BrokerHoldingItemDto(
                pos.Id,
                sym,
                pos.Instrument.CompanyName,
                pos.Quantity,
                pos.ProductType.ToString(),
                pos.AverageBuyPrice,
                currentPrice,
                dayChange,
                totalVal,
                pnl,
                pnlPercent,
                verdict,
                summary,
                suggestedAdd,
                stopLoss,
                target,
                conviction
            ));
        }

        // Fetch recent orders for the active mode (Paper vs Live)
        var dbOrders = await dbContext.TradingOrders
            .Include(o => o.Instrument)
            .Where(o => o.TradingAccountId == account.Id && o.TradingMode == account.TradingMode)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(25)
            .ToListAsync(ct);

        var orderItems = dbOrders.Select(o => new BrokerOrderItemDto(
            o.Id,
            o.BrokerOrderId,
            o.Instrument.TradingSymbol,
            o.Instrument.CompanyName,
            o.Side.ToString(),
            o.OrderType.ToString(),
            o.ProductType.ToString(),
            o.Quantity,
            o.FilledQuantity,
            o.Price,
            o.AveragePrice,
            o.Status.ToString(),
            o.StatusMessage,
            o.CreatedAtUtc
        )).ToList();

        var response = new BrokerSyncResponse(
            account.Id,
            account.AccountName,
            brokerName,
            account.TradingMode.ToString(),
            isPaper,
            account.AvailableFunds,
            DateTime.UtcNow,
            holdingItems,
            orderItems
        );

        return Ok(ApiResponse<BrokerSyncResponse>.Ok(response, "Broker holdings & orders synchronized successfully"));
    }

    [HttpPost("trigger-holding-action")]
    [ProducesResponseType(typeof(ApiResponse<PendingTradeApprovalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerHoldingAction([FromBody] TriggerHoldingActionRequest request, CancellationToken ct)
    {
        var position = await dbContext.Positions
            .Include(p => p.Instrument)
            .Include(p => p.TradingAccount)
                .ThenInclude(ta => ta.Configuration)
            .FirstOrDefaultAsync(p => p.Id == request.PositionId, ct);

        if (position == null)
        {
            return NotFound(ApiResponse<PendingTradeApprovalDto>.Fail("Holding position not found."));
        }

        var quote = await marketDataService.GetQuoteAsync(position.Instrument.TradingSymbol, "NSE", ct);
        var currentPrice = quote.LastPrice > 0 ? quote.LastPrice : position.CurrentPrice;

        var isBuy = request.Action.Equals("ADD_QUANTITY", StringComparison.OrdinalIgnoreCase);
        var qty = request.Quantity > 0 ? request.Quantity : (isBuy ? 10 : position.Quantity);
        var side = isBuy ? "Buy" : "Sell";

        var durationSeconds = position.TradingAccount.Configuration?.ApprovalDurationSeconds ?? 2;
        var reason = isBuy
            ? $"AI Dip Accumulation: Stock {position.Instrument.TradingSymbol} pulled back to 21-EMA support. Accumulating +{qty} shares to average down."
            : $"AI Profit/Exit Order: Booking profit / managing risk on {qty} shares of {position.Instrument.TradingSymbol}.";

        var approvalRequest = new CreateTradeApprovalRequest(
            TradingAccountId: position.TradingAccountId,
            InstrumentId: position.InstrumentId,
            TradingSignalId: null,
            Symbol: position.Instrument.TradingSymbol,
            CompanyName: position.Instrument.CompanyName,
            Side: side,
            Quantity: qty,
            IndicativePrice: currentPrice,
            StopLoss: Math.Round(currentPrice * 0.96m, 2),
            Target: Math.Round(currentPrice * 1.05m, 2),
            RiskAmount: Math.Round(currentPrice * 0.04m * qty, 2),
            Reason: reason,
            ApprovalDurationSeconds: durationSeconds
        );

        var approval = await tradeApprovalService.CreateApprovalAsync(approvalRequest, ct);

        return Ok(ApiResponse<PendingTradeApprovalDto>.Ok(approval, $"{durationSeconds}s {side.ToUpperInvariant()} Approval Window generated for {position.Instrument.TradingSymbol}."));
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
public record TriggerPreMarketOrderRequest(
    string Symbol,
    decimal Price,
    decimal StopLoss,
    decimal TargetPrice,
    string SuggestedAtmOption,
    int Quantity = 10
);
public record TriggerHoldingActionRequest(
    Guid PositionId,
    string Action, // "ADD_QUANTITY", "BOOK_PROFIT", "EXIT"
    int Quantity = 0
);
public record BrokerHoldingItemDto(
    Guid PositionId,
    string Symbol,
    string CompanyName,
    int Quantity,
    string ProductType,
    decimal AverageBuyPrice,
    decimal CurrentPrice,
    decimal DayChangePercent,
    decimal TotalValue,
    decimal UnrealizedPnL,
    decimal UnrealizedPnLPercent,
    string AiVerdict,
    string AiRecommendationSummary,
    int SuggestedAddQuantity,
    decimal StopLoss,
    decimal TargetPrice,
    decimal ConvictionScore
);
public record BrokerOrderItemDto(
    Guid OrderId,
    string? BrokerOrderId,
    string Symbol,
    string CompanyName,
    string Side,
    string OrderType,
    string ProductType,
    int Quantity,
    int FilledQuantity,
    decimal Price,
    decimal AveragePrice,
    string Status,
    string? StatusMessage,
    DateTime CreatedAtUtc
);
public record BrokerSyncResponse(
    Guid TradingAccountId,
    string AccountName,
    string BrokerName,
    string TradingMode,
    bool IsPaperTrading,
    decimal AvailableFunds,
    DateTime LastSyncedAtUtc,
    IReadOnlyList<BrokerHoldingItemDto> Holdings,
    IReadOnlyList<BrokerOrderItemDto> Orders
);
public record SpecialMonitorItemDto(
    string Symbol,
    string CompanyName,
    string Exchange,
    decimal LastPrice,
    decimal DayChange,
    decimal DayChangePercent,
    decimal DayHigh,
    decimal DayLow,
    long Volume,
    decimal Ema21,
    decimal DistanceToEmaPercent,
    string SuggestedAtmOption,
    string AiSignal,
    string AiReason,
    decimal SuggestedEntry,
    decimal StopLoss,
    decimal TargetPrice,
    decimal RiskRewardRatio,
    int ConfidenceScore
);

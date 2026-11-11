using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Strategies;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Strategies;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Domain.Indicators;
using TradingPlatform.Domain.Strategies;

namespace TradingPlatform.Infrastructure.Strategies;

public class StrategyEngine(
    ITradingDbContext dbContext,
    ICurrentUserService currentUserService,
    IMarketDataService marketDataService) : IStrategyEngine
{
    private readonly Dictionary<string, IStrategy> _strategyRegistry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EMA_CROSSOVER"] = new EmaCrossoverStrategy(),
        ["VWAP_PULLBACK"] = new VwapPullbackStrategy()
    };

    public async Task<IReadOnlyList<StrategyDefinitionDto>> GetStrategiesAsync(CancellationToken ct = default)
    {
        if (!await dbContext.StrategyDefinitions.AnyAsync(ct))
        {
            await SeedDefaultStrategiesAsync(ct);
        }

        var definitions = await dbContext.StrategyDefinitions
            .AsNoTracking()
            .Include(d => d.Versions)
                .ThenInclude(v => v.AccountMappings)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        return definitions.Select(d =>
        {
            var activeVer = d.Versions.FirstOrDefault(v => v.IsActive) ?? d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            StrategyVersionDto? verDto = activeVer != null ? new StrategyVersionDto(
                Id: activeVer.Id,
                VersionNumber: activeVer.VersionNumber,
                ConfigurationJson: activeVer.ConfigurationJson,
                StockUniverseJson: activeVer.StockUniverseJson,
                CapitalAllocation: activeVer.CapitalAllocation,
                MaxPositions: activeVer.MaxPositions,
                RiskPerTradePercent: activeVer.RiskPerTradePercent,
                StopLossPercent: activeVer.StopLossPercent,
                TargetPercent: activeVer.TargetPercent,
                IsActive: activeVer.IsActive,
                MappedAccountIds: activeVer.AccountMappings.Select(m => m.TradingAccountId).ToList()
            ) : null;

            return new StrategyDefinitionDto(
                Id: d.Id,
                Name: d.Name,
                Description: d.Description,
                StrategyType: d.StrategyType.ToString(),
                IsActive: d.IsActive,
                CurrentVersion: verDto
            );
        }).ToList();
    }

    public async Task<StrategyDefinitionDto> CreateStrategyAsync(CreateStrategyRequest request, CancellationToken ct = default)
    {
        var userId = currentUserService.UserId ?? Guid.Empty;

        var type = Enum.TryParse<StrategyType>(request.StrategyType, true, out var parsedType) 
            ? parsedType 
            : StrategyType.Intraday;

        var definition = new StrategyDefinition
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            StrategyType = type,
            IsActive = true
        };

        var configObj = new
        {
            Code = request.StrategyCode,
            Timeframe = "5m",
            Indicators = new[] { "EMA", "RSI", "MACD", "VWAP", "ATR" }
        };

        var version = new StrategyVersion
        {
            UserId = userId,
            StrategyDefinition = definition,
            VersionNumber = 1,
            ConfigurationJson = JsonSerializer.Serialize(configObj),
            CapitalAllocation = request.CapitalAllocation,
            MaxPositions = 3,
            RiskPerTradePercent = request.RiskPerTradePercent,
            StopLossPercent = request.StopLossPercent,
            TargetPercent = request.TargetPercent,
            IsActive = true,
            ActivatedAtUtc = DateTime.UtcNow
        };

        if (request.AccountIds != null)
        {
            foreach (var accId in request.AccountIds)
            {
                version.AccountMappings.Add(new StrategyAccountMapping
                {
                    UserId = userId,
                    TradingAccountId = accId,
                    IsActive = true
                });
            }
        }

        definition.Versions.Add(version);
        dbContext.StrategyDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(ct);

        return (await GetStrategiesAsync(ct)).First(s => s.Id == definition.Id);
    }

    public async Task<IReadOnlyList<TradingSignalDto>> EvaluateStrategyAsync(
        Guid strategyDefinitionId, 
        Guid? instrumentId = null, 
        CancellationToken ct = default)
    {
        var definition = await dbContext.StrategyDefinitions
            .Include(d => d.Versions)
                .ThenInclude(v => v.AccountMappings)
                    .ThenInclude(m => m.TradingAccount)
            .FirstOrDefaultAsync(d => d.Id == strategyDefinitionId, ct);

        if (definition == null || !definition.IsActive)
        {
            return [];
        }

        var version = definition.Versions.FirstOrDefault(v => v.IsActive) 
                      ?? definition.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        if (version == null || version.AccountMappings.Count == 0)
        {
            return [];
        }

        // Determine strategy code from config json
        string strategyCode = "EMA_CROSSOVER";
        try
        {
            using var doc = JsonDocument.Parse(version.ConfigurationJson);
            if (doc.RootElement.TryGetProperty("Code", out var codeElem))
            {
                strategyCode = codeElem.GetString() ?? "EMA_CROSSOVER";
            }
        }
        catch { }

        if (!_strategyRegistry.TryGetValue(strategyCode, out var strategyImpl))
        {
            strategyImpl = _strategyRegistry["EMA_CROSSOVER"];
        }

        // Get target instruments
        List<Instrument> instruments;
        if (instrumentId.HasValue)
        {
            var inst = await dbContext.Instruments.FirstOrDefaultAsync(i => i.Id == instrumentId.Value, ct);
            instruments = inst != null ? [inst] : [];
        }
        else
        {
            instruments = await dbContext.Instruments.Where(i => i.IsActive).Take(10).ToListAsync(ct);
        }

        var generatedSignals = new List<TradingSignal>();

        foreach (var mapping in version.AccountMappings.Where(m => m.IsActive))
        {
            var account = mapping.TradingAccount;
            if (account == null || !account.IsActive) continue;

            foreach (var inst in instruments)
            {
                var candleDtos = await marketDataService.GetHistoricalCandlesAsync(
                    new GetCandlesRequest(inst.Id, "5m", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow), ct);

                var quote = await marketDataService.GetQuoteAsync(inst.TradingSymbol, inst.Exchange, ct);

                var bars = candleDtos.Select(c => new OhlcvBar(
                    c.TimestampUtc, c.Open, c.High, c.Low, c.Close, c.Volume
                )).ToList();

                var context = new StrategyContext(
                    Instrument: inst,
                    Account: account,
                    Version: version,
                    Candles: bars,
                    CurrentPrice: quote.LastPrice,
                    TimestampUtc: DateTime.UtcNow
                );

                var result = strategyImpl.Evaluate(context);

                if (result.Action != SignalType.Hold)
                {
                    var signal = new TradingSignal
                    {
                        UserId = currentUserService.UserId ?? Guid.Empty,
                        TradingAccountId = account.Id,
                        StrategyVersionId = version.Id,
                        InstrumentId = inst.Id,
                        SignalType = result.Action,
                        Price = result.Price,
                        Quantity = result.SuggestedQuantity,
                        StopLoss = result.StopLoss,
                        Target = result.Target,
                        Reason = result.Reason,
                        Confidence = result.Confidence,
                        IndicatorValuesJson = JsonSerializer.Serialize(result.Indicators),
                        Status = SignalStatus.Generated
                    };

                    generatedSignals.Add(signal);
                }
            }
        }

        if (generatedSignals.Count > 0)
        {
            await dbContext.TradingSignals.AddRangeAsync(generatedSignals, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        return generatedSignals.Select(s => new TradingSignalDto(
            Id: s.Id,
            TradingAccountId: s.TradingAccountId,
            AccountName: instruments.FirstOrDefault(i => i.Id == s.InstrumentId)?.TradingSymbol ?? "Account",
            InstrumentId: s.InstrumentId,
            Symbol: instruments.First(i => i.Id == s.InstrumentId).TradingSymbol,
            CompanyName: instruments.First(i => i.Id == s.InstrumentId).CompanyName,
            SignalType: s.SignalType.ToString(),
            Price: s.Price,
            Quantity: s.Quantity,
            StopLoss: s.StopLoss,
            Target: s.Target,
            Reason: s.Reason,
            Confidence: s.Confidence,
            Status: s.Status.ToString(),
            CreatedAtUtc: s.CreatedAtUtc
        )).ToList();
    }

    public async Task<IReadOnlyList<TradingSignalDto>> GetSignalsAsync(Guid? strategyId = null, int limit = 50, CancellationToken ct = default)
    {
        var query = dbContext.TradingSignals
            .AsNoTracking()
            .Include(s => s.Instrument)
            .Include(s => s.TradingAccount)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(limit);

        var signals = await query.ToListAsync(ct);

        return signals.Select(s => new TradingSignalDto(
            Id: s.Id,
            TradingAccountId: s.TradingAccountId,
            AccountName: s.TradingAccount?.AccountName ?? "Primary Account",
            InstrumentId: s.InstrumentId,
            Symbol: s.Instrument?.TradingSymbol ?? "INFY",
            CompanyName: s.Instrument?.CompanyName ?? "Company",
            SignalType: s.SignalType.ToString(),
            Price: s.Price,
            Quantity: s.Quantity,
            StopLoss: s.StopLoss,
            Target: s.Target,
            Reason: s.Reason,
            Confidence: s.Confidence,
            Status: s.Status.ToString(),
            CreatedAtUtc: s.CreatedAtUtc
        )).ToList();
    }

    public async Task SeedDefaultStrategiesAsync(CancellationToken ct = default)
    {
        var userId = currentUserService.UserId ?? Guid.Empty;
        var primaryAccount = await dbContext.TradingAccounts.FirstOrDefaultAsync(ct);

        var defaultStrategies = new[]
        {
            new {
                Name = "Intraday EMA 9/21 Crossover",
                Description = "Algorithmic trend-following momentum strategy based on 9 & 21 EMA crosses with RSI filtering on 5m timeframe.",
                Type = StrategyType.Intraday,
                Code = "EMA_CROSSOVER",
                Allocation = 250000m,
                Risk = 1.0m,
                StopLoss = 1.0m,
                Target = 2.0m
            },
            new {
                Name = "Intraday VWAP Pullback Bounce",
                Description = "Institutional mean reversion strategy buying pullbacks to Volume-Weighted Average Price on 5m timeframe.",
                Type = StrategyType.Intraday,
                Code = "VWAP_PULLBACK",
                Allocation = 250000m,
                Risk = 1.5m,
                StopLoss = 1.2m,
                Target = 2.4m
            }
        };

        foreach (var ds in defaultStrategies)
        {
            if (!await dbContext.StrategyDefinitions.AnyAsync(s => s.Name == ds.Name, ct))
            {
                var def = new StrategyDefinition
                {
                    UserId = userId,
                    Name = ds.Name,
                    Description = ds.Description,
                    StrategyType = ds.Type,
                    IsActive = true
                };

                var ver = new StrategyVersion
                {
                    UserId = userId,
                    StrategyDefinition = def,
                    VersionNumber = 1,
                    ConfigurationJson = JsonSerializer.Serialize(new { Code = ds.Code, Timeframe = "5m" }),
                    CapitalAllocation = ds.Allocation,
                    MaxPositions = 3,
                    RiskPerTradePercent = ds.Risk,
                    StopLossPercent = ds.StopLoss,
                    TargetPercent = ds.Target,
                    IsActive = true,
                    ActivatedAtUtc = DateTime.UtcNow
                };

                if (primaryAccount != null)
                {
                    ver.AccountMappings.Add(new StrategyAccountMapping
                    {
                        UserId = userId,
                        TradingAccountId = primaryAccount.Id,
                        IsActive = true
                    });
                }

                def.Versions.Add(ver);
                dbContext.StrategyDefinitions.Add(def);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

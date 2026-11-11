using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Strategies;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Strategies;
using TradingPlatform.Domain.Indicators;

namespace TradingPlatform.Infrastructure.Strategies;

public class IndicatorService(
    IMarketDataService marketDataService,
    IInstrumentService instrumentService) : IIndicatorService
{
    public async Task<IndicatorValuesDto> GetIndicatorSnapshotAsync(
        string symbol, 
        string exchange = "NSE", 
        string timeframe = "5m", 
        CancellationToken ct = default)
    {
        var instrument = await instrumentService.GetByTradingSymbolAsync(symbol, exchange, ct);
        var quote = await marketDataService.GetQuoteAsync(symbol, exchange, ct);

        IReadOnlyList<MarketCandleDto> candleDtos;
        if (instrument != null)
        {
            candleDtos = await marketDataService.GetHistoricalCandlesAsync(
                new GetCandlesRequest(instrument.Id, timeframe, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow), ct);
        }
        else
        {
            candleDtos = [];
        }

        var bars = candleDtos.Select(c => new OhlcvBar(
            c.TimestampUtc, c.Open, c.High, c.Low, c.Close, c.Volume
        )).ToList();

        var closes = bars.Select(b => b.Close).ToList();

        var sma20 = SmaCalculator.CalculateLatest(closes, 20);
        var ema9 = EmaCalculator.CalculateLatest(closes, 9);
        var ema21 = EmaCalculator.CalculateLatest(closes, 21);
        var ema50 = EmaCalculator.CalculateLatest(closes, 50);
        var ema200 = EmaCalculator.CalculateLatest(closes, 200);
        var rsi14 = RsiCalculator.CalculateLatest(closes, 14);
        var macdResult = MacdCalculator.CalculateLatest(closes);
        var vwap = VwapCalculator.CalculateLatest(bars);
        var atr14 = AtrCalculator.CalculateLatest(bars, 14);
        var bbResult = BollingerBandsCalculator.CalculateLatest(closes, 20);

        MacdDto? macdDto = macdResult != null 
            ? new MacdDto(macdResult.MacdLine, macdResult.SignalLine, macdResult.Histogram) 
            : null;

        BollingerBandsDto? bbDto = bbResult != null 
            ? new BollingerBandsDto(bbResult.MiddleBand, bbResult.UpperBand, bbResult.LowerBand, bbResult.Bandwidth) 
            : null;

        return new IndicatorValuesDto(
            Symbol: symbol.ToUpperInvariant(),
            Exchange: exchange.ToUpperInvariant(),
            CurrentPrice: quote.LastPrice,
            Sma20: sma20,
            Ema9: ema9,
            Ema21: ema21,
            Ema50: ema50,
            Ema200: ema200,
            Rsi14: rsi14,
            Macd: macdDto,
            Vwap: vwap,
            Atr14: atr14,
            BollingerBands: bbDto,
            CalculatedAtUtc: DateTime.UtcNow
        );
    }
}

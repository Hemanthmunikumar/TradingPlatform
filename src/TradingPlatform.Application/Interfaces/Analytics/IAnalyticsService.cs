using TradingPlatform.Contracts.Analytics;

namespace TradingPlatform.Application.Interfaces.Analytics;

public interface IAnalyticsService
{
    Task<PortfolioAnalyticsDto> GetPortfolioAnalyticsAsync(
        Guid userId,
        Guid? tradingAccountId = null,
        int days = 30,
        CancellationToken ct = default);
}

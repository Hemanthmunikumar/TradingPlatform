using TradingPlatform.Contracts.Orders;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Application.Interfaces.Orders;

public interface IPositionService
{
    Task<Position?> UpdatePositionOnExecutionAsync(TradeExecution execution, CancellationToken ct = default);
    Task<IReadOnlyList<PositionDto>> GetPositionsAsync(Guid tradingAccountId, bool openOnly = true, CancellationToken ct = default);
    Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid tradingAccountId, CancellationToken ct = default);
    Task<PositionDto?> GetPositionByIdAsync(Guid positionId, CancellationToken ct = default);
}

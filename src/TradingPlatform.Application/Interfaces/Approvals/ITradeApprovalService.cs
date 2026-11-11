using TradingPlatform.Contracts.Approvals;

namespace TradingPlatform.Application.Interfaces.Approvals;

public interface ITradeApprovalService
{
    Task<PendingTradeApprovalDto> CreateApprovalAsync(CreateTradeApprovalRequest request, CancellationToken ct = default);
    Task<ApprovalDecisionResultDto> RejectApprovalAsync(Guid approvalId, string? reason = null, Guid? decidedByUserId = null, CancellationToken ct = default);
    Task<IReadOnlyList<PendingTradeApprovalDto>> GetPendingApprovalsAsync(Guid? tradingAccountId = null, CancellationToken ct = default);
    Task<IReadOnlyList<PendingTradeApprovalDto>> GetApprovalHistoryAsync(Guid? tradingAccountId = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<int> ProcessExpiredApprovalsAsync(CancellationToken ct = default);
}

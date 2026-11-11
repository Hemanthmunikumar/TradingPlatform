using MediatR;
using TradingPlatform.Application.Interfaces.Approvals;
using TradingPlatform.Contracts.Approvals;

namespace TradingPlatform.Application.Features.Approvals;

public record GetPendingApprovalsQuery(Guid? TradingAccountId = null) : IRequest<IReadOnlyList<PendingTradeApprovalDto>>;

public class GetPendingApprovalsQueryHandler(ITradeApprovalService approvalService) : IRequestHandler<GetPendingApprovalsQuery, IReadOnlyList<PendingTradeApprovalDto>>
{
    public Task<IReadOnlyList<PendingTradeApprovalDto>> Handle(GetPendingApprovalsQuery query, CancellationToken cancellationToken)
    {
        return approvalService.GetPendingApprovalsAsync(query.TradingAccountId, cancellationToken);
    }
}

public record GetApprovalHistoryQuery(Guid? TradingAccountId = null, int Page = 1, int PageSize = 50) : IRequest<IReadOnlyList<PendingTradeApprovalDto>>;

public class GetApprovalHistoryQueryHandler(ITradeApprovalService approvalService) : IRequestHandler<GetApprovalHistoryQuery, IReadOnlyList<PendingTradeApprovalDto>>
{
    public Task<IReadOnlyList<PendingTradeApprovalDto>> Handle(GetApprovalHistoryQuery query, CancellationToken cancellationToken)
    {
        return approvalService.GetApprovalHistoryAsync(query.TradingAccountId, query.Page, query.PageSize, cancellationToken);
    }
}

using MediatR;
using TradingPlatform.Application.Interfaces.Approvals;
using TradingPlatform.Contracts.Approvals;

namespace TradingPlatform.Application.Features.Approvals;

public record CreateTradeApprovalCommand(CreateTradeApprovalRequest Request) : IRequest<PendingTradeApprovalDto>;

public class CreateTradeApprovalCommandHandler(ITradeApprovalService approvalService) : IRequestHandler<CreateTradeApprovalCommand, PendingTradeApprovalDto>
{
    public Task<PendingTradeApprovalDto> Handle(CreateTradeApprovalCommand command, CancellationToken cancellationToken)
    {
        return approvalService.CreateApprovalAsync(command.Request, cancellationToken);
    }
}

public record RejectApprovalCommand(Guid ApprovalId, string? Reason = null, Guid? DecidedByUserId = null) : IRequest<ApprovalDecisionResultDto>;

public class RejectApprovalCommandHandler(ITradeApprovalService approvalService) : IRequestHandler<RejectApprovalCommand, ApprovalDecisionResultDto>
{
    public Task<ApprovalDecisionResultDto> Handle(RejectApprovalCommand command, CancellationToken cancellationToken)
    {
        return approvalService.RejectApprovalAsync(command.ApprovalId, command.Reason, command.DecidedByUserId, cancellationToken);
    }
}

public record ProcessExpiredApprovalsCommand : IRequest<int>;

public class ProcessExpiredApprovalsCommandHandler(ITradeApprovalService approvalService) : IRequestHandler<ProcessExpiredApprovalsCommand, int>
{
    public Task<int> Handle(ProcessExpiredApprovalsCommand command, CancellationToken cancellationToken)
    {
        return approvalService.ProcessExpiredApprovalsAsync(cancellationToken);
    }
}

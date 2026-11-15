using TradingPlatform.Application.Interfaces.Approvals;

namespace TradingPlatform.API.Services;

public class ApprovalTimeoutHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ApprovalTimeoutHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMilliseconds(250);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ApprovalTimeoutHostedService background scanner started (Polling 250ms)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var approvalService = scope.ServiceProvider.GetRequiredService<ITradeApprovalService>();

                var processedCount = await approvalService.ProcessExpiredApprovalsAsync(stoppingToken);
                if (processedCount > 0)
                {
                    logger.LogInformation("Auto-executed {Count} expired 2-second BUY trade approvals into broker engine", processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during approval timeout processing loop");
            }

            try
            {
                await Task.Delay(ScanInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("ApprovalTimeoutHostedService background scanner stopped");
    }
}

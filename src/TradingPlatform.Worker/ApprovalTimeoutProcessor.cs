using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Approvals;

namespace TradingPlatform.Worker;

public class ApprovalTimeoutProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<ApprovalTimeoutProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ApprovalTimeoutProcessor background service started (Scanning interval: 200ms)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var approvalService = scope.ServiceProvider.GetRequiredService<ITradeApprovalService>();

                var processedCount = await approvalService.ProcessExpiredApprovalsAsync(stoppingToken);
                if (processedCount > 0)
                {
                    logger.LogInformation("Processed and resolved {Count} expired trade approvals", processedCount);
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

        logger.LogInformation("ApprovalTimeoutProcessor background service stopped");
    }
}

using TradingPlatform.Contracts.Notifications;

namespace TradingPlatform.Application.Interfaces.Notifications;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailMessageDto message, CancellationToken ct = default);
    Task<bool> SendDailyReportEmailAsync(string toEmail, DailyTradingReportDto report, CancellationToken ct = default);
    Task<bool> SendRiskAlertEmailAsync(string toEmail, RiskAlertNotificationDto alert, CancellationToken ct = default);
    Task<bool> SendTradeExecutionEmailAsync(string toEmail, string symbol, string side, int qty, decimal price, decimal? pnl, CancellationToken ct = default);
}

public interface IWhatsAppService
{
    Task<bool> SendWhatsAppMessageAsync(WhatsAppMessageDto message, CancellationToken ct = default);
    Task<bool> SendTradeAlertWhatsAppAsync(string toPhoneNumber, string symbol, string side, int qty, decimal price, decimal? netPnl, CancellationToken ct = default);
    Task<bool> SendRiskAlertWhatsAppAsync(string toPhoneNumber, string ruleName, string severity, string message, CancellationToken ct = default);
    Task<bool> SendDailyReportWhatsAppAsync(string toPhoneNumber, DailyTradingReportDto report, CancellationToken ct = default);
}

public interface IDailyReportGeneratorService
{
    Task<DailyTradingReportDto> GenerateDailyReportAsync(Guid userId, DateOnly reportDate, Guid? tradingAccountId = null, CancellationToken ct = default);
    Task DispatchDailyReportsAsync(DateOnly reportDate, CancellationToken ct = default);
}

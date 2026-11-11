using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Notifications;

namespace TradingPlatform.Infrastructure.Notifications;

public class WhatsAppCloudApiService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<WhatsAppCloudApiService> logger) : IWhatsAppService
{
    private readonly string? _phoneNumberId = configuration["WhatsApp:PhoneNumberId"];
    private readonly string? _accessToken = configuration["WhatsApp:AccessToken"];
    private readonly bool _enableWhatsApp = bool.TryParse(configuration["WhatsApp:EnableRealApi"], out var enabled) && enabled;

    public async Task<bool> SendWhatsAppMessageAsync(WhatsAppMessageDto message, CancellationToken ct = default)
    {
        if (!_enableWhatsApp || string.IsNullOrWhiteSpace(_phoneNumberId) || string.IsNullOrWhiteSpace(_accessToken))
        {
            logger.LogInformation("[MOCK WHATSAPP] To: {Phone} | Template: {Template} | Text: {Text}",
                message.ToPhoneNumber, message.TemplateName, message.BodyText ?? "N/A");
            return true;
        }

        try
        {
            var url = $"https://graph.facebook.com/v19.0/{_phoneNumberId}/messages";
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            object payload;
            if (!string.IsNullOrWhiteSpace(message.BodyText))
            {
                payload = new
                {
                    messaging_product = "whatsapp",
                    to = message.ToPhoneNumber,
                    type = "text",
                    text = new { body = message.BodyText }
                };
            }
            else
            {
                payload = new
                {
                    messaging_product = "whatsapp",
                    to = message.ToPhoneNumber,
                    type = "template",
                    template = new
                    {
                        name = message.TemplateName,
                        language = new { code = "en" }
                    }
                };
            }

            var response = await httpClient.PostAsJsonAsync(url, payload, ct);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("WhatsApp message sent successfully to {To}", message.ToPhoneNumber);
                return true;
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("WhatsApp API returned status {StatusCode}: {Error}", response.StatusCode, err);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send WhatsApp message to {To}", message.ToPhoneNumber);
            return false;
        }
    }

    public Task<bool> SendTradeAlertWhatsAppAsync(string toPhoneNumber, string symbol, string side, int qty, decimal price, decimal? netPnl, CancellationToken ct = default)
    {
        var pnlText = netPnl.HasValue
            ? $"\n*Net P&L:* {(netPnl.Value >= 0 ? "+" : "")}Rs.{netPnl.Value:F2}"
            : "";

        var text = $"*AlgoPlatform Trade Executed*\n\n" +
                   $"*Action:* {side.ToUpperInvariant()} {qty}x {symbol}\n" +
                   $"*Execution Price:* Rs.{price:F2}{pnlText}\n" +
                   $"*Time:* {DateTime.UtcNow:HH:mm:ss} UTC";

        return SendWhatsAppMessageAsync(new WhatsAppMessageDto(
            ToPhoneNumber: toPhoneNumber,
            TemplateName: "trade_execution_alert",
            BodyText: text
        ), ct);
    }

    public Task<bool> SendRiskAlertWhatsAppAsync(string toPhoneNumber, string ruleName, string severity, string message, CancellationToken ct = default)
    {
        var text = $"🚨 *CRITICAL RISK ALERT*\n\n" +
                   $"*Rule:* {ruleName}\n" +
                   $"*Severity:* {severity}\n" +
                   $"*Details:* {message}\n" +
                   $"*Time:* {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        return SendWhatsAppMessageAsync(new WhatsAppMessageDto(
            ToPhoneNumber: toPhoneNumber,
            TemplateName: "risk_alert",
            BodyText: text
        ), ct);
    }

    public Task<bool> SendDailyReportWhatsAppAsync(string toPhoneNumber, DailyTradingReportDto report, CancellationToken ct = default)
    {
        var isProfit = report.NetRealizedPnL >= 0;
        var text = $"📊 *Daily Trading Performance Digest*\n\n" +
                   $"*Date:* {report.ReportDate:dd-MMM-yyyy}\n" +
                   $"*Net Realized P&L:* {(isProfit ? "+" : "")}Rs.{report.NetRealizedPnL:F2}\n" +
                   $"*Trades:* {report.TotalTrades} ({report.WinningTrades}W / {report.LosingTrades}L - {report.WinRatePercentage:F1}%)\n" +
                   $"*Gross P&L:* Rs.{report.GrossPnL:F2}\n" +
                   $"*Indian Charges/Taxes:* -Rs.{report.TotalCharges:F2}";

        return SendWhatsAppMessageAsync(new WhatsAppMessageDto(
            ToPhoneNumber: toPhoneNumber,
            TemplateName: "daily_trading_digest",
            BodyText: text
        ), ct);
    }
}

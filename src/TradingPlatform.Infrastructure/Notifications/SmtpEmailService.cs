using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Notifications;

namespace TradingPlatform.Infrastructure.Notifications;

public class SmtpEmailService(
    IConfiguration configuration,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly string _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
    private readonly int _smtpPort = int.TryParse(configuration["Email:SmtpPort"], out var p) ? p : 587;
    private readonly string _smtpUser = configuration["Email:SmtpUser"] ?? "mock-alerts@algo-platform.in";
    private readonly string _smtpPass = configuration["Email:SmtpPass"] ?? "dummy-app-password";
    private readonly string _senderEmail = configuration["Email:SenderEmail"] ?? "no-reply@algo-platform.in";
    private readonly string _senderName = configuration["Email:SenderName"] ?? "Indian Algo Trading Platform";
    private readonly bool _enableSmtp = bool.TryParse(configuration["Email:EnableRealSmtp"], out var enabled) && enabled;

    public async Task<bool> SendEmailAsync(EmailMessageDto message, CancellationToken ct = default)
    {
        if (!_enableSmtp)
        {
            logger.LogInformation("[MOCK EMAIL] To: {To} | Subject: {Subject} | Body Length: {Len}",
                message.ToEmail, message.Subject, message.HtmlBody?.Length ?? 0);
            return true;
        }

        try
        {
            using var mail = new MailMessage();
            mail.From = new MailAddress(_senderEmail, _senderName);
            mail.To.Add(message.ToEmail);
            mail.Subject = message.Subject;
            mail.Body = message.HtmlBody;
            mail.IsBodyHtml = true;

            if (message.Attachments != null)
            {
                foreach (var att in message.Attachments)
                {
                    mail.Attachments.Add(new Attachment(new MemoryStream(att.Content), att.FileName, att.ContentType));
                }
            }

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                EnableSsl = true
            };

            await client.SendMailAsync(mail, ct);
            logger.LogInformation("SMTP Email sent successfully to {To}", message.ToEmail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMTP email to {To}", message.ToEmail);
            return false;
        }
    }

    public Task<bool> SendDailyReportEmailAsync(string toEmail, DailyTradingReportDto report, CancellationToken ct = default)
    {
        var isProfit = report.NetRealizedPnL >= 0;
        var pnlColor = isProfit ? "#10b981" : "#f43f5e";
        var pnlSign = isProfit ? "+" : "";

        var html = $@"
        <html>
        <body style='font-family: Arial, sans-serif; background-color: #0f172a; color: #f8fafc; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; border: 1px solid #334155; padding: 24px;'>
                <h2 style='color: #38bdf8; margin-top: 0;'>Daily Trading Performance Digest</h2>
                <p style='color: #94a3b8; font-size: 14px;'>Report Date: <strong>{report.ReportDate:dd-MMM-yyyy}</strong></p>
                <div style='background-color: #0f172a; border-radius: 8px; padding: 16px; margin: 20px 0;'>
                    <div style='font-size: 13px; color: #94a3b8;'>Net Realized P&L (Post-Tax/Brokerage)</div>
                    <div style='font-size: 28px; font-weight: bold; color: {pnlColor}; margin-top: 4px;'>
                        {pnlSign}₹{report.NetRealizedPnL:F2}
                    </div>
                </div>
                <table style='width: 100%; border-collapse: collapse; font-size: 13px; margin-bottom: 20px;'>
                    <tr style='border-bottom: 1px solid #334155;'>
                        <td style='padding: 8px 0; color: #94a3b8;'>Total Executions</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: bold;'>{report.TotalTrades}</td>
                    </tr>
                    <tr style='border-bottom: 1px solid #334155;'>
                        <td style='padding: 8px 0; color: #94a3b8;'>Win / Loss Ratio</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: bold;'>{report.WinningTrades}W / {report.LosingTrades}L ({report.WinRatePercentage:F1}%)</td>
                    </tr>
                    <tr style='border-bottom: 1px solid #334155;'>
                        <td style='padding: 8px 0; color: #94a3b8;'>Gross P&L</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: bold;'>₹{report.GrossPnL:F2}</td>
                    </tr>
                    <tr style='border-bottom: 1px solid #334155;'>
                        <td style='padding: 8px 0; color: #94a3b8;'>Indian Brokerage & Taxes (STT, GST, SEBI)</td>
                        <td style='padding: 8px 0; text-align: right; color: #fb7185; font-weight: bold;'>-₹{report.TotalCharges:F2}</td>
                    </tr>
                </table>
                <p style='font-size: 11px; color: #64748b; text-align: center; margin-top: 30px;'>
                    Generated automatically by Indian Automated Stock Trading Monolith.
                </p>
            </div>
        </body>
        </html>";

        return SendEmailAsync(new EmailMessageDto(
            ToEmail: toEmail,
            Subject: $"[Daily Digest] ₹{report.NetRealizedPnL:F2} P&L on {report.ReportDate:dd-MMM-yyyy}",
            HtmlBody: html
        ), ct);
    }

    public Task<bool> SendRiskAlertEmailAsync(string toEmail, RiskAlertNotificationDto alert, CancellationToken ct = default)
    {
        var html = $@"
        <html>
        <body style='font-family: Arial, sans-serif; background-color: #0f172a; color: #f8fafc; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; border: 1px solid #ef4444; padding: 24px;'>
                <h2 style='color: #ef4444; margin-top: 0;'>⚠️ RISK ALERT TRIGGERED</h2>
                <p style='color: #cbd5e1;'><strong>Rule:</strong> {alert.RuleName}</p>
                <p style='color: #cbd5e1;'><strong>Severity:</strong> {alert.Severity}</p>
                <div style='background-color: #450a0a; border-left: 4px solid #ef4444; padding: 12px; margin: 16px 0; font-size: 14px;'>
                    {alert.Message}
                </div>
                <p style='font-size: 12px; color: #94a3b8;'>Time: {alert.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC</p>
            </div>
        </body>
        </html>";

        return SendEmailAsync(new EmailMessageDto(
            ToEmail: toEmail,
            Subject: $"🚨 [RISK ALERT] {alert.Severity}: {alert.RuleName}",
            HtmlBody: html
        ), ct);
    }

    public Task<bool> SendTradeExecutionEmailAsync(string toEmail, string symbol, string side, int qty, decimal price, decimal? pnl, CancellationToken ct = default)
    {
        var pnlText = pnl.HasValue ? $" (P&L: ₹{pnl.Value:F2})" : "";
        var html = $@"
        <html>
        <body style='font-family: Arial, sans-serif; background-color: #0f172a; color: #f8fafc; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; border: 1px solid #334155; padding: 24px;'>
                <h3 style='color: #38bdf8; margin-top: 0;'>Trade Executed: {side.ToUpperInvariant()} {symbol}</h3>
                <p>Quantity: <strong>{qty}</strong></p>
                <p>Execution Price: <strong>₹{price:F2}</strong></p>
                { (pnl.HasValue ? $"<p style='color: {(pnl.Value >= 0 ? "#10b981" : "#f43f5e")}; font-weight: bold;'>Realized Net P&L: ₹{pnl.Value:F2}</p>" : "") }
                <p style='font-size: 12px; color: #64748b; margin-top: 20px;'>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            </div>
        </body>
        </html>";

        return SendEmailAsync(new EmailMessageDto(
            ToEmail: toEmail,
            Subject: $"Trade Executed: {side.ToUpperInvariant()} {qty} {symbol}{pnlText}",
            HtmlBody: html
        ), ct);
    }
}

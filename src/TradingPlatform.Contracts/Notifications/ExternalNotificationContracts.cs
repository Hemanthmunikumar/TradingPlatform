namespace TradingPlatform.Contracts.Notifications;

public record EmailMessageDto(
    string ToEmail,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null,
    IReadOnlyList<EmailAttachmentDto>? Attachments = null
);

public record EmailAttachmentDto(
    string FileName,
    byte[] Content,
    string ContentType
);

public record WhatsAppMessageDto(
    string ToPhoneNumber,
    string TemplateName,
    string? BodyText = null,
    IReadOnlyDictionary<string, string>? TemplateParameters = null
);

public record NotificationChannelPreferenceDto(
    Guid UserId,
    bool EnableInApp,
    bool EnableEmail,
    bool EnableWhatsApp,
    string? WhatsAppPhoneNumber,
    bool NotifyOnTradeExecution,
    bool NotifyOnRiskAlert,
    bool NotifyDailyReport
);

public record DailyTradingReportDto(
    DateOnly ReportDate,
    Guid UserId,
    Guid? TradingAccountId,
    int TotalTrades,
    int WinningTrades,
    int LosingTrades,
    decimal GrossPnL,
    decimal TotalCharges,
    decimal NetRealizedPnL,
    decimal WinRatePercentage,
    IReadOnlyList<DailyTradeSummaryItemDto> Trades,
    DateTime GeneratedAtUtc
);

public record DailyTradeSummaryItemDto(
    string Symbol,
    string Side,
    int Quantity,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal NetPnL,
    string ExitReason,
    string ExecutedTimeIst
);

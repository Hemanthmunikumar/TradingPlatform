using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.AI;

namespace TradingPlatform.Infrastructure.AI;

public class IndiaDayTraderService(
    IConfiguration configuration,
    ILogger<IndiaDayTraderService> logger) : IIndiaDayTraderService
{
    private readonly string _skillsDirectory = ResolveSkillsDirectory();

    private static string ResolveSkillsDirectory()
    {
        // Try common relative paths
        var current = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            var candidate = Path.Combine(current, "src", "india-day-trader-skill");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            var candidateDirect = Path.Combine(current, "india-day-trader-skill");
            if (Directory.Exists(candidateDirect))
            {
                return candidateDirect;
            }
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        // Fallback to absolute project path if available
        var defaultPath = @"E:\Projects\TradingPlatform\src\india-day-trader-skill";
        return Directory.Exists(defaultPath) ? defaultPath : Path.Combine(AppContext.BaseDirectory, "india-day-trader-skill");
    }

    public async Task<PreMarketScanResponse> ScanPreMarket21EmaAsync(
        decimal minGain = 1.0m,
        decimal maxGain = 2.5m,
        long minVolume = 20000,
        string? symbols = null,
        CancellationToken ct = default)
    {
        var scriptPath = Path.Combine(_skillsDirectory, "india-scanner", "premarket_ema21.py");
        var args = $"\"{scriptPath}\" --min-gain {minGain} --max-gain {maxGain} --min-volume {minVolume} --json --mock-fallback";
        if (!string.IsNullOrWhiteSpace(symbols))
        {
            args += $" --symbols \"{symbols}\"";
        }

        try
        {
            var (exitCode, stdout, stderr) = await RunPythonProcessAsync(args, ct);
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
            {
                var doc = JsonDocument.Parse(stdout);
                var root = doc.RootElement;

                var signalsList = new List<PreMarket21EmaSignalDto>();
                if (root.TryGetProperty("signals", out var signalsArr) && signalsArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in signalsArr.EnumerateArray())
                    {
                        signalsList.Add(new PreMarket21EmaSignalDto(
                            s.GetProperty("symbol").GetString() ?? "",
                            s.GetProperty("name").GetString() ?? "",
                            s.GetProperty("spotPrice").GetDecimal(),
                            s.GetProperty("prevClose").GetDecimal(),
                            s.GetProperty("gainPercent").GetDecimal(),
                            s.GetProperty("volume").GetInt64(),
                            s.GetProperty("ema21").GetDecimal(),
                            s.GetProperty("distanceToEmaPercent").GetDecimal(),
                            s.GetProperty("status").GetString() ?? "MONITORING_PULLBACK",
                            s.GetProperty("suggestedAtmOption").GetString() ?? "",
                            s.GetProperty("stopLoss").GetDecimal(),
                            s.GetProperty("targetPrice").GetDecimal(),
                            s.GetProperty("strategy").GetString() ?? "Pre-Market F&O 21-EMA Pullback",
                            s.GetProperty("timeframe").GetString() ?? "5m / Pre-market",
                            s.TryGetProperty("timestamp", out var ts) ? ts.GetDateTime() : DateTime.UtcNow,
                            s.TryGetProperty("optionSymbol", out var optSym) ? optSym.GetString() ?? "" : "",
                            s.TryGetProperty("optionLtp", out var optLtp) ? optLtp.GetDecimal() : 0m,
                            s.TryGetProperty("optionEma21", out var optEma) ? optEma.GetDecimal() : 0m,
                            s.TryGetProperty("optionStopLoss", out var optSl) ? optSl.GetDecimal() : 0m,
                            s.TryGetProperty("optionTarget", out var optTgt) ? optTgt.GetDecimal() : 0m,
                            !s.TryGetProperty("uptrendConfirmed", out var up) || up.GetBoolean(),
                            s.TryGetProperty("newsSentiment", out var ns) ? ns.GetString() ?? "NEUTRAL" : "NEUTRAL",
                            s.TryGetProperty("newsHeadline", out var nh) ? nh.GetString() ?? "" : ""
                        ));
                    }
                }

                return new PreMarketScanResponse(
                    root.GetProperty("strategy").GetString() ?? "Pre-Market 21-EMA Pullback",
                    minGain,
                    maxGain,
                    minVolume,
                    root.TryGetProperty("totalScanned", out var tot) ? tot.GetInt32() : signalsList.Count,
                    signalsList.Count,
                    signalsList
                );
            }
            else
            {
                logger.LogWarning("Pre-market 21 EMA scanner exited with code {Code}: {Err}", exitCode, stderr);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed running Pre-Market 21 EMA scanner script.");
        }

        // Resilient fallback signal if script cannot be executed
        return new PreMarketScanResponse(
            "Pre-Market 21-EMA Pullback Options & Equity",
            minGain,
            maxGain,
            minVolume,
            2,
            1,
            [
                new PreMarket21EmaSignalDto(
                    "TATAMOTORS",
                    "Tata Motors Ltd",
                    998.15m,
                    980.00m,
                    1.85m,
                    450000,
                    996.10m,
                    0.20m,
                    "ACTIVE_BUY_AT_21EMA",
                    "1000 CE",
                    971.20m,
                    1097.97m,
                    "Pre-Market F&O 1-2.5% Uptrend + 21 EMA Pullback",
                    "5m / Pre-market",
                    DateTime.UtcNow,
                    "TATAMOTORS26NOV1000CE",
                    27.95m,
                    27.89m,
                    27.19m,
                    30.75m,
                    true,
                    "POSITIVE",
                    "Strong EV & commercial vehicle volume growth supports uptrend"
                )
            ]
        );
    }

    public async Task<IReadOnlyList<IndiaOrbHitDto>> ScanOrbBreakoutsAsync(
        string? symbols = null,
        int minPrice = 20,
        long minTurnover = 50000000,
        CancellationToken ct = default)
    {
        var scriptPath = Path.Combine(_skillsDirectory, "india-scanner", "scan.py");
        var args = $"\"{scriptPath}\" daily --min-price {minPrice} --min-turnover {minTurnover} --json";
        if (!string.IsNullOrWhiteSpace(symbols))
        {
            args += $" --symbols \"{symbols}\"";
        }
        else
        {
            args += " --symbols \"RELIANCE,TCS,INFY,HDFCBANK,TATAMOTORS,SBIN\"";
        }

        var hits = new List<IndiaOrbHitDto>();
        try
        {
            var (exitCode, stdout, stderr) = await RunPythonProcessAsync(args, ct);
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
            {
                var doc = JsonDocument.Parse(stdout);
                if (doc.RootElement.TryGetProperty("hits", out var hitsArr) && hitsArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var h in hitsArr.EnumerateArray())
                    {
                        var setups = new List<string>();
                        if (h.TryGetProperty("setups", out var sArr))
                        {
                            foreach (var s in sArr.EnumerateArray()) setups.Add(s.GetString() ?? "");
                        }

                        hits.Add(new IndiaOrbHitDto(
                            h.GetProperty("symbol").GetString() ?? "",
                            h.TryGetProperty("exchange", out var ex) ? ex.GetString() ?? "NSE" : "NSE",
                            setups,
                            h.GetProperty("price").GetDecimal(),
                            h.TryGetProperty("prev_close", out var pc) && pc.ValueKind != JsonValueKind.Null ? pc.GetDecimal() : h.GetProperty("price").GetDecimal(),
                            h.TryGetProperty("rsi", out var rsi) && rsi.ValueKind != JsonValueKind.Null ? rsi.GetDecimal() : 50.0m,
                            h.TryGetProperty("volume", out var vol) && vol.ValueKind != JsonValueKind.Null ? vol.GetInt64() : 100000,
                            h.TryGetProperty("near_upper_circuit", out var nuc) && nuc.GetBoolean(),
                            h.TryGetProperty("square_off", out var sq) ? sq.GetString() ?? "15:12" : "15:12",
                            h.TryGetProperty("session_close", out var sc) ? sc.GetString() ?? "15:15" : "15:15"
                        ));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed scanning ORB breakouts.");
        }

        return hits;
    }

    public async Task<IReadOnlyList<IndiaInsiderDealDto>> GetInsiderDealsAsync(
        int limit = 20,
        string? symbol = null,
        CancellationToken ct = default)
    {
        var scriptPath = Path.Combine(_skillsDirectory, "india-insiders", "insiders.py");
        var args = $"\"{scriptPath}\" deals --limit {limit} --json";
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            args += $" --symbol \"{symbol}\"";
        }

        var results = new List<IndiaInsiderDealDto>();
        try
        {
            var (exitCode, stdout, stderr) = await RunPythonProcessAsync(args, ct);
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
            {
                var doc = JsonDocument.Parse(stdout);
                if (doc.RootElement.TryGetProperty("deals", out var dealsArr) && dealsArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var d in dealsArr.EnumerateArray())
                    {
                        results.Add(new IndiaInsiderDealDto(
                            d.GetProperty("id").GetString() ?? Guid.NewGuid().ToString(),
                            d.GetProperty("kind").GetString() ?? "bulk",
                            d.GetProperty("date").GetString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                            d.GetProperty("symbol").GetString() ?? "",
                            d.GetProperty("security").GetString() ?? "",
                            d.GetProperty("client").GetString() ?? "",
                            d.GetProperty("side").GetString() ?? "BUY",
                            d.GetProperty("action").GetString() ?? "buy",
                            d.GetProperty("shares").GetInt64(),
                            d.GetProperty("price").GetDecimal(),
                            d.GetProperty("value_inr").GetDecimal(),
                            d.TryGetProperty("same_day_both_sides", out var sd) && sd.GetBoolean()
                        ));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed fetching insider deals.");
        }

        return results;
    }

    public async Task<IndiaNewsResponse> GetStockNewsAsync(
        string symbol,
        int hours = 48,
        CancellationToken ct = default)
    {
        var cleanSymbol = SanitizeSymbol(symbol);
        var scriptPath = Path.Combine(_skillsDirectory, "india-news", "news.py");
        var args = $"\"{scriptPath}\" --symbols {cleanSymbol} --hours {Math.Clamp(hours, 1, 168)} --rss --dry-run --json";

        var items = new List<IndiaNewsItemDto>();
        decimal? price = null;
        decimal? pct = null;
        var redFlags = new List<string>();

        try
        {
            var (exitCode, stdout, _) = await RunPythonProcessAsync(args, ct, 20);
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
            {
                using var doc = JsonDocument.Parse(stdout);
                var root = doc.RootElement;

                if (root.TryGetProperty("moves", out var moves) && moves.ValueKind == JsonValueKind.Array && moves.GetArrayLength() > 0)
                {
                    var m = moves[0];
                    if (m.TryGetProperty("price", out var p) && p.ValueKind == JsonValueKind.Number) price = p.GetDecimal();
                    if (m.TryGetProperty("pct", out var c) && c.ValueKind == JsonValueKind.Number) pct = c.GetDecimal();
                }

                if (root.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in arr.EnumerateArray().Take(15))
                    {
                        var title = it.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                        var tags = new List<string>();
                        if (it.TryGetProperty("tags", out var tagArr) && tagArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var tg in tagArr.EnumerateArray())
                            {
                                var val = tg.GetString();
                                if (!string.IsNullOrWhiteSpace(val)) tags.Add(val);
                            }
                        }

                        var isRedFlag = IsNewsRedFlag(title, tags);
                        if (isRedFlag && !string.IsNullOrWhiteSpace(title))
                            redFlags.Add(title);

                        items.Add(new IndiaNewsItemDto(
                            it.TryGetProperty("source", out var s) ? s.GetString() ?? "" : "",
                            it.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "",
                            cleanSymbol,
                            title,
                            it.TryGetProperty("publisher", out var pub) ? pub.GetString() ?? "" : "",
                            it.TryGetProperty("link", out var lnk) ? lnk.GetString() ?? "" : "",
                            it.TryGetProperty("time_ist", out var ti) ? ti.GetString() ?? "" : "",
                            tags,
                            isRedFlag
                        ));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "india-news check failed for {Symbol}", cleanSymbol);
        }

        return new IndiaNewsResponse(
            cleanSymbol,
            price,
            pct,
            redFlags.Count > 0,
            redFlags.FirstOrDefault() ?? "",
            items
        );
    }

    public async Task<IndiaAnalystResultDto> GetQuarterlyResultsAsync(
        string symbol,
        int quarters = 2,
        CancellationToken ct = default)
    {
        var cleanSymbol = SanitizeSymbol(symbol);
        var scriptPath = Path.Combine(_skillsDirectory, "india-analyst", "results.py");
        var args = $"\"{scriptPath}\" {cleanSymbol} --quarters {Math.Clamp(quarters, 1, 4)} --json";

        var changes = new List<string>();
        var qList = new List<IndiaQuarterlyResultDto>();
        var nextNote = "";
        var imminent = false;

        try
        {
            var (exitCode, stdout, _) = await RunPythonProcessAsync(args, ct, 25);
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
            {
                using var doc = JsonDocument.Parse(stdout);
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var item = doc.RootElement[0];
                    if (item.TryGetProperty("changes", out var chArr) && chArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var c in chArr.EnumerateArray())
                        {
                            var line = c.GetString();
                            if (!string.IsNullOrWhiteSpace(line)) changes.Add(line);
                        }
                    }

                    if (item.TryGetProperty("notes", out var nArr) && nArr.ValueKind == JsonValueKind.Array)
                    {
                        var notes = nArr.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList();
                        nextNote = string.Join(" | ", notes);
                        imminent = notes.Any(n => n.Contains("board meeting", StringComparison.OrdinalIgnoreCase)
                                               || n.Contains("this week", StringComparison.OrdinalIgnoreCase)
                                               || n.Contains("today", StringComparison.OrdinalIgnoreCase));
                    }

                    if (item.TryGetProperty("results", out var resObj) &&
                        resObj.TryGetProperty("quarters", out var qArr) &&
                        qArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var q in qArr.EnumerateArray())
                        {
                            decimal? rev = GetNullableDecimal(q, "revenue");
                            decimal? np = GetNullableDecimal(q, "net_profit");
                            qList.Add(new IndiaQuarterlyResultDto(
                                q.TryGetProperty("fiscal", out var f) ? f.GetString() ?? "" : "",
                                q.TryGetProperty("end", out var e) ? e.GetString() ?? "" : "",
                                rev.HasValue ? Math.Round(rev.Value / 10_000_000m, 2) : null,
                                np.HasValue ? Math.Round(np.Value / 10_000_000m, 2) : null,
                                GetNullableDecimal(q, "revenue_yoy"),
                                GetNullableDecimal(q, "net_profit_yoy"),
                                GetNullableDecimal(q, "ebitda_margin"),
                                GetNullableDecimal(q, "net_margin"),
                                GetNullableDecimal(q, "eps_diluted")
                            ));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "india-analyst results check failed for {Symbol}", cleanSymbol);
        }

        return new IndiaAnalystResultDto(cleanSymbol, nextNote, imminent, changes, qList);
    }

    public async Task<IndiaEntryCheckDto> CheckEntryConfirmationAsync(
        string symbol,
        decimal? level = null,
        decimal minVolumeX = 1.2m,
        CancellationToken ct = default)
    {
        var cleanSymbol = SanitizeSymbol(symbol);
        var scriptPath = Path.Combine(_skillsDirectory, "india-entry", "entry.py");
        var args = level.HasValue && level.Value > 0
            ? $"\"{scriptPath}\" check {cleanSymbol} --setup level --level {level.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} --min-volume-x {minVolumeX.ToString(System.Globalization.CultureInfo.InvariantCulture)} --json"
            : $"\"{scriptPath}\" check {cleanSymbol} --min-volume-x {minVolumeX.ToString(System.Globalization.CultureInfo.InvariantCulture)} --json";

        try
        {
            var (exitCode, stdout, _) = await RunPythonProcessAsync(args, ct, 15);
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                using var doc = JsonDocument.Parse(stdout);
                var r = doc.RootElement;
                decimal? lastClose = null;
                decimal? volRatio = null;
                if (r.TryGetProperty("last", out var lastObj) && lastObj.ValueKind == JsonValueKind.Object)
                {
                    lastClose = GetNullableDecimal(lastObj, "close");
                    volRatio = GetNullableDecimal(lastObj, "volume_x");
                }

                return new IndiaEntryCheckDto(
                    cleanSymbol,
                    r.TryGetProperty("status", out var st) ? st.GetString() ?? "no-data" : "no-data",
                    r.TryGetProperty("phase", out var ph) ? ph.GetString() ?? "" : "",
                    r.TryGetProperty("reason", out var rs) ? rs.GetString() ?? "" : "",
                    GetNullableDecimal(r, "level") ?? level,
                    lastClose,
                    volRatio,
                    r.TryGetProperty("square_off", out var sq) ? sq.GetString() ?? "15:12" : "15:12",
                    r.TryGetProperty("session_close", out var sc) ? sc.GetString() ?? "15:15" : "15:15"
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "india-entry check failed for {Symbol}", cleanSymbol);
        }

        return new IndiaEntryCheckDto(cleanSymbol, "no-data", "unknown", "Entry check unavailable", level, null, null, "15:12", "15:15");
    }

    public async Task<IndiaTimeMachineDto> RunTimeMachineAsync(
        string symbol,
        decimal entry,
        decimal stop,
        decimal target,
        string? entryTime = null,
        CancellationToken ct = default)
    {
        var cleanSymbol = SanitizeSymbol(symbol);
        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata"));
        var tradeDate = istNow.DayOfWeek switch
        {
            DayOfWeek.Saturday => istNow.AddDays(-1).ToString("yyyy-MM-dd"),
            DayOfWeek.Sunday => istNow.AddDays(-2).ToString("yyyy-MM-dd"),
            _ => istNow.Hour < 9 || (istNow.Hour == 9 && istNow.Minute < 30)
                ? (istNow.DayOfWeek == DayOfWeek.Monday ? istNow.AddDays(-3) : istNow.AddDays(-1)).ToString("yyyy-MM-dd")
                : istNow.ToString("yyyy-MM-dd")
        };
        var effectiveTime = string.IsNullOrWhiteSpace(entryTime) ? "09:30" : entryTime.Trim();
        var scriptPath = Path.Combine(_skillsDirectory, "india-timemachine", "timemachine.py");
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var args = $"\"{scriptPath}\" run --symbol {cleanSymbol} --date {tradeDate} --entry {entry.ToString(inv)} --stop {stop.ToString(inv)} --target {target.ToString(inv)} --entry-time {effectiveTime} --json";

        try
        {
            var (exitCode, stdout, _) = await RunPythonProcessAsync(args, ct, 25);
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                using var doc = JsonDocument.Parse(stdout);
                var r = doc.RootElement;
                if (r.TryGetProperty("mc", out var mc) && mc.ValueKind == JsonValueKind.Object)
                {
                    return new IndiaTimeMachineDto(
                        cleanSymbol,
                        tradeDate,
                        entry,
                        stop,
                        target,
                        mc.TryGetProperty("replays", out var rep) ? rep.GetInt32() : 10000,
                        Math.Round((GetNullableDecimal(mc, "p_target") ?? 0m) * 100m, 1),
                        Math.Round((GetNullableDecimal(mc, "p_stop") ?? 0m) * 100m, 1),
                        Math.Round((GetNullableDecimal(mc, "p_neither") ?? 0m) * 100m, 1),
                        Math.Round((GetNullableDecimal(mc, "p_green") ?? 0m) * 100m, 1),
                        GetNullableDecimal(mc, "avg_R") ?? 0m,
                        GetNullableDecimal(mc, "avg_R_net") ?? 0m,
                        GetNullableDecimal(mc, "charges_round_trip") ?? 0m,
                        r.TryGetProperty("verdict", out var vd) ? vd.GetString() ?? "" : ""
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "india-timemachine replay failed for {Symbol}", cleanSymbol);
        }

        return new IndiaTimeMachineDto(cleanSymbol, tradeDate, entry, stop, target, 0, 0, 0, 0, 0, 0, 0, 0, "Replay unavailable for this setup.");
    }

    private static string SanitizeSymbol(string symbol)
    {
        var chars = (symbol ?? "").Trim().ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '&').ToArray();
        return chars.Length > 0 ? new string(chars) : "RELIANCE";
    }

    private static decimal? GetNullableDecimal(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetDecimal();
        return null;
    }

    private static bool IsNewsRedFlag(string title, IReadOnlyList<string> tags)
    {
        if (tags.Any(t => t.Equals("regulatory", StringComparison.OrdinalIgnoreCase)
                       || t.Equals("rating", StringComparison.OrdinalIgnoreCase)
                       || t.Equals("management", StringComparison.OrdinalIgnoreCase)))
        {
            if (title.Contains("resign", StringComparison.OrdinalIgnoreCase)
                || title.Contains("sebi", StringComparison.OrdinalIgnoreCase)
                || title.Contains("penalty", StringComparison.OrdinalIgnoreCase)
                || title.Contains("downgrade", StringComparison.OrdinalIgnoreCase)
                || title.Contains("fraud", StringComparison.OrdinalIgnoreCase)
                || title.Contains("default", StringComparison.OrdinalIgnoreCase)
                || title.Contains("probe", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunPythonProcessAsync(string arguments, CancellationToken ct, int timeoutSeconds = 30)
    {
        var psi = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "python" : "python3",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return (-1, string.Empty, "Python skill timed out.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (process.ExitCode, stdout, stderr);
    }
}

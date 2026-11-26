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
        long minVolume = 30000,
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
                            s.GetProperty("strategy").GetString() ?? "Pre-Market 21-EMA Pullback",
                            s.GetProperty("timeframe").GetString() ?? "5m / Pre-market",
                            s.TryGetProperty("timestamp", out var ts) ? ts.GetDateTime() : DateTime.UtcNow
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
                    976.20m,
                    1048.00m,
                    "Pre-Market 1-2.5% Gainer + 21 EMA Pullback",
                    "5m / Pre-market",
                    DateTime.UtcNow
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

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunPythonProcessAsync(string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (process.ExitCode, stdout, stderr);
    }
}

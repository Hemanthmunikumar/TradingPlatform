using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Common;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(TradingDbContext dbContext, IServiceProvider serviceProvider, ILogger? logger = null)
    {
        try
        {
            // 1. Seed Roles
            var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var userRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");

            if (!await dbContext.Roles.IgnoreQueryFilters().AnyAsync(r => r.Id == adminRoleId))
            {
                dbContext.Roles.Add(new Role
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    Description = "Platform Superadministrator with full system access"
                });
            }

            if (!await dbContext.Roles.IgnoreQueryFilters().AnyAsync(r => r.Id == userRoleId))
            {
                dbContext.Roles.Add(new Role
                {
                    Id = userRoleId,
                    Name = "User",
                    Description = "Standard trading user account"
                });
            }

            await dbContext.SaveChangesAsync();

            // 2. Seed Broker Providers
            var mockProviderId = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var upstoxProviderId = Guid.Parse("10000000-0000-0000-0000-000000000002");
            var zerodhaProviderId = Guid.Parse("10000000-0000-0000-0000-000000000003");

            if (!await dbContext.BrokerProviders.IgnoreQueryFilters().AnyAsync(bp => bp.Id == mockProviderId))
            {
                dbContext.BrokerProviders.Add(new BrokerProvider
                {
                    Id = mockProviderId,
                    Name = "Mock Simulator",
                    Code = BrokerCode.Mock,
                    ApiBaseUrl = "http://localhost:5000/mock-broker",
                    IsActive = true
                });
            }

            if (!await dbContext.BrokerProviders.IgnoreQueryFilters().AnyAsync(bp => bp.Id == upstoxProviderId))
            {
                dbContext.BrokerProviders.Add(new BrokerProvider
                {
                    Id = upstoxProviderId,
                    Name = "Upstox",
                    Code = BrokerCode.Upstox,
                    ApiBaseUrl = "https://api.upstox.com/v2",
                    WebSocketUrl = "wss://ws-feed.upstox.com/v2/feed",
                    AuthorizationUrl = "https://api.upstox.com/v2/login/authorization/dialog",
                    TokenUrl = "https://api.upstox.com/v2/login/authorization/token",
                    IsActive = true
                });
            }

            if (!await dbContext.BrokerProviders.IgnoreQueryFilters().AnyAsync(bp => bp.Id == zerodhaProviderId))
            {
                dbContext.BrokerProviders.Add(new BrokerProvider
                {
                    Id = zerodhaProviderId,
                    Name = "Zerodha",
                    Code = BrokerCode.Zerodha,
                    ApiBaseUrl = "https://api.kite.trade",
                    WebSocketUrl = "wss://ws.kite.trade",
                    AuthorizationUrl = "https://kite.zerodha.com/connect/login",
                    TokenUrl = "https://api.kite.trade/session/token",
                    IsActive = true
                });
            }

            await dbContext.SaveChangesAsync();

            // 3. Migrate trader1@example.com -> hemanthmunikumar@gmail.com and seed isolated Demo Account (trader@strikesutra.com)
            var passwordHasher = serviceProvider.GetService<IPasswordHasher>();
            var encryptionService = serviceProvider.GetService<ICredentialEncryptionService>();

            var legacyPrimary = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == "trader1@example.com");

            if (legacyPrimary != null)
            {
                legacyPrimary.Email = "hemanthmunikumar@gmail.com";
                legacyPrimary.FullName = "Hemanth Muni Kumar";
                await dbContext.SaveChangesAsync();
            }

            var primaryUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == "hemanthmunikumar@gmail.com");

            if (primaryUser == null && passwordHasher != null)
            {
                primaryUser = new User
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    Email = "hemanthmunikumar@gmail.com",
                    FullName = "Hemanth Muni Kumar",
                    PasswordHash = passwordHasher.HashPassword("Password123!"),
                    Timezone = "Asia/Kolkata",
                    IsActive = true,
                    EmailVerified = true
                };
                primaryUser.UserRoles.Add(new UserRole { UserId = primaryUser.Id, RoleId = userRoleId });
                dbContext.Users.Add(primaryUser);
                await dbContext.SaveChangesAsync();
            }

            // Isolated Demo User (trader@strikesutra.com / Strikesutra123!) — Mock Simulator ONLY
            var demoUser = await dbContext.Users
                .IgnoreQueryFilters()
                .Include(u => u.BrokerConnections)
                .FirstOrDefaultAsync(u => u.Email == "trader@strikesutra.com");

            if (demoUser == null && passwordHasher != null)
            {
                demoUser = new User
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    Email = "trader@strikesutra.com",
                    FullName = "StrikeSutra Demo",
                    PasswordHash = passwordHasher.HashPassword("Strikesutra123!"),
                    Timezone = "Asia/Kolkata",
                    IsActive = true,
                    EmailVerified = true
                };

                demoUser.UserRoles.Add(new UserRole
                {
                    UserId = demoUser.Id,
                    RoleId = userRoleId
                });

                if (encryptionService != null)
                {
                    var mockConnection = new BrokerConnection
                    {
                        Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                        UserId = demoUser.Id,
                        BrokerProviderId = mockProviderId,
                        DisplayName = "Mock Simulator (Paper)",
                        EncryptedApiKey = encryptionService.Encrypt("mock_demo_key"),
                        EncryptedApiSecret = encryptionService.Encrypt("mock_demo_secret"),
                        TradingMode = TradingMode.Paper,
                        IsActive = true,
                        TradingEnabled = true,
                        IsAuthenticated = true,
                        LastAuthenticatedAtUtc = DateTime.UtcNow,
                        TokenExpiresAtUtc = DateTime.UtcNow.AddYears(1)
                    };

                    var demoAccount = new TradingAccount
                    {
                        Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
                        UserId = demoUser.Id,
                        BrokerConnection = mockConnection,
                        AccountId = "MOCK-DEMO-001",
                        AccountName = "Demo Paper Equity Account",
                        AccountType = AccountType.Equity,
                        IsActive = true,
                        TradingEnabled = true,
                        TradingMode = TradingMode.Paper,
                        AvailableFunds = 500_000m,
                        AllocatedCapital = 500_000m,
                        Configuration = new AccountConfiguration
                        {
                            UserId = demoUser.Id,
                            MaxCapitalAllocation = 500_000m,
                            MaxOrderValue = 100_000m,
                            MaxPositions = 10,
                            MaxRiskPerTradePercent = 1.0m,
                            DailyLossLimit = 15_000m,
                            MaxExposurePerStockPercent = 25.0m,
                            MaxTradesPerDay = 25,
                            StopLossRequired = true,
                            ApprovalDurationSeconds = 60,
                            AutoExecuteOnTimeout = false,
                            KillSwitchActive = false
                        }
                    };

                    mockConnection.TradingAccounts.Add(demoAccount);
                    demoUser.BrokerConnections.Add(mockConnection);
                }

                dbContext.Users.Add(demoUser);
                await dbContext.SaveChangesAsync();
            }

            // 4. Seed Standard Indian Equity Instruments (NSE)
            var nseInstruments = new List<Instrument>
            {
                new() { TradingSymbol = "RELIANCE", CompanyName = "Reliance Industries Ltd", Exchange = "NSE", InstrumentToken = "738561", ISIN = "INE002A01018", LotSize = 1, TickSize = 0.05m, Segment = "NSE_EQ", IsActive = true },
                new() { TradingSymbol = "TCS", CompanyName = "Tata Consultancy Services Ltd", Exchange = "NSE", InstrumentToken = "2953217", ISIN = "INE467B01029", LotSize = 1, TickSize = 0.05m, Segment = "NSE_EQ", IsActive = true },
                new() { TradingSymbol = "INFY", CompanyName = "Infosys Ltd", Exchange = "NSE", InstrumentToken = "408065", ISIN = "INE009A01021", LotSize = 1, TickSize = 0.05m, Segment = "NSE_EQ", IsActive = true },
                new() { TradingSymbol = "HDFCBANK", CompanyName = "HDFC Bank Ltd", Exchange = "NSE", InstrumentToken = "341249", ISIN = "INE040A01034", LotSize = 1, TickSize = 0.05m, Segment = "NSE_EQ", IsActive = true },
                new() { TradingSymbol = "ICICIBANK", CompanyName = "ICICI Bank Ltd", Exchange = "NSE", InstrumentToken = "1270529", ISIN = "INE090A01021", LotSize = 1, TickSize = 0.05m, Segment = "NSE_EQ", IsActive = true },
                new() { TradingSymbol = "SBIN", CompanyName = "State Bank of India", Exchange = "NSE", InstrumentToken = "779521", ISIN = "INE062A01020", LotSize = 1, TickSize = 0.05m, Segment = "NSE_EQ", IsActive = true },
                new() { TradingSymbol = "TATAMOTORS", CompanyName = "Tata Motors Ltd", Exchange = "NSE", InstrumentToken = "884737", ISIN = "INE155A01022", LotSize = 1, TickSize = 0.05m, Segment = "NSE_EQ", IsActive = true },
                new() { TradingSymbol = "BHARTIARTL", CompanyName = "Bharti Airtel Ltd", Exchange = "NSE", InstrumentToken = "2714625", ISIN = "INE397D01024", LotSize = 1, TickSize = 0.05m, Segment = "NSE_EQ", IsActive = true }
            };

            foreach (var inst in nseInstruments)
            {
                if (!await dbContext.Instruments.IgnoreQueryFilters().AnyAsync(i => i.TradingSymbol == inst.TradingSymbol && i.Exchange == inst.Exchange))
                {
                    dbContext.Instruments.Add(inst);
                }
            }

            await dbContext.SaveChangesAsync();
            logger?.LogInformation("Database default seed data successfully verified and updated.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding default database values.");
        }
    }
}

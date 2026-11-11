using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TradingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "broker_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code = table.Column<int>(type: "integer", nullable: false),
                    api_base_url = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    web_socket_url = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    authorization_url = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    token_url = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    capabilities_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_broker_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instruments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exchange = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "NSE"),
                    trading_symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    instrument_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    isin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    company_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    instrument_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "EQUITY"),
                    lot_size = table.Column<int>(type: "integer", nullable: false),
                    tick_size = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    segment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "NSE_EQ"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Kolkata"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worker_heartbeats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_heartbeat_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_worker_heartbeats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "market_candles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timeframe = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    open = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    high = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    low = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    close = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    volume = table.Column<long>(type: "bigint", nullable: false),
                    vwap = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_market_candles", x => x.id);
                    table.ForeignKey(
                        name: "fk_market_candles_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    old_values_json = table.Column<string>(type: "jsonb", nullable: true),
                    new_values_json = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "broker_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    broker_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    encrypted_api_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    encrypted_api_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    encrypted_access_token = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    encrypted_refresh_token = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    token_expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_authenticated = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    trading_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    trading_mode = table.Column<int>(type: "integer", nullable: false),
                    last_authenticated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_broker_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_broker_connections_broker_providers_broker_provider_id",
                        column: x => x.broker_provider_id,
                        principalTable: "broker_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_broker_connections_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instrument_universes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_universes", x => x.id);
                    table.ForeignKey(
                        name: "fk_instrument_universes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strategy_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    strategy_type = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strategy_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_strategy_definitions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trading_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    broker_connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    account_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    account_type = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    trading_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    trading_mode = table.Column<int>(type: "integer", nullable: false),
                    available_funds = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    allocated_capital = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trading_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_trading_accounts_broker_connections_broker_connection_id",
                        column: x => x.broker_connection_id,
                        principalTable: "broker_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trading_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instrument_universe_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_universe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_universe_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_instrument_universe_items_instrument_universes_instrument_u",
                        column: x => x.instrument_universe_id,
                        principalTable: "instrument_universes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_instrument_universe_items_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "strategy_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    configuration_json = table.Column<string>(type: "jsonb", nullable: false),
                    stock_universe_json = table.Column<string>(type: "jsonb", nullable: true),
                    capital_allocation = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_positions = table.Column<int>(type: "integer", nullable: false),
                    risk_per_trade_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    stop_loss_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    target_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    activated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strategy_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_strategy_versions_strategy_definitions_strategy_definition_",
                        column: x => x.strategy_definition_id,
                        principalTable: "strategy_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_strategy_versions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_capital_allocation = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_order_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_positions = table.Column<int>(type: "integer", nullable: false),
                    max_risk_per_trade_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    daily_loss_limit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_exposure_per_stock_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    max_trades_per_day = table.Column<int>(type: "integer", nullable: false),
                    stop_loss_required = table.Column<bool>(type: "boolean", nullable: false),
                    approval_duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    auto_execute_on_timeout = table.Column<bool>(type: "boolean", nullable: false),
                    kill_switch_active = table.Column<bool>(type: "boolean", nullable: false),
                    trading_schedule_start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    trading_schedule_end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    intraday_square_off_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_account_configurations_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_account_configurations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    report_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivered_via_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_daily_reports_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_daily_reports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    data_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    max_capital = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_daily_loss = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_position_size = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_risk_profiles_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_risk_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    side = table.Column<int>(type: "integer", nullable: false),
                    product_type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    average_buy_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    stop_loss = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    target = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    realized_gross_pn_l = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    realized_net_pn_l = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_charges = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_open = table.Column<bool>(type: "boolean", nullable: false),
                    opened_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exit_reason = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_positions_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_positions_strategy_versions_strategy_version_id",
                        column: x => x.strategy_version_id,
                        principalTable: "strategy_versions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_positions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strategy_account_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strategy_account_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_strategy_account_mappings_strategy_versions_strategy_versio",
                        column: x => x.strategy_version_id,
                        principalTable: "strategy_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_strategy_account_mappings_trading_accounts_trading_account_",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_strategy_account_mappings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trading_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_type = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    stop_loss = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    target = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    indicator_values_json = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trading_signals", x => x.id);
                    table.ForeignKey(
                        name: "fk_trading_signals_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trading_signals_strategy_versions_strategy_version_id",
                        column: x => x.strategy_version_id,
                        principalTable: "strategy_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trading_signals_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trading_signals_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pending_trade_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_signal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    company_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    indicative_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    stop_loss = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    target = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    risk_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    approval_deadline_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    decided_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_reason = table.Column<string>(type: "text", nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_trade_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_pending_trade_approvals_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pending_trade_approvals_trading_signals_trading_signal_id",
                        column: x => x.trading_signal_id,
                        principalTable: "trading_signals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_pending_trade_approvals_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trading_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pending_trade_approval_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    broker_order_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    side = table.Column<int>(type: "integer", nullable: false),
                    order_type = table.Column<int>(type: "integer", nullable: false),
                    product_type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    filled_quantity = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    trigger_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    average_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    status_message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trading_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_trading_orders_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trading_orders_pending_trade_approvals_pending_trade_approv",
                        column: x => x.pending_trade_approval_id,
                        principalTable: "pending_trade_approvals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_trading_orders_strategy_versions_strategy_version_id",
                        column: x => x.strategy_version_id,
                        principalTable: "strategy_versions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_trading_orders_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trading_orders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: false),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    broker_response_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_events_trading_orders_trading_order_id",
                        column: x => x.trading_order_id,
                        principalTable: "trading_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trade_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    broker_trade_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    side = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    exchange = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "NSE"),
                    executed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trade_executions", x => x.id);
                    table.ForeignKey(
                        name: "fk_trade_executions_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trade_executions_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trade_executions_trading_orders_trading_order_id",
                        column: x => x.trading_order_id,
                        principalTable: "trading_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trade_executions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "broker_providers",
                columns: new[] { "id", "api_base_url", "authorization_url", "capabilities_json", "code", "created_at_utc", "is_active", "name", "token_url", "updated_at_utc", "web_socket_url" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "http://localhost:5000/mock-broker", null, null, 1, new DateTime(2026, 11, 11, 8, 24, 16, 19, DateTimeKind.Utc).AddTicks(4380), true, "Mock Simulator", null, null, null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "https://api.upstox.com/v2", "https://api.upstox.com/v2/login/authorization/dialog", null, 2, new DateTime(2026, 11, 11, 8, 24, 16, 20, DateTimeKind.Utc).AddTicks(1976), true, "Upstox", "https://api.upstox.com/v2/login/authorization/token", null, "wss://ws-feed.upstox.com/v2/feed" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "https://api.kite.trade", "https://kite.zerodha.com/connect/login", null, 3, new DateTime(2026, 11, 11, 8, 24, 16, 20, DateTimeKind.Utc).AddTicks(5610), true, "Zerodha", "https://api.kite.trade/session/token", null, "wss://ws.kite.trade" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at_utc", "description", "name", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 11, 11, 8, 24, 16, 97, DateTimeKind.Utc).AddTicks(2128), "Platform Administrator", "Admin", null },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2026, 11, 11, 8, 24, 16, 97, DateTimeKind.Utc).AddTicks(5319), "Standard Trading User", "User", null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_configurations_trading_account_id",
                table: "account_configurations",
                column: "trading_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_account_configurations_user_id",
                table: "account_configurations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id_timestamp_utc",
                table: "audit_logs",
                columns: new[] { "user_id", "timestamp_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_broker_connections_broker_provider_id",
                table: "broker_connections",
                column: "broker_provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_broker_connections_user_id_broker_provider_id",
                table: "broker_connections",
                columns: new[] { "user_id", "broker_provider_id" });

            migrationBuilder.CreateIndex(
                name: "ix_broker_providers_code",
                table: "broker_providers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_daily_reports_trading_account_id",
                table: "daily_reports",
                column: "trading_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_daily_reports_user_id_report_date",
                table: "daily_reports",
                columns: new[] { "user_id", "report_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instrument_universe_items_instrument_id",
                table: "instrument_universe_items",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_universe_items_instrument_universe_id_instrument",
                table: "instrument_universe_items",
                columns: new[] { "instrument_universe_id", "instrument_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instrument_universes_user_id_name",
                table: "instrument_universes",
                columns: new[] { "user_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_instruments_exchange_trading_symbol",
                table: "instruments",
                columns: new[] { "exchange", "trading_symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instruments_instrument_token",
                table: "instruments",
                column: "instrument_token");

            migrationBuilder.CreateIndex(
                name: "ix_market_candles_instrument_id_timeframe_timestamp_utc",
                table: "market_candles",
                columns: new[] { "instrument_id", "timeframe", "timestamp_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_notification_id_channel",
                table: "notification_deliveries",
                columns: new[] { "notification_id", "channel" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_trading_account_id",
                table: "notifications",
                column: "trading_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_is_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_order_events_trading_order_id",
                table: "order_events",
                column: "trading_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_pending_trade_approvals_idempotency_key",
                table: "pending_trade_approvals",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pending_trade_approvals_status_approval_deadline_utc",
                table: "pending_trade_approvals",
                columns: new[] { "status", "approval_deadline_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_pending_trade_approvals_trading_account_id",
                table: "pending_trade_approvals",
                column: "trading_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_pending_trade_approvals_trading_signal_id",
                table: "pending_trade_approvals",
                column: "trading_signal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pending_trade_approvals_user_id",
                table: "pending_trade_approvals",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_instrument_id",
                table: "positions",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_strategy_version_id",
                table: "positions",
                column: "strategy_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_trading_account_id_instrument_id_is_open",
                table: "positions",
                columns: new[] { "trading_account_id", "instrument_id", "is_open" });

            migrationBuilder.CreateIndex(
                name: "ix_positions_trading_account_id_is_open",
                table: "positions",
                columns: new[] { "trading_account_id", "is_open" });

            migrationBuilder.CreateIndex(
                name: "ix_positions_user_id",
                table: "positions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_profiles_trading_account_id",
                table: "risk_profiles",
                column: "trading_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_profiles_user_id",
                table: "risk_profiles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_strategy_account_mappings_strategy_version_id_trading_accou",
                table: "strategy_account_mappings",
                columns: new[] { "strategy_version_id", "trading_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_strategy_account_mappings_trading_account_id",
                table: "strategy_account_mappings",
                column: "trading_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_account_mappings_user_id",
                table: "strategy_account_mappings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_strategy_definitions_user_id_name",
                table: "strategy_definitions",
                columns: new[] { "user_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_strategy_versions_strategy_definition_id_version_number",
                table: "strategy_versions",
                columns: new[] { "strategy_definition_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_strategy_versions_user_id",
                table: "strategy_versions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_configurations_key",
                table: "system_configurations",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trade_executions_instrument_id",
                table: "trade_executions",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_executions_trading_account_id",
                table: "trade_executions",
                column: "trading_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_executions_trading_order_id",
                table: "trade_executions",
                column: "trading_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_trade_executions_user_id",
                table: "trade_executions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_accounts_broker_connection_id_account_id",
                table: "trading_accounts",
                columns: new[] { "broker_connection_id", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trading_accounts_user_id",
                table: "trading_accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_orders_broker_order_id",
                table: "trading_orders",
                column: "broker_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_orders_idempotency_key",
                table: "trading_orders",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trading_orders_instrument_id",
                table: "trading_orders",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_orders_pending_trade_approval_id",
                table: "trading_orders",
                column: "pending_trade_approval_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_orders_strategy_version_id",
                table: "trading_orders",
                column: "strategy_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_orders_trading_account_id_status",
                table: "trading_orders",
                columns: new[] { "trading_account_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_trading_orders_user_id",
                table: "trading_orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_signals_instrument_id",
                table: "trading_signals",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_signals_strategy_version_id",
                table: "trading_signals",
                column: "strategy_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_trading_signals_trading_account_id_status",
                table: "trading_signals",
                columns: new[] { "trading_account_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_trading_signals_user_id",
                table: "trading_signals",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_worker_heartbeats_worker_name",
                table: "worker_heartbeats",
                column: "worker_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_configurations");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "daily_reports");

            migrationBuilder.DropTable(
                name: "instrument_universe_items");

            migrationBuilder.DropTable(
                name: "market_candles");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "order_events");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "risk_profiles");

            migrationBuilder.DropTable(
                name: "strategy_account_mappings");

            migrationBuilder.DropTable(
                name: "system_configurations");

            migrationBuilder.DropTable(
                name: "trade_executions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "worker_heartbeats");

            migrationBuilder.DropTable(
                name: "instrument_universes");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "trading_orders");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "pending_trade_approvals");

            migrationBuilder.DropTable(
                name: "trading_signals");

            migrationBuilder.DropTable(
                name: "instruments");

            migrationBuilder.DropTable(
                name: "strategy_versions");

            migrationBuilder.DropTable(
                name: "trading_accounts");

            migrationBuilder.DropTable(
                name: "strategy_definitions");

            migrationBuilder.DropTable(
                name: "broker_connections");

            migrationBuilder.DropTable(
                name: "broker_providers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

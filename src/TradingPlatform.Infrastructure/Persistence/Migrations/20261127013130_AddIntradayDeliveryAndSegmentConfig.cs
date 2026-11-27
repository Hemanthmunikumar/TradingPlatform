using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntradayDeliveryAndSegmentConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "allowed_trading_segment",
                table: "account_configurations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Both");

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_daily_loss_limit",
                table: "account_configurations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_max_capital_allocation",
                table: "account_configurations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_max_order_value",
                table: "account_configurations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "delivery_max_positions",
                table: "account_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "delivery_min_volume",
                table: "account_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_stop_loss_percent",
                table: "account_configurations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_target_percent",
                table: "account_configurations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "intraday_daily_loss_limit",
                table: "account_configurations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "intraday_max_capital_allocation",
                table: "account_configurations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "intraday_max_order_value",
                table: "account_configurations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "intraday_max_positions",
                table: "account_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "intraday_min_volume",
                table: "account_configurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "intraday_stop_loss_percent",
                table: "account_configurations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "intraday_target_percent",
                table: "account_configurations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 27, 1, 31, 20, 568, DateTimeKind.Utc).AddTicks(9161));

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 27, 1, 31, 20, 570, DateTimeKind.Utc).AddTicks(2576));

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 27, 1, 31, 20, 570, DateTimeKind.Utc).AddTicks(7830));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 27, 1, 31, 20, 673, DateTimeKind.Utc).AddTicks(8341));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 27, 1, 31, 20, 674, DateTimeKind.Utc).AddTicks(2719));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allowed_trading_segment",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "delivery_daily_loss_limit",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "delivery_max_capital_allocation",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "delivery_max_order_value",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "delivery_max_positions",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "delivery_min_volume",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "delivery_stop_loss_percent",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "delivery_target_percent",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "intraday_daily_loss_limit",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "intraday_max_capital_allocation",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "intraday_max_order_value",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "intraday_max_positions",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "intraday_min_volume",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "intraday_stop_loss_percent",
                table: "account_configurations");

            migrationBuilder.DropColumn(
                name: "intraday_target_percent",
                table: "account_configurations");

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 11, 8, 24, 16, 19, DateTimeKind.Utc).AddTicks(4380));

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 11, 8, 24, 16, 20, DateTimeKind.Utc).AddTicks(1976));

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 11, 8, 24, 16, 20, DateTimeKind.Utc).AddTicks(5610));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 11, 8, 24, 16, 97, DateTimeKind.Utc).AddTicks(2128));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 11, 8, 24, 16, 97, DateTimeKind.Utc).AddTicks(5319));
        }
    }
}

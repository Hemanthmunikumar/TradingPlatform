using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTradingModeIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "trading_mode",
                table: "trading_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "trading_mode",
                table: "trade_executions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "trading_mode",
                table: "positions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "trading_mode",
                table: "pending_trade_approvals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 28, 2, 13, 31, 16, DateTimeKind.Utc).AddTicks(6446));

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 28, 2, 13, 31, 17, DateTimeKind.Utc).AddTicks(6045));

            migrationBuilder.UpdateData(
                table: "broker_providers",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 28, 2, 13, 31, 18, DateTimeKind.Utc).AddTicks(1589));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 28, 2, 13, 31, 186, DateTimeKind.Utc).AddTicks(103));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "created_at_utc",
                value: new DateTime(2026, 11, 28, 2, 13, 31, 186, DateTimeKind.Utc).AddTicks(3408));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trading_mode",
                table: "trading_orders");

            migrationBuilder.DropColumn(
                name: "trading_mode",
                table: "trade_executions");

            migrationBuilder.DropColumn(
                name: "trading_mode",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "trading_mode",
                table: "pending_trade_approvals");

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
    }
}

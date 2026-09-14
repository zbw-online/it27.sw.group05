using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Open");

            _ = migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAtUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            // Safe, deterministic initial status for orders that existed before the status concept
            // was introduced - based only on their own already-persisted IsInventoryApplied flag,
            // never on the date the migration happens to run. An order whose inventory effect was
            // already applied is treated as fulfilled (Completed); every other existing order is
            // treated as still Open, matching the "no direct Open -> Completed shortcut" rule for
            // orders going forward.
            _ = migrationBuilder.Sql(
                """
                UPDATE [Orders] SET
                    [Status] = CASE WHEN [IsInventoryApplied] = 1 THEN N'Completed' ELSE N'Open' END,
                    [StatusChangedAtUtc] = CASE WHEN [IsInventoryApplied] = 1 THEN [OrderDate] ELSE NULL END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "StatusChangedAtUtc",
                table: "Orders");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersTemporal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    DeliveryStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeliveryHouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeliveryPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeliveryCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeliveryCountryCode = table.Column<string>(type: "nchar(2)", nullable: false),
                    RowValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    RowValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCurrency = table.Column<string>(type: "nchar(3)", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Orders", x => x.OrderId);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrdersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");

            _ = migrationBuilder.CreateTable(
                name: "OrderLines",
                columns: table => new
                {
                    OrderLineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    ArticleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPriceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPriceCurrency = table.Column<string>(type: "nchar(3)", nullable: false),
                    RowValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    RowValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    LineTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotalCurrency = table.Column<string>(type: "nchar(3)", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_OrderLines", x => x.OrderLineId);
                    _ = table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrderLinesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");

            _ = migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                table: "OrderLines",
                column: "OrderId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId_LineNumber",
                table: "OrderLines",
                columns: ["OrderId", "LineNumber"],
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "OrderLines")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrderLinesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");

            _ = migrationBuilder.DropTable(
                name: "Orders")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrdersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");
        }
    }
}

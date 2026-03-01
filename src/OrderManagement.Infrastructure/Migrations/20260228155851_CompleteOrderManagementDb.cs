using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteOrderManagementDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.RenameColumn(
                name: "Id",
                table: "Customers",
                newName: "CustomerId");

            _ = migrationBuilder.RenameColumn(
                name: "Id",
                table: "CustomerAddresses",
                newName: "CustomerAddressId");

            _ = migrationBuilder.RenameColumn(
                name: "Id",
                table: "Articles",
                newName: "ArticleId");

            _ = migrationBuilder.RenameColumn(
                name: "Id",
                table: "ArticleGroups",
                newName: "ArticleGroupId");

            _ = migrationBuilder.AlterTable(
                name: "CustomerAddresses")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CustomerAddressesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "CustomerAddressHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");

            _ = migrationBuilder.AlterTable(
                name: "Articles")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ArticlesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");

            _ = migrationBuilder.AlterColumn<string>(
                name: "PriceCurrency",
                table: "Articles",
                type: "nchar(3)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            _ = migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            _ = migrationBuilder.AddColumn<DateTime>(
                name: "RowValidFrom",
                table: "Articles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("SqlServer:TemporalIsPeriodStartColumn", true);

            _ = migrationBuilder.AddColumn<DateTime>(
                name: "RowValidUntil",
                table: "Articles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("SqlServer:TemporalIsPeriodEndColumn", true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ArticleId",
                table: "OrderLines",
                column: "ArticleId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Articles_Name",
                table: "Articles",
                column: "Name");

            _ = migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_Articles_ArticleId",
                table: "OrderLines",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "ArticleId",
                onDelete: ReferentialAction.Restrict);

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_Articles_ArticleId",
                table: "OrderLines");

            _ = migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            _ = migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders");

            _ = migrationBuilder.DropIndex(
                name: "IX_OrderLines_ArticleId",
                table: "OrderLines");

            _ = migrationBuilder.DropIndex(
                name: "IX_Articles_Name",
                table: "Articles");

            _ = migrationBuilder.DropColumn(
                name: "RowValidFrom",
                table: "Articles")
                .Annotation("SqlServer:TemporalIsPeriodStartColumn", true);

            _ = migrationBuilder.DropColumn(
                name: "RowValidUntil",
                table: "Articles")
                .Annotation("SqlServer:TemporalIsPeriodEndColumn", true);

            _ = migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Customers",
                newName: "Id");

            _ = migrationBuilder.RenameColumn(
                name: "CustomerAddressId",
                table: "CustomerAddresses",
                newName: "Id");

            _ = migrationBuilder.RenameColumn(
                name: "ArticleId",
                table: "Articles",
                newName: "Id");

            _ = migrationBuilder.RenameColumn(
                name: "ArticleGroupId",
                table: "ArticleGroups",
                newName: "Id");

            _ = migrationBuilder.AlterTable(
                name: "CustomerAddresses")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CustomerAddressHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "CustomerAddressesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");

            _ = migrationBuilder.AlterTable(
                name: "Articles")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "ArticlesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");

            _ = migrationBuilder.AlterColumn<string>(
                name: "PriceCurrency",
                table: "Articles",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nchar(3)");

            _ = migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Articles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}

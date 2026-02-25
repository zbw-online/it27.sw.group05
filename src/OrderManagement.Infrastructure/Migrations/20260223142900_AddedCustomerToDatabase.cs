using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedCustomerToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.AlterTable(
                name: "ArticleGroups")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ArticleGroupsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");

            _ = migrationBuilder.AddColumn<DateTime>(
                name: "RowValidFrom",
                table: "ArticleGroups",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("SqlServer:TemporalIsPeriodStartColumn", true);

            _ = migrationBuilder.AddColumn<DateTime>(
                name: "RowValidUntil",
                table: "ArticleGroups",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("SqlServer:TemporalIsPeriodEndColumn", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropColumn(
                name: "RowValidFrom",
                table: "ArticleGroups")
                .Annotation("SqlServer:TemporalIsPeriodStartColumn", true);

            _ = migrationBuilder.DropColumn(
                name: "RowValidUntil",
                table: "ArticleGroups")
                .Annotation("SqlServer:TemporalIsPeriodEndColumn", true);

            _ = migrationBuilder.AlterTable(
                name: "ArticleGroups")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "ArticleGroupsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "RowValidUntil")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "RowValidFrom");
        }
    }
}

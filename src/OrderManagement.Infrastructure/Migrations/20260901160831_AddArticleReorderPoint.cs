using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleReorderPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => _ = migrationBuilder.AddColumn<int>(
                name: "ReorderPoint",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 20);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => _ = migrationBuilder.DropColumn(
                name: "ReorderPoint",
                table: "Articles");
    }
}

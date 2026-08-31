using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryDateAndBillingAddressToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.AddColumn<string>(
                name: "BillingAddressSource",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            _ = migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            _ = migrationBuilder.AddColumn<string>(
                name: "BillingCountryCode",
                table: "Orders",
                type: "nchar(2)",
                nullable: false,
                defaultValue: "");

            _ = migrationBuilder.AddColumn<string>(
                name: "BillingHouseNumber",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            _ = migrationBuilder.AddColumn<string>(
                name: "BillingPostalCode",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            _ = migrationBuilder.AddColumn<string>(
                name: "BillingStreet",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            _ = migrationBuilder.AddColumn<string>(
                name: "CustomerReference",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            _ = migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressSource",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            _ = migrationBuilder.AddColumn<DateOnly>(
                name: "DeliveryDate",
                table: "Orders",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            _ = migrationBuilder.Sql(
                """
                UPDATE [Orders] SET
                    [BillingStreet] = [DeliveryStreet],
                    [BillingHouseNumber] = [DeliveryHouseNumber],
                    [BillingPostalCode] = [DeliveryPostalCode],
                    [BillingCity] = [DeliveryCity],
                    [BillingCountryCode] = [DeliveryCountryCode],
                    [BillingAddressSource] = N'Automatic',
                    [DeliveryAddressSource] = N'Automatic',
                    [DeliveryDate] = CAST([OrderDate] AS date);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropColumn(
                name: "BillingAddressSource",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "BillingCity",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "BillingCountryCode",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "BillingHouseNumber",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "BillingPostalCode",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "BillingStreet",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "CustomerReference",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "DeliveryAddressSource",
                table: "Orders");

            _ = migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "Orders");
        }
    }
}

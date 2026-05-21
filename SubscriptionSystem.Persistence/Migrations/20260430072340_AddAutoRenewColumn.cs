using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoRenewColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "Subscriptions");
        }
    }
}

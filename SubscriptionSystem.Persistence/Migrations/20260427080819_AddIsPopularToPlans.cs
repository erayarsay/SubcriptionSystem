using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPopularToPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPopular",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPopular",
                table: "Plans");
        }
    }
}

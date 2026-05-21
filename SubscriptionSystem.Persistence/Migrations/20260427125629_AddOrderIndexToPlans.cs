using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIndexToPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SubTitle",
                table: "Plans",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "SubTitle",
                table: "Plans");
        }
    }
}

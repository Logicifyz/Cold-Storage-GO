using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cold_Storage_GO.Migrations
{
    /// <inheritdoc />
    public partial class RemovedMealTypeandStuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealType",
                table: "UserRecipeRequests");

            migrationBuilder.DropColumn(
                name: "AvoidCookingMethods",
                table: "AIRecipeRequests");

            migrationBuilder.DropColumn(
                name: "MealType",
                table: "AIRecipeRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MealType",
                table: "UserRecipeRequests",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "AvoidCookingMethods",
                table: "AIRecipeRequests",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "MealType",
                table: "AIRecipeRequests",
                type: "longtext",
                nullable: false);
        }
    }
}

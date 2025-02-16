using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Cold_Storage_GO.Migrations
{
    /// <inheritdoc />
    public partial class profileidguid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRecommendations");

            migrationBuilder.DropColumn(
                name: "Ingredients",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Recipes");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProfileId",
                table: "UserProfiles",
                type: "char(36)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Discussions",
                type: "varchar(5000)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateTable(
                name: "AIRecipeRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Ingredients = table.Column<string>(type: "longtext", nullable: false),
                    ExcludeIngredients = table.Column<string>(type: "longtext", nullable: false),
                    DietaryPreferences = table.Column<string>(type: "longtext", nullable: false),
                    MaxIngredients = table.Column<int>(type: "int", nullable: true),
                    Preference = table.Column<string>(type: "longtext", nullable: false),
                    FreeText = table.Column<string>(type: "longtext", nullable: false),
                    UseChat = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CookingTime = table.Column<string>(type: "longtext", nullable: false),
                    Servings = table.Column<int>(type: "int", nullable: true),
                    NeedsClarification = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TrendingRequest = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRecipeRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_AIRecipeRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DiscussionImages",
                columns: table => new
                {
                    ImageId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ImageData = table.Column<byte[]>(type: "longblob", nullable: false),
                    DiscussionId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_DiscussionImages_Discussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalTable: "Discussions",
                        principalColumn: "DiscussionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FinalDishes",
                columns: table => new
                {
                    DishId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId1 = table.Column<Guid>(type: "char(36)", nullable: true),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false),
                    Ingredients = table.Column<string>(type: "longtext", nullable: false),
                    Steps = table.Column<string>(type: "longtext", nullable: false),
                    Nutrition_Calories = table.Column<int>(type: "int", nullable: false),
                    Nutrition_Protein = table.Column<int>(type: "int", nullable: false),
                    Nutrition_Carbs = table.Column<int>(type: "int", nullable: false),
                    Nutrition_Fats = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "longtext", nullable: false),
                    Servings = table.Column<int>(type: "int", nullable: false),
                    CookingTime = table.Column<string>(type: "longtext", nullable: false),
                    Difficulty = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalDishes", x => x.DishId);
                    table.ForeignKey(
                        name: "FK_FinalDishes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalDishes_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "UserId");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecipeImages",
                columns: table => new
                {
                    ImageId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ImageData = table.Column<byte[]>(type: "longblob", nullable: false),
                    RecipeId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_RecipeImages_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    IngredientId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Quantity = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    RecipeId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.IngredientId);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecipeInstructions",
                columns: table => new
                {
                    InstructionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    StepImage = table.Column<byte[]>(type: "longblob", nullable: true),
                    RecipeId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeInstructions", x => x.InstructionId);
                    table.ForeignKey(
                        name: "FK_RecipeInstructions_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserRecipeRequests",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    FreeText = table.Column<string>(type: "longtext", nullable: false),
                    MaxIngredients = table.Column<int>(type: "int", nullable: true),
                    Cuisine = table.Column<string>(type: "longtext", nullable: false),
                    CookingTime = table.Column<string>(type: "longtext", nullable: false),
                    Servings = table.Column<int>(type: "int", nullable: true),
                    NeedsClarification = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TrendingRequest = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AIResponseLogs",
                columns: table => new
                {
                    ChatId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false),
                    UserResponse = table.Column<string>(type: "longtext", nullable: true),
                    FinalRecipeId = table.Column<Guid>(type: "char(36)", nullable: true),
                    NeedsFinalDish = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIResponseLogs", x => x.ChatId);
                    table.ForeignKey(
                        name: "FK_AIResponseLogs_FinalDishes_FinalRecipeId",
                        column: x => x.FinalRecipeId,
                        principalTable: "FinalDishes",
                        principalColumn: "DishId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AIResponseLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AIRecipeRequests_UserId",
                table: "AIRecipeRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIResponseLogs_FinalRecipeId",
                table: "AIResponseLogs",
                column: "FinalRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_AIResponseLogs_UserId",
                table: "AIResponseLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionImages_DiscussionId",
                table: "DiscussionImages",
                column: "DiscussionId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalDishes_UserId",
                table: "FinalDishes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalDishes_UserId1",
                table: "FinalDishes",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeImages_RecipeId",
                table: "RecipeImages",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeInstructions_RecipeId",
                table: "RecipeInstructions",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRecipeRequests");

            migrationBuilder.DropTable(
                name: "AIResponseLogs");

            migrationBuilder.DropTable(
                name: "DiscussionImages");

            migrationBuilder.DropTable(
                name: "RecipeImages");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "RecipeInstructions");

            migrationBuilder.DropTable(
                name: "UserRecipeRequests");

            migrationBuilder.DropTable(
                name: "FinalDishes");

            migrationBuilder.AlterColumn<int>(
                name: "ProfileId",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<string>(
                name: "Ingredients",
                table: "Recipes",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Recipes",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Recipes",
                type: "longtext",
                nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Discussions",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5000)",
                oldMaxLength: 5000);

            migrationBuilder.CreateTable(
                name: "AIRecommendations",
                columns: table => new
                {
                    ChatId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRecommendations", x => x.ChatId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }
    }
}

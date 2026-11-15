using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace web_ca_3.Data.Migrations;

public partial class AddDomainModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Ingredients",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 120, nullable: false),
                Unit = table.Column<string>(maxLength: 20, nullable: false),
                UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                CurrentStock = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Ingredients", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MenuDishes",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 120, nullable: false),
                Category = table.Column<string>(maxLength: 40, nullable: false),
                Calories = table.Column<int>(nullable: false),
                IsActive = table.Column<bool>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MenuDishes", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AttendanceRecords",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ServiceDate = table.Column<DateTime>(nullable: false),
                Meal = table.Column<int>(nullable: false),
                ExpectedCount = table.Column<int>(nullable: false),
                ActualCount = table.Column<int>(nullable: false),
                Notes = table.Column<string>(maxLength: 200, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AttendanceRecords", x => x.Id));

        migrationBuilder.CreateTable(
            name: "CalendarEvents",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(maxLength: 160, nullable: false),
                EventDate = table.Column<DateTime>(nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                AttendanceAdjustment = table.Column<int>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CalendarEvents", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ForecastRecords",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ForecastDate = table.Column<DateTime>(nullable: false),
                Meal = table.Column<int>(nullable: false),
                ForecastCount = table.Column<int>(nullable: false),
                Confidence = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                Method = table.Column<string>(maxLength: 300, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ForecastRecords", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ServiceActuals",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ServiceDate = table.Column<DateTime>(nullable: false),
                Meal = table.Column<int>(nullable: false),
                PreparedCount = table.Column<int>(nullable: false),
                ServedCount = table.Column<int>(nullable: false),
                LeftoverCount = table.Column<int>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ServiceActuals", x => x.Id));

        migrationBuilder.CreateTable(
            name: "RecipeIngredients",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                MenuDishId = table.Column<int>(nullable: false),
                IngredientId = table.Column<int>(nullable: false),
                QuantityPerServing = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                table.ForeignKey("FK_RecipeIngredients_MenuDishes_MenuDishId", x => x.MenuDishId, "MenuDishes", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_RecipeIngredients_Ingredients_IngredientId", x => x.IngredientId, "Ingredients", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WasteRecords",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                WasteDate = table.Column<DateTime>(nullable: false),
                IngredientId = table.Column<int>(nullable: true),
                MenuDishId = table.Column<int>(nullable: true),
                Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Reason = table.Column<int>(nullable: false),
                Notes = table.Column<string>(maxLength: 300, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WasteRecords", x => x.Id);
                table.ForeignKey("FK_WasteRecords_Ingredients_IngredientId", x => x.IngredientId, "Ingredients", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_WasteRecords_MenuDishes_MenuDishId", x => x.MenuDishId, "MenuDishes", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_Ingredients_Name", "Ingredients", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_MenuDishes_Name", "MenuDishes", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_AttendanceRecords_ServiceDate_Meal", "AttendanceRecords", new[] { "ServiceDate", "Meal" }, unique: true);
        migrationBuilder.CreateIndex("IX_CalendarEvents_EventDate", "CalendarEvents", "EventDate");
        migrationBuilder.CreateIndex("IX_ForecastRecords_ForecastDate_Meal", "ForecastRecords", new[] { "ForecastDate", "Meal" }, unique: true);
        migrationBuilder.CreateIndex("IX_ServiceActuals_ServiceDate_Meal", "ServiceActuals", new[] { "ServiceDate", "Meal" }, unique: true);
        migrationBuilder.CreateIndex("IX_RecipeIngredients_MenuDishId_IngredientId", "RecipeIngredients", new[] { "MenuDishId", "IngredientId" }, unique: true);
        migrationBuilder.CreateIndex("IX_RecipeIngredients_IngredientId", "RecipeIngredients", "IngredientId");
        migrationBuilder.CreateIndex("IX_WasteRecords_IngredientId", "WasteRecords", "IngredientId");
        migrationBuilder.CreateIndex("IX_WasteRecords_MenuDishId", "WasteRecords", "MenuDishId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("WasteRecords");
        migrationBuilder.DropTable("RecipeIngredients");
        migrationBuilder.DropTable("ServiceActuals");
        migrationBuilder.DropTable("ForecastRecords");
        migrationBuilder.DropTable("CalendarEvents");
        migrationBuilder.DropTable("AttendanceRecords");
        migrationBuilder.DropTable("MenuDishes");
        migrationBuilder.DropTable("Ingredients");
    }
}

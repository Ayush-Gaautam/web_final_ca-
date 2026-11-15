using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_ca_3.Data.Migrations;

public partial class AddPlanningHallAnalyticsSqlServer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Halls",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Capacity = table.Column<int>(nullable: false),
                IsActive = table.Column<bool>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Halls", x => x.Id));

        migrationBuilder.AddColumn<int>(
            name: "HallId", table: "AttendanceRecords", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "HallId", table: "ForecastRecords", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "HallId", table: "ServiceActuals", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "Meal", table: "WasteRecords", nullable: true);

        migrationBuilder.CreateTable(
            name: "Feedback",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                StudentEmail = table.Column<string>(maxLength: 256, nullable: false),
                SubmittedAt = table.Column<DateTime>(nullable: false),
                Rating = table.Column<int>(nullable: false),
                Comment = table.Column<string>(maxLength: 1000, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Feedback", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MealBookings",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                StudentEmail = table.Column<string>(maxLength: 256, nullable: false),
                BookingDate = table.Column<DateTime>(nullable: false),
                Meal = table.Column<int>(nullable: false),
                HallId = table.Column<int>(nullable: true),
                Status = table.Column<int>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MealBookings", x => x.Id);
                table.ForeignKey("FK_MealBookings_Halls_HallId", x => x.HallId, "Halls", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "PreparationPlans",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                PlanDate = table.Column<DateTime>(nullable: false),
                Meal = table.Column<int>(nullable: false),
                HallId = table.Column<int>(nullable: true),
                PredictedServings = table.Column<int>(nullable: false),
                SafetyMarginPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PreparationPlans", x => x.Id);
                table.ForeignKey("FK_PreparationPlans_Halls_HallId", x => x.HallId, "Halls", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "PreparationPlanLines",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                PreparationPlanId = table.Column<int>(nullable: false),
                IngredientId = table.Column<int>(nullable: false),
                RequiredQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                AvailableQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                ShortageQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PreparationPlanLines", x => x.Id);
                table.ForeignKey("FK_PreparationPlanLines_PreparationPlans_PreparationPlanId", x => x.PreparationPlanId, "PreparationPlans", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_PreparationPlanLines_Ingredients_IngredientId", x => x.IngredientId, "Ingredients", "Id", onDelete: ReferentialAction.Cascade);
            });

        foreach (var table in new[] { "AttendanceRecords", "ForecastRecords", "ServiceActuals" })
            migrationBuilder.AddForeignKey(
                name: $"FK_{table}_Halls_HallId", table: table, column: "HallId",
                principalTable: "Halls", principalColumn: "Id", onDelete: ReferentialAction.SetNull);

        migrationBuilder.CreateIndex("IX_Halls_Name", "Halls", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_AttendanceRecords_ServiceDate_Meal_HallId", "AttendanceRecords", new[] { "ServiceDate", "Meal", "HallId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ForecastRecords_ForecastDate_Meal_HallId", "ForecastRecords", new[] { "ForecastDate", "Meal", "HallId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ServiceActuals_ServiceDate_Meal_HallId", "ServiceActuals", new[] { "ServiceDate", "Meal", "HallId" }, unique: true);
        migrationBuilder.CreateIndex("IX_MealBookings_StudentEmail_BookingDate_Meal", "MealBookings", new[] { "StudentEmail", "BookingDate", "Meal" }, unique: true);
        migrationBuilder.CreateIndex("IX_PreparationPlans_PlanDate_Meal_HallId", "PreparationPlans", new[] { "PlanDate", "Meal", "HallId" }, unique: true);
        migrationBuilder.CreateIndex("IX_PreparationPlanLines_PreparationPlanId", "PreparationPlanLines", "PreparationPlanId");
        migrationBuilder.CreateIndex("IX_PreparationPlanLines_IngredientId", "PreparationPlanLines", "IngredientId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PreparationPlanLines");
        migrationBuilder.DropTable("PreparationPlans");
        migrationBuilder.DropTable("MealBookings");
        migrationBuilder.DropTable("Feedback");
        migrationBuilder.DropForeignKey("FK_ServiceActuals_Halls_HallId", "ServiceActuals");
        migrationBuilder.DropForeignKey("FK_ForecastRecords_Halls_HallId", "ForecastRecords");
        migrationBuilder.DropForeignKey("FK_AttendanceRecords_Halls_HallId", "AttendanceRecords");
        migrationBuilder.DropColumn("HallId", "AttendanceRecords");
        migrationBuilder.DropColumn("HallId", "ForecastRecords");
        migrationBuilder.DropColumn("HallId", "ServiceActuals");
        migrationBuilder.DropColumn("Meal", "WasteRecords");
        migrationBuilder.DropTable("Halls");
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Models;

namespace web_ca_3.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (db.Database.IsSqlServer())
            await db.Database.MigrateAsync();
        else
        {
            await db.Database.EnsureCreatedAsync();
            await EnsureSqliteDomainSchemaAsync(db);
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_Ingredients_Name ON Ingredients(Name);");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_MenuDishes_Name ON MenuDishes(Name);");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_AttendanceRecords_ServiceDate_Meal ON AttendanceRecords(ServiceDate, Meal);");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_ServiceActuals_ServiceDate_Meal ON ServiceActuals(ServiceDate, Meal);");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_ForecastRecords_ForecastDate_Meal ON ForecastRecords(ForecastDate, Meal);");
        }
        if (await db.MenuDishes.AnyAsync()) return;

        var rice = new Ingredient { Name = "Rice", Unit = "kg", UnitCost = 70, CurrentStock = 40 };
        var dal = new Ingredient { Name = "Dal", Unit = "kg", UnitCost = 120, CurrentStock = 25 };
        var oil = new Ingredient { Name = "Cooking oil", Unit = "litre", UnitCost = 140, CurrentStock = 18 };
        var rajma = new Ingredient { Name = "Rajma", Unit = "kg", UnitCost = 160, CurrentStock = 12 };
        db.Ingredients.AddRange(rice, dal, oil, rajma);
        var khichdi = new MenuDish { Name = "Vegetable Khichdi", Category = "Main", Calories = 420 };
        var rajmaRice = new MenuDish { Name = "Rajma Rice", Category = "Main", Calories = 520 };
        db.MenuDishes.AddRange(khichdi, rajmaRice);
        db.RecipeIngredients.AddRange(
            new RecipeIngredient { MenuDish = khichdi, Ingredient = rice, QuantityPerServing = .08m },
            new RecipeIngredient { MenuDish = khichdi, Ingredient = dal, QuantityPerServing = .04m },
            new RecipeIngredient { MenuDish = khichdi, Ingredient = oil, QuantityPerServing = .01m },
            new RecipeIngredient { MenuDish = rajmaRice, Ingredient = rice, QuantityPerServing = .09m },
            new RecipeIngredient { MenuDish = rajmaRice, Ingredient = rajma, QuantityPerServing = .05m });
        db.AttendanceRecords.AddRange(
            new AttendanceRecord { ServiceDate = DateTime.Today.AddDays(-1), Meal = MealType.Lunch, ExpectedCount = 100, ActualCount = 92 },
            new AttendanceRecord { ServiceDate = DateTime.Today.AddDays(-2), Meal = MealType.Lunch, ExpectedCount = 100, ActualCount = 96 },
            new AttendanceRecord { ServiceDate = DateTime.Today.AddDays(-3), Meal = MealType.Lunch, ExpectedCount = 100, ActualCount = 88 });
        await db.SaveChangesAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Manager", "Staff" })
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await userManager.FindByEmailAsync("admin@mess.local");
        if (admin is null)
        {
            admin = new IdentityUser { UserName = "admin@mess.local", Email = "admin@mess.local", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    private static async Task EnsureSqliteDomainSchemaAsync(ApplicationDbContext db)
        {
            var commands = new[]
            {
                """
                CREATE TABLE IF NOT EXISTS Ingredients (
                    Id INTEGER NOT NULL CONSTRAINT PK_Ingredients PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL, Unit TEXT NOT NULL, UnitCost decimal(18,2) NOT NULL, CurrentStock decimal(18,2) NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS MenuDishes (
                    Id INTEGER NOT NULL CONSTRAINT PK_MenuDishes PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL, Category TEXT NOT NULL, Calories INTEGER NOT NULL, IsActive INTEGER NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS AttendanceRecords (
                    Id INTEGER NOT NULL CONSTRAINT PK_AttendanceRecords PRIMARY KEY AUTOINCREMENT,
                    ServiceDate TEXT NOT NULL, Meal INTEGER NOT NULL, ExpectedCount INTEGER NOT NULL, ActualCount INTEGER NOT NULL, Notes TEXT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS CalendarEvents (
                    Id INTEGER NOT NULL CONSTRAINT PK_CalendarEvents PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL, EventDate TEXT NOT NULL, Description TEXT NULL, AttendanceAdjustment INTEGER NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS ForecastRecords (
                    Id INTEGER NOT NULL CONSTRAINT PK_ForecastRecords PRIMARY KEY AUTOINCREMENT,
                    ForecastDate TEXT NOT NULL, Meal INTEGER NOT NULL, ForecastCount INTEGER NOT NULL, Confidence decimal(5,2) NOT NULL, Method TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS ServiceActuals (
                    Id INTEGER NOT NULL CONSTRAINT PK_ServiceActuals PRIMARY KEY AUTOINCREMENT,
                    ServiceDate TEXT NOT NULL, Meal INTEGER NOT NULL, PreparedCount INTEGER NOT NULL, ServedCount INTEGER NOT NULL, LeftoverCount INTEGER NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS RecipeIngredients (
                    Id INTEGER NOT NULL CONSTRAINT PK_RecipeIngredients PRIMARY KEY AUTOINCREMENT,
                    MenuDishId INTEGER NOT NULL, IngredientId INTEGER NOT NULL, QuantityPerServing decimal(18,3) NOT NULL,
                    CONSTRAINT FK_RecipeIngredients_MenuDishes_MenuDishId FOREIGN KEY (MenuDishId) REFERENCES MenuDishes(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_RecipeIngredients_Ingredients_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredients(Id) ON DELETE CASCADE
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS WasteRecords (
                    Id INTEGER NOT NULL CONSTRAINT PK_WasteRecords PRIMARY KEY AUTOINCREMENT,
                    WasteDate TEXT NOT NULL, IngredientId INTEGER NULL, MenuDishId INTEGER NULL, Quantity decimal(18,3) NOT NULL,
                    UnitCost decimal(18,2) NOT NULL, Reason INTEGER NOT NULL, Notes TEXT NULL,
                    CONSTRAINT FK_WasteRecords_Ingredients_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredients(Id) ON DELETE SET NULL,
                    CONSTRAINT FK_WasteRecords_MenuDishes_MenuDishId FOREIGN KEY (MenuDishId) REFERENCES MenuDishes(Id) ON DELETE SET NULL
                );
                """
            };
            foreach (var command in commands)
                await db.Database.ExecuteSqlRawAsync(command);
        }
    }

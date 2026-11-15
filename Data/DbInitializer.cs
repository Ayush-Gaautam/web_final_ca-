using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Models;
using web_ca_3.Security;

namespace web_ca_3.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IHostEnvironment environment)
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
            await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS IX_AttendanceRecords_ServiceDate_Meal;");
            await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS IX_ServiceActuals_ServiceDate_Meal;");
            await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS IX_ForecastRecords_ForecastDate_Meal;");
            await AddSqliteColumnIfMissingAsync(db, "AttendanceRecords", "HallId", "INTEGER NULL");
            await AddSqliteColumnIfMissingAsync(db, "ForecastRecords", "HallId", "INTEGER NULL");
            await AddSqliteColumnIfMissingAsync(db, "ServiceActuals", "HallId", "INTEGER NULL");
            await AddSqliteColumnIfMissingAsync(db, "MealBookings", "HallId", "INTEGER NULL");
            await AddSqliteColumnIfMissingAsync(db, "WasteRecords", "Meal", "INTEGER NULL");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_AttendanceRecords_ServiceDate_Meal_HallId ON AttendanceRecords(ServiceDate, Meal, HallId);");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_ForecastRecords_ForecastDate_Meal_HallId ON ForecastRecords(ForecastDate, Meal, HallId);");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_ServiceActuals_ServiceDate_Meal_HallId ON ServiceActuals(ServiceDate, Meal, HallId);");
        }
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Manager", "Staff", "Student", "KitchenStaff" }.Concat(RoleNames.All).Distinct())
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Could not create role '{role}': {FormatErrors(roleResult.Errors)}");
        }

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var demoUsers = new[]
        {
            new DemoUser("admin@mess.local", "Admin123!", new[] { "Admin", RoleNames.SystemAdministrator }),
            new DemoUser("manager@mess.local", "Manager123!", new[] { "Manager", RoleNames.MessManager }),
            new DemoUser("kitchen@mess.local", "Kitchen123!", new[] { "Staff", RoleNames.KitchenStaff }),
            new DemoUser("student@mess.local", "Student123!", new[] { RoleNames.Student }),
            new DemoUser("messadmin@mess.local", "MessAdmin123!", new[] { RoleNames.MessAdministrator }),
            new DemoUser("finance@mess.local", "Finance123!", new[] { RoleNames.FinanceOfficer }),
            new DemoUser("sysadmin@mess.local", "SysAdmin123!", new[] { RoleNames.SystemAdministrator })
        };
        foreach (var demoUser in demoUsers)
            await EnsureUserAsync(userManager, demoUser, environment.IsDevelopment());

        foreach (var user in await userManager.Users.ToListAsync())
            if (!await HasAnyRoleAsync(userManager, user, RoleNames.All.Concat(new[] { "Admin", "Manager", "Staff" })))
            {
                var studentRoleResult = await userManager.AddToRoleAsync(user, RoleNames.Student);
                if (!studentRoleResult.Succeeded)
                    throw new InvalidOperationException($"Could not assign fallback Student role to '{user.Email}': {FormatErrors(studentRoleResult.Errors)}");
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

    }

    private static async Task EnsureUserAsync(UserManager<IdentityUser> userManager, DemoUser demoUser, bool resetPassword)
    {
        var user = await userManager.FindByEmailAsync(demoUser.Email);
        if (user is null)
        {
            user = new IdentityUser { UserName = demoUser.Email, Email = demoUser.Email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, demoUser.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not create demo user '{demoUser.Email}': {FormatErrors(result.Errors)}");
        }
        else
        {
            var changed = false;
            if (!string.Equals(user.UserName, demoUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = demoUser.Email;
                changed = true;
            }
            if (!string.Equals(user.Email, demoUser.Email, StringComparison.OrdinalIgnoreCase) || !user.EmailConfirmed)
            {
                user.Email = demoUser.Email;
                user.EmailConfirmed = true;
                changed = true;
            }
            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    throw new InvalidOperationException($"Could not update demo user '{demoUser.Email}': {FormatErrors(updateResult.Errors)}");
            }

            if (resetPassword)
            {
                var removeResult = await userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                    throw new InvalidOperationException($"Could not reset password for '{demoUser.Email}': {FormatErrors(removeResult.Errors)}");

                var addResult = await userManager.AddPasswordAsync(user, demoUser.Password);
                if (!addResult.Succeeded)
                    throw new InvalidOperationException($"Could not set password for '{demoUser.Email}': {FormatErrors(addResult.Errors)}");
            }
        }

        foreach (var role in demoUser.Roles)
        {
            if (await userManager.IsInRoleAsync(user, role))
                continue;

            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Could not assign role '{role}' to '{demoUser.Email}': {FormatErrors(roleResult.Errors)}");
        }
    }

    private sealed record DemoUser(string Email, string Password, string[] Roles);

    private static string FormatErrors(IEnumerable<IdentityError> errors) =>
        string.Join("; ", errors.Select(error => error.Description));

    private static async Task<bool> HasAnyRoleAsync(UserManager<IdentityUser> userManager, IdentityUser user, IEnumerable<string> roles)
    {
        foreach (var role in roles)
            if (await userManager.IsInRoleAsync(user, role))
                return true;
        return false;
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
                    WasteDate TEXT NOT NULL, Meal INTEGER NULL, IngredientId INTEGER NULL, MenuDishId INTEGER NULL, Quantity decimal(18,3) NOT NULL,
                    UnitCost decimal(18,2) NOT NULL, Reason INTEGER NOT NULL, Notes TEXT NULL,
                    CONSTRAINT FK_WasteRecords_Ingredients_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredients(Id) ON DELETE SET NULL,
                    CONSTRAINT FK_WasteRecords_MenuDishes_MenuDishId FOREIGN KEY (MenuDishId) REFERENCES MenuDishes(Id) ON DELETE SET NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS Halls (
                    Id INTEGER NOT NULL CONSTRAINT PK_Halls PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL, Capacity INTEGER NOT NULL, IsActive INTEGER NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS MealBookings (
                    Id INTEGER NOT NULL CONSTRAINT PK_MealBookings PRIMARY KEY AUTOINCREMENT,
                    StudentEmail TEXT NOT NULL, BookingDate TEXT NOT NULL, Meal INTEGER NOT NULL,
                    Status INTEGER NOT NULL, CreatedAt TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS Feedback (
                    Id INTEGER NOT NULL CONSTRAINT PK_Feedback PRIMARY KEY AUTOINCREMENT,
                    StudentEmail TEXT NOT NULL, SubmittedAt TEXT NOT NULL, Rating INTEGER NOT NULL, Comment TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS PreparationPlans (
                    Id INTEGER NOT NULL CONSTRAINT PK_PreparationPlans PRIMARY KEY AUTOINCREMENT,
                    PlanDate TEXT NOT NULL, Meal INTEGER NOT NULL, HallId INTEGER NULL,
                    PredictedServings INTEGER NOT NULL, SafetyMarginPercent decimal(5,2) NOT NULL, CreatedAt TEXT NOT NULL,
                    CONSTRAINT FK_PreparationPlans_Halls_HallId FOREIGN KEY (HallId) REFERENCES Halls(Id) ON DELETE SET NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS PreparationPlanLines (
                    Id INTEGER NOT NULL CONSTRAINT PK_PreparationPlanLines PRIMARY KEY AUTOINCREMENT,
                    PreparationPlanId INTEGER NOT NULL, IngredientId INTEGER NOT NULL,
                    RequiredQuantity decimal(18,3) NOT NULL, AvailableQuantity decimal(18,3) NOT NULL, ShortageQuantity decimal(18,3) NOT NULL,
                    CONSTRAINT FK_PreparationPlanLines_PreparationPlans_PreparationPlanId FOREIGN KEY (PreparationPlanId) REFERENCES PreparationPlans(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_PreparationPlanLines_Ingredients_IngredientId FOREIGN KEY (IngredientId) REFERENCES Ingredients(Id) ON DELETE CASCADE
                );
                """
            };
            foreach (var command in commands)
                await db.Database.ExecuteSqlRawAsync(command);
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_Halls_Name ON Halls(Name);");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_MealBookings_StudentEmail_BookingDate_Meal ON MealBookings(StudentEmail, BookingDate, Meal);");
            await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_PreparationPlans_PlanDate_Meal_HallId ON PreparationPlans(PlanDate, Meal, HallId);");
        }

    private static async Task AddSqliteColumnIfMissingAsync(ApplicationDbContext db, string table, string column, string definition)
    {
        var columns = await db.Database.SqlQueryRaw<string>($"SELECT name AS Value FROM pragma_table_info('{table}')").ToListAsync();
        if (!columns.Contains(column, StringComparer.OrdinalIgnoreCase))
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition};");
    }
    }

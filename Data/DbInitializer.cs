using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Models;

namespace web_ca_3.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
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
}

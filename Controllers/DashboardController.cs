using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Data;
using web_ca_3.Models;
using web_ca_3.Services;

namespace web_ca_3.Controllers;

[Authorize]
public class DashboardController(ApplicationDbContext db, ForecastingService forecasting) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var model = new DashboardViewModel
        {
            TodayForecast = forecasting.Forecast(await db.AttendanceRecords.ToListAsync(), await db.CalendarEvents.ToListAsync(), today, MealType.Lunch),
            ActiveDishes = await db.MenuDishes.CountAsync(x => x.IsActive),
            IngredientValue = await db.Ingredients.SumAsync(x => x.CurrentStock * x.UnitCost),
            WasteCostThisMonth = await db.WasteRecords.Where(x => x.WasteDate.Month == today.Month && x.WasteDate.Year == today.Year).SumAsync(x => (decimal?)x.Quantity * x.UnitCost) ?? 0,
            RecentWaste = await db.WasteRecords.Include(x => x.Ingredient).Include(x => x.MenuDish).OrderByDescending(x => x.WasteDate).Take(8).ToListAsync()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Forecast(DateTime? date, MealType meal = MealType.Lunch)
    {
        var target = date?.Date ?? DateTime.Today;
        var count = forecasting.Forecast(await db.AttendanceRecords.ToListAsync(), await db.CalendarEvents.ToListAsync(), target, meal);
        return Json(new { date = target.ToString("yyyy-MM-dd"), meal = meal.ToString(), forecast = count, confidence = forecasting.Confidence(await db.AttendanceRecords.CountAsync(x => x.Meal == meal)) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWaste(WasteRecord waste)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));
        if (waste.IngredientId is int ingredientId)
            waste.UnitCost = await db.Ingredients.Where(x => x.Id == ingredientId).Select(x => x.UnitCost).FirstOrDefaultAsync();
        await db.WasteRecords.AddAsync(waste);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

public sealed class DashboardViewModel
{
    public int TodayForecast { get; set; }
    public int ActiveDishes { get; set; }
    public decimal IngredientValue { get; set; }
    public decimal WasteCostThisMonth { get; set; }
    public List<WasteRecord> RecentWaste { get; set; } = [];
}

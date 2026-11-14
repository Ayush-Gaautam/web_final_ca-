using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Data;
using web_ca_3.Models;
using web_ca_3.Services;

namespace web_ca_3.Controllers;

[ApiController, Route("api")]
public class ApiController(ApplicationDbContext db, ForecastingService forecasting) : ControllerBase
{
    [HttpGet("forecast")]
    public async Task<IActionResult> Forecast(DateTime? date = null, MealType meal = MealType.Lunch)
    {
        var target = date?.Date ?? DateTime.Today;
        var value = forecasting.Forecast(await db.AttendanceRecords.ToListAsync(), await db.CalendarEvents.ToListAsync(), target, meal);
        return Ok(new { targetDate = target, meal, forecast = value });
    }

    [HttpGet("dishes")]
    public async Task<IEnumerable<MenuDish>> Dishes() => await db.MenuDishes.Include(x => x.Ingredients).ThenInclude(x => x.Ingredient).Where(x => x.IsActive).ToListAsync();

    [HttpGet("waste-summary")]
    public async Task<IActionResult> WasteSummary() => Ok(await db.WasteRecords.GroupBy(x => x.Reason).Select(x => new { reason = x.Key.ToString(), quantity = x.Sum(y => y.Quantity), cost = x.Sum(y => y.Quantity * y.UnitCost) }).ToListAsync());

    [Authorize]
    [HttpPost("attendance")]
    public async Task<IActionResult> AddAttendance(AttendanceRecord record)
    {
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Forecast), new { date = record.ServiceDate, meal = record.Meal }, record);
    }

    [Authorize]
    [HttpPost("events")]
    public async Task<IActionResult> AddEvent(CalendarEvent calendarEvent)
    {
        db.CalendarEvents.Add(calendarEvent);
        await db.SaveChangesAsync();
        return Ok(calendarEvent);
    }

    [Authorize]
    [HttpPost("actuals")]
    public async Task<IActionResult> AddActual(ServiceActual actual)
    {
        actual.LeftoverCount = Math.Max(0, actual.PreparedCount - actual.ServedCount);
        db.ServiceActuals.Add(actual);
        await db.SaveChangesAsync();
        return Ok(actual);
    }

    [Authorize]
    [HttpPost("ingredients")]
    public async Task<IActionResult> AddIngredient(Ingredient ingredient)
    {
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        return Ok(ingredient);
    }

    [Authorize]
    [HttpPost("dishes")]
    public async Task<IActionResult> AddDish(MenuDish dish)
    {
        db.MenuDishes.Add(dish);
        await db.SaveChangesAsync();
        return Ok(dish);
    }
}

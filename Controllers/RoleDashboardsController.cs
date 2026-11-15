using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Data;
using web_ca_3.Models;
using web_ca_3.Security;
using web_ca_3.Services;

namespace web_ca_3.Controllers;

[Authorize]
public sealed class RoleDashboardsController(
    ApplicationDbContext db,
    ForecastingService forecasting,
    WasteAnalyticsService wasteAnalytics) : Controller
{
    [Authorize(Roles = RoleNames.Student)]
    public Task<IActionResult> Student() => RenderAsync(RoleNames.Student, "Bookings, menus and feedback", "Book meals, review menus and track your attendance.");

    [Authorize(Roles = RoleNames.KitchenStaff + ",Staff")]
    public Task<IActionResult> KitchenStaff() => RenderAsync(RoleNames.KitchenStaff, "Preparation and service control", "Capture attendance, preparation, service actuals and waste.");

    [Authorize(Roles = RoleNames.MessManager + ",Manager")]
    public Task<IActionResult> MessManager() => RenderAsync(RoleNames.MessManager, "Demand and planning control", "Review forecasts, preparation plans and operational performance.");

    [Authorize(Roles = RoleNames.MessAdministrator + ",Admin")]
    public Task<IActionResult> MessAdministrator() => RenderAsync(RoleNames.MessAdministrator, "Mess administration", "Manage halls, menus, users and operational configuration.");

    [Authorize(Roles = RoleNames.FinanceOfficer)]
    public Task<IActionResult> FinanceOfficer() => RenderAsync(RoleNames.FinanceOfficer, "Cost and savings control", "Monitor inventory value, waste cost and measurable savings.");

    [Authorize(Roles = RoleNames.SystemAdministrator + ",Admin")]
    public Task<IActionResult> SystemAdministrator() => RenderAsync(RoleNames.SystemAdministrator, "System administration", "Monitor platform health, roles, APIs and database-backed activity.");

    private async Task<IActionResult> RenderAsync(string role, string subtitle, string description)
    {
        var today = DateTime.Today;
        var forecast = forecasting.Analyze(
            await db.AttendanceRecords.AsNoTracking().ToListAsync(),
            await db.CalendarEvents.AsNoTracking().ToListAsync(),
            today, MealType.Lunch);
        var waste = await wasteAnalytics.SummaryAsync(today.AddDays(-6), today);
        var model = new RoleDashboardViewModel
        {
            RoleKey = role,
            RoleName = RoleNames.DisplayName(role),
            Subtitle = subtitle,
            Description = description,
            TodayForecast = forecast.Forecast,
            ForecastConfidence = forecast.Confidence,
            ForecastMae = forecast.Mae,
            ForecastMape = forecast.Mape,
            ActiveDishes = await db.MenuDishes.CountAsync(x => x.IsActive),
            IngredientValue = await db.Ingredients.SumAsync(x => x.CurrentStock * x.UnitCost),
            WasteCost = waste.WasteCost,
            WastePercentage = waste.WastePercentage,
            Bookings = await db.MealBookings.CountAsync(x => x.BookingDate >= today && x.Status == BookingStatus.Booked),
            AttendanceRecords = await db.AttendanceRecords.CountAsync(x => x.ServiceDate >= today.AddDays(-6))
        };
        return View(role, model);
    }
}

public sealed class RoleDashboardViewModel
{
    public string RoleKey { get; set; } = RoleNames.Student;
    public string RoleName { get; set; } = "Student";
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TodayForecast { get; set; }
    public decimal ForecastConfidence { get; set; }
    public decimal ForecastMae { get; set; }
    public decimal ForecastMape { get; set; }
    public int ActiveDishes { get; set; }
    public decimal IngredientValue { get; set; }
    public decimal WasteCost { get; set; }
    public decimal WastePercentage { get; set; }
    public int Bookings { get; set; }
    public int AttendanceRecords { get; set; }
}

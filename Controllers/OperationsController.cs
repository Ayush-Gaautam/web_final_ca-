using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Data;

namespace web_ca_3.Controllers;

[Authorize]
public sealed class OperationsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new OperationsViewModel
        {
            Dishes = await db.MenuDishes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            Ingredients = await db.Ingredients.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            Attendance = await db.AttendanceRecords.AsNoTracking().OrderByDescending(x => x.ServiceDate).Take(12).ToListAsync()
            ,Bookings = await db.MealBookings.AsNoTracking().Where(x => x.StudentEmail == User.Identity!.Name && x.Status == Models.BookingStatus.Booked).OrderBy(x => x.BookingDate).Take(10).ToListAsync()
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDish(Models.DishRequest request)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));
        if (!await db.MenuDishes.AnyAsync(x => x.Name == request.Name))
        {
            db.MenuDishes.Add(new Models.MenuDish { Name = request.Name.Trim(), Category = request.Category.Trim(), Calories = request.Calories, IsActive = request.IsActive });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddIngredient(Models.IngredientRequest request)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));
        if (!await db.Ingredients.AnyAsync(x => x.Name == request.Name))
        {
            db.Ingredients.Add(new Models.Ingredient { Name = request.Name.Trim(), Unit = request.Unit.Trim(), UnitCost = request.UnitCost, CurrentStock = request.CurrentStock });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAttendance(Models.AttendanceRequest request)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));
        db.AttendanceRecords.Add(new Models.AttendanceRecord { ServiceDate = request.ServiceDate.Date, Meal = request.Meal, ExpectedCount = request.ExpectedCount, ActualCount = request.ActualCount, Notes = request.Notes });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BookMeal(Models.BookingRequest request)
    {
        if (request.BookingDate.Date < DateTime.Today || (request.BookingDate.Date == DateTime.Today && DateTime.Now.Hour >= 10))
            TempData["Error"] = "Bookings for today close at 10:00.";
        else if (!await db.MealBookings.AnyAsync(x => x.StudentEmail == User.Identity!.Name && x.BookingDate == request.BookingDate.Date && x.Meal == request.Meal && x.Status == Models.BookingStatus.Booked))
        {
            db.MealBookings.Add(new Models.MealBooking { StudentEmail = User.Identity!.Name!, BookingDate = request.BookingDate.Date, Meal = request.Meal });
            await db.SaveChangesAsync();
            TempData["Message"] = "Meal booked successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMeal(int id)
    {
        var booking = await db.MealBookings.FindAsync(id);
        if (booking is not null && (User.IsInRole("Admin") || booking.StudentEmail == User.Identity!.Name))
        {
            booking.Status = Models.BookingStatus.Cancelled;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}

public sealed class OperationsViewModel
{
    public List<Models.MenuDish> Dishes { get; set; } = [];
    public List<Models.Ingredient> Ingredients { get; set; } = [];
    public List<Models.AttendanceRecord> Attendance { get; set; } = [];
    public List<Models.MealBooking> Bookings { get; set; } = [];
}

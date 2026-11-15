using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Data;

namespace web_ca_3.Controllers;

[Authorize(Roles = "Admin,Manager,Staff")]
public sealed class OperationsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new OperationsViewModel
        {
            Dishes = await db.MenuDishes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            Ingredients = await db.Ingredients.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            Attendance = await db.AttendanceRecords.AsNoTracking().OrderByDescending(x => x.ServiceDate).Take(12).ToListAsync()
        };
        return View(model);
    }
}

public sealed class OperationsViewModel
{
    public List<Models.MenuDish> Dishes { get; set; } = [];
    public List<Models.Ingredient> Ingredients { get; set; } = [];
    public List<Models.AttendanceRecord> Attendance { get; set; } = [];
}

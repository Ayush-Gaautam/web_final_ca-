using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Data;
using web_ca_3.Models;
using web_ca_3.Security;

namespace web_ca_3.Controllers;

[Authorize(Roles = RoleNames.MessAdministrator + "," + RoleNames.SystemAdministrator + ",Admin")]
public sealed class AdminController(ApplicationDbContext db, UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index() => View(new AdminViewModel
    {
        Users = await users.Users.OrderBy(x => x.Email).ToListAsync(),
        Halls = await db.Halls.OrderBy(x => x.Name).ToListAsync()
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHall(HallRequest request)
    {
        if (ModelState.IsValid && !await db.Halls.AnyAsync(x => x.Name == request.Name))
        {
            db.Halls.Add(new Hall { Name = request.Name.Trim(), Capacity = request.Capacity, IsActive = request.IsActive });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableUser(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user is not null) { user.LockoutEnabled = true; user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(10); await users.UpdateAsync(user); }
        return RedirectToAction(nameof(Index));
    }
}

public sealed class AdminViewModel
{
    public List<IdentityUser> Users { get; set; } = [];
    public List<Hall> Halls { get; set; } = [];
}

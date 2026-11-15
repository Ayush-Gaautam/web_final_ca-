using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Data;
using web_ca_3.Models;
using web_ca_3.Services;

namespace web_ca_3.Controllers;

[ApiController, Authorize, Route("api")]
public sealed class ApiController(ApplicationDbContext db, ForecastingService forecasting) : ControllerBase
{
    [HttpGet("forecast")]
    public async Task<ActionResult<ForecastResult>> Forecast(DateTime? date = null, MealType meal = MealType.Lunch)
    {
        var target = date?.Date ?? DateTime.Today;
        return Ok(forecasting.Analyze(await db.AttendanceRecords.AsNoTracking().ToListAsync(), await db.CalendarEvents.AsNoTracking().ToListAsync(), target, meal));
    }

    [HttpGet("forecast/metrics")]
    public async Task<IActionResult> ForecastMetrics(MealType meal = MealType.Lunch)
    {
        var records = await db.AttendanceRecords.AsNoTracking().Where(x => x.Meal == meal).ToListAsync();
        return Ok(new { meal, mae = forecasting.MeanAbsoluteError(records), mape = forecasting.MeanAbsolutePercentageError(records), sampleCount = records.Count });
    }

    [HttpGet("dishes")]
    public async Task<ActionResult<IEnumerable<DishDto>>> GetDishes() => Ok(await db.MenuDishes.AsNoTracking().Where(x => x.IsActive).Select(x => new DishDto(
        x.Id, x.Name, x.Category, x.Calories, x.IsActive,
        x.Ingredients.Select(i => new RecipeIngredientDto(i.IngredientId, i.Ingredient.Name, i.Ingredient.Unit, i.QuantityPerServing, i.Ingredient.UnitCost)).ToList())).ToListAsync());

    [HttpGet("dishes/{id:int}")]
    public async Task<ActionResult<DishDto>> GetDish(int id)
    {
        var dish = await db.MenuDishes.AsNoTracking().Where(x => x.Id == id).Select(x => new DishDto(
            x.Id, x.Name, x.Category, x.Calories, x.IsActive,
            x.Ingredients.Select(i => new RecipeIngredientDto(i.IngredientId, i.Ingredient.Name, i.Ingredient.Unit, i.QuantityPerServing, i.Ingredient.UnitCost)).ToList())).SingleOrDefaultAsync();
        return dish is null ? NotFound() : Ok(dish);
    }

    [HttpPost("dishes"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<DishDto>> CreateDish(DishRequest request)
    {
        if (await db.MenuDishes.AnyAsync(x => x.Name == request.Name)) return Conflict("A dish with this name already exists.");
        var dish = new MenuDish { Name = request.Name.Trim(), Category = request.Category.Trim(), Calories = request.Calories, IsActive = request.IsActive };
        db.MenuDishes.Add(dish);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetDish), new { id = dish.Id }, dish);
    }

    [HttpPut("dishes/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateDish(int id, DishRequest request)
    {
        var dish = await db.MenuDishes.FindAsync(id);
        if (dish is null) return NotFound();
        if (await db.MenuDishes.AnyAsync(x => x.Id != id && x.Name == request.Name)) return Conflict("A dish with this name already exists.");
        dish.Name = request.Name.Trim(); dish.Category = request.Category.Trim(); dish.Calories = request.Calories; dish.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("dishes/{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDish(int id)
    {
        var dish = await db.MenuDishes.FindAsync(id);
        if (dish is null) return NotFound();
        dish.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("ingredients")]
    public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients() => Ok(await db.Ingredients.AsNoTracking().OrderBy(x => x.Name).ToListAsync());

    [HttpGet("ingredients/{id:int}")]
    public async Task<ActionResult<Ingredient>> GetIngredient(int id) => await db.Ingredients.FindAsync(id) is { } item ? Ok(item) : NotFound();

    [HttpPost("ingredients"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Ingredient>> CreateIngredient(IngredientRequest request)
    {
        if (await db.Ingredients.AnyAsync(x => x.Name == request.Name)) return Conflict("An ingredient with this name already exists.");
        var item = new Ingredient { Name = request.Name.Trim(), Unit = request.Unit.Trim(), UnitCost = request.UnitCost, CurrentStock = request.CurrentStock };
        db.Ingredients.Add(item); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetIngredient), new { id = item.Id }, item);
    }

    [HttpPut("ingredients/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateIngredient(int id, IngredientRequest request)
    {
        var item = await db.Ingredients.FindAsync(id);
        if (item is null) return NotFound();
        if (await db.Ingredients.AnyAsync(x => x.Id != id && x.Name == request.Name)) return Conflict("An ingredient with this name already exists.");
        item.Name = request.Name.Trim(); item.Unit = request.Unit.Trim(); item.UnitCost = request.UnitCost; item.CurrentStock = request.CurrentStock;
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("ingredients/{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteIngredient(int id)
    {
        var item = await db.Ingredients.FindAsync(id);
        if (item is null) return NotFound();
        db.Ingredients.Remove(item); await db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("attendance")]
    public async Task<ActionResult<IEnumerable<AttendanceRecord>>> GetAttendance() => Ok(await db.AttendanceRecords.AsNoTracking().OrderByDescending(x => x.ServiceDate).ToListAsync());

    [HttpPost("attendance"), Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<AttendanceRecord>> CreateAttendance(AttendanceRequest request)
    {
        if (await db.AttendanceRecords.AnyAsync(x => x.ServiceDate.Date == request.ServiceDate.Date && x.Meal == request.Meal)) return Conflict("Attendance already exists for this service.");
        var record = new AttendanceRecord { ServiceDate = request.ServiceDate.Date, Meal = request.Meal, ExpectedCount = request.ExpectedCount, ActualCount = request.ActualCount, Notes = request.Notes };
        db.AttendanceRecords.Add(record); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAttendance), null, record);
    }

    [HttpPut("attendance/{id:int}"), Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> UpdateAttendance(int id, AttendanceRequest request)
    {
        var record = await db.AttendanceRecords.FindAsync(id); if (record is null) return NotFound();
        record.ServiceDate = request.ServiceDate.Date; record.Meal = request.Meal; record.ExpectedCount = request.ExpectedCount; record.ActualCount = request.ActualCount; record.Notes = request.Notes;
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("attendance/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteAttendance(int id) => await RemoveAsync(db, db.AttendanceRecords, id);

    [HttpGet("events")]
    public async Task<ActionResult<IEnumerable<CalendarEvent>>> GetEvents() => Ok(await db.CalendarEvents.AsNoTracking().OrderBy(x => x.EventDate).ToListAsync());

    [HttpPost("events"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CalendarEvent>> CreateEvent(EventRequest request)
    {
        var item = new CalendarEvent { Title = request.Title.Trim(), EventDate = request.EventDate.Date, Description = request.Description, AttendanceAdjustment = request.AttendanceAdjustment };
        db.CalendarEvents.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetEvents), null, item);
    }

    [HttpPut("events/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var item = await db.CalendarEvents.FindAsync(id); if (item is null) return NotFound();
        item.Title = request.Title.Trim(); item.EventDate = request.EventDate.Date; item.Description = request.Description; item.AttendanceAdjustment = request.AttendanceAdjustment;
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("events/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteEvent(int id) => await RemoveAsync(db, db.CalendarEvents, id);

    [HttpGet("actuals")]
    public async Task<ActionResult<IEnumerable<ServiceActual>>> GetActuals() => Ok(await db.ServiceActuals.AsNoTracking().OrderByDescending(x => x.ServiceDate).ToListAsync());

    [HttpPost("actuals"), Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ServiceActual>> CreateActual(ActualRequest request)
    {
        var item = new ServiceActual { ServiceDate = request.ServiceDate.Date, Meal = request.Meal, PreparedCount = request.PreparedCount, ServedCount = request.ServedCount, LeftoverCount = Math.Max(0, request.PreparedCount - request.ServedCount) };
        db.ServiceActuals.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetActuals), null, item);
    }

    [HttpPut("actuals/{id:int}"), Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> UpdateActual(int id, ActualRequest request)
    {
        var item = await db.ServiceActuals.FindAsync(id); if (item is null) return NotFound();
        item.ServiceDate = request.ServiceDate.Date; item.Meal = request.Meal; item.PreparedCount = request.PreparedCount; item.ServedCount = request.ServedCount; item.LeftoverCount = Math.Max(0, request.PreparedCount - request.ServedCount);
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("actuals/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteActual(int id) => await RemoveAsync(db, db.ServiceActuals, id);

    [HttpGet("waste")]
    public async Task<ActionResult<IEnumerable<WasteRecord>>> GetWaste() => Ok(await db.WasteRecords.AsNoTracking().Include(x => x.Ingredient).Include(x => x.MenuDish).OrderByDescending(x => x.WasteDate).ToListAsync());

    [HttpGet("waste-summary")]
    public async Task<IActionResult> WasteSummary() => Ok(await db.WasteRecords.AsNoTracking().GroupBy(x => x.Reason).Select(x => new { reason = x.Key.ToString(), quantity = x.Sum(y => y.Quantity), cost = x.Sum(y => y.Quantity * y.UnitCost) }).ToListAsync());

    [HttpPost("waste"), Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<WasteRecord>> CreateWaste(WasteRequest request)
    {
        if (request.IngredientId is null && request.MenuDishId is null) return BadRequest("IngredientId or MenuDishId is required.");
        var unitCost = request.IngredientId is int ingredientId
            ? await db.Ingredients.Where(x => x.Id == ingredientId).Select(x => (decimal?)x.UnitCost).SingleOrDefaultAsync()
            : 0;
        if (request.IngredientId is not null && unitCost is null) return BadRequest("Ingredient not found.");
        var item = new WasteRecord { WasteDate = request.WasteDate.Date, IngredientId = request.IngredientId, MenuDishId = request.MenuDishId, Quantity = request.Quantity, UnitCost = unitCost ?? 0, Reason = request.Reason, Notes = request.Notes };
        db.WasteRecords.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetWaste), null, item);
    }

    [HttpPut("waste/{id:int}"), Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> UpdateWaste(int id, WasteRequest request)
    {
        var item = await db.WasteRecords.FindAsync(id); if (item is null) return NotFound();
        item.WasteDate = request.WasteDate.Date; item.IngredientId = request.IngredientId; item.MenuDishId = request.MenuDishId; item.Quantity = request.Quantity; item.Reason = request.Reason; item.Notes = request.Notes;
        if (request.IngredientId is int ingredientId) item.UnitCost = await db.Ingredients.Where(x => x.Id == ingredientId).Select(x => x.UnitCost).SingleOrDefaultAsync();
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("waste/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteWaste(int id) => await RemoveAsync(db, db.WasteRecords, id);

    [HttpGet("halls")]
    public async Task<ActionResult<IEnumerable<Hall>>> GetHalls() => Ok(await db.Halls.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync());

    [HttpPost("halls"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Hall>> CreateHall(HallRequest request)
    {
        if (await db.Halls.AnyAsync(x => x.Name == request.Name)) return Conflict("A hall with this name already exists.");
        var hall = new Hall { Name = request.Name.Trim(), Capacity = request.Capacity, IsActive = request.IsActive };
        db.Halls.Add(hall); await db.SaveChangesAsync();
        return Ok(hall);
    }

    [HttpGet("bookings"), Authorize(Roles = "Admin,Manager,Staff,Student,KitchenStaff")]
    public async Task<ActionResult<IEnumerable<MealBooking>>> GetBookings()
    {
        var email = User.IsInRole("Student") ? User.Identity!.Name! : null;
        var query = db.MealBookings.AsNoTracking().AsQueryable();
        if (email is not null) query = query.Where(x => x.StudentEmail == email);
        return Ok(await query.OrderByDescending(x => x.BookingDate).ToListAsync());
    }

    [HttpPost("bookings"), Authorize(Roles = "Student")]
    public async Task<ActionResult<MealBooking>> CreateBooking(BookingRequest request)
    {
        if (request.BookingDate.Date < DateTime.Today) return BadRequest("Booking date must be today or later.");
        if (request.BookingDate.Date == DateTime.Today && DateTime.Now.Hour >= 10) return BadRequest("Same-day booking closes at 10:00.");
        var email = User.Identity!.Name!;
        if (await db.MealBookings.AnyAsync(x => x.StudentEmail == email && x.BookingDate == request.BookingDate.Date && x.Meal == request.Meal && x.Status == BookingStatus.Booked))
            return Conflict("This meal is already booked.");
        var booking = new MealBooking { StudentEmail = email, BookingDate = request.BookingDate.Date, Meal = request.Meal };
        db.MealBookings.Add(booking); await db.SaveChangesAsync();
        return Ok(booking);
    }

    [HttpDelete("bookings/{id:int}"), Authorize(Roles = "Student,Admin,Manager")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var booking = await db.MealBookings.FindAsync(id);
        if (booking is null || (User.IsInRole("Student") && booking.StudentEmail != User.Identity!.Name)) return NotFound();
        if (booking.BookingDate.Date == DateTime.Today && DateTime.Now.Hour >= 10) return BadRequest("Same-day cancellation closes at 10:00.");
        booking.Status = BookingStatus.Cancelled; await db.SaveChangesAsync(); return NoContent();
    }

    [HttpPost("feedback"), Authorize(Roles = "Student")]
    public async Task<ActionResult<Feedback>> CreateFeedback(FeedbackRequest request)
    {
        var feedback = new Feedback { StudentEmail = User.Identity!.Name!, Rating = request.Rating, Comment = request.Comment.Trim() };
        db.Feedback.Add(feedback); await db.SaveChangesAsync(); return Ok(feedback);
    }

    private static async Task<IActionResult> RemoveAsync<TEntity>(ApplicationDbContext db, DbSet<TEntity> set, int id) where TEntity : class
    {
        var item = await set.FindAsync(id); if (item is null) return new NotFoundResult();
        set.Remove(item); await db.SaveChangesAsync(); return new NoContentResult();
    }
}

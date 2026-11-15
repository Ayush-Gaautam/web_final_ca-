using Microsoft.EntityFrameworkCore;
using web_ca_3.Models;
using web_ca_3.Services;

namespace web_ca_3.Data;

public static class DevelopmentDataSeeder
{
    private const string Marker = "__development_sample_data_v1__";

    public static async Task InitializeAsync(IServiceProvider services, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return;

        var db = services.GetRequiredService<ApplicationDbContext>();
        if (await db.CalendarEvents.AnyAsync(x => x.Title == Marker) &&
            await db.PreparationPlanLines.AnyAsync())
        {
            var sampleWaste = await db.WasteRecords.Where(x => x.Notes == "Development sample waste" && x.Meal == null).ToListAsync();
            foreach (var item in sampleWaste)
                item.Meal = item.MenuDishId.HasValue ? MealType.Dinner : MealType.Lunch;
            if (sampleWaste.Count > 0)
                await db.SaveChangesAsync();
            return;
        }

        var planning = services.GetRequiredService<PlanningService>();
        var forecasting = services.GetRequiredService<ForecastingService>();
        var today = DateTime.Today;

        var halls = await EnsureHallsAsync(db);
        var ingredients = await EnsureIngredientsAsync(db);
        var dishes = await EnsureDishesAsync(db, ingredients);
        await db.SaveChangesAsync();

        var student = "student@mess.local";
        for (var offset = 1; offset <= 30; offset++)
        {
            var date = today.AddDays(-offset);
            foreach (var hall in halls)
            {
                foreach (var meal in new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner })
                {
                    if (!await db.AttendanceRecords.AnyAsync(x => x.ServiceDate == date && x.Meal == meal && x.HallId == hall.Id))
                    {
                        var baseCount = meal == MealType.Lunch ? 115 : meal == MealType.Dinner ? 95 : 65;
                        var variation = ((offset * 7 + hall.Id * 11 + (int)meal * 5) % 21) - 10;
                        var actual = Math.Max(0, baseCount + variation);
                        db.AttendanceRecords.Add(new AttendanceRecord
                        {
                            ServiceDate = date,
                            Meal = meal,
                            HallId = hall.Id,
                            ExpectedCount = actual + ((offset + (int)meal) % 7) - 3,
                            ActualCount = actual,
                            Notes = "Development sample attendance"
                        });
                    }

                    if (!await db.ServiceActuals.AnyAsync(x => x.ServiceDate == date && x.Meal == meal && x.HallId == hall.Id))
                    {
                        var served = meal == MealType.Lunch ? 108 : meal == MealType.Dinner ? 88 : 58;
                        db.ServiceActuals.Add(new ServiceActual
                        {
                            ServiceDate = date,
                            Meal = meal,
                            HallId = hall.Id,
                            PreparedCount = served + 12,
                            ServedCount = served,
                            LeftoverCount = 12
                        });
                    }
                }
            }
        }

        await db.SaveChangesAsync();
        dishes = await db.MenuDishes.Include(x => x.Ingredients).ThenInclude(x => x.Ingredient)
            .Where(x => x.Name == "Vegetable Khichdi" || x.Name == "Rajma Rice" || x.Name == "Dal Roti" || x.Name == "Aloo Paratha")
            .ToListAsync();

        foreach (var eventDate in new[] { today.AddDays(3), today.AddDays(10) })
        {
            if (!await db.CalendarEvents.AnyAsync(x => x.EventDate == eventDate && x.Title.StartsWith("Development ")))
                db.CalendarEvents.Add(new CalendarEvent
                {
                    Title = $"Development festival {eventDate:dd MMM}",
                    EventDate = eventDate,
                    AttendanceAdjustment = 35,
                    Description = "Simulated campus event"
                });
        }

        for (var offset = 0; offset < 7; offset++)
        {
            var date = today.AddDays(offset);
            foreach (var hall in halls)
            {
                var result = forecasting.Analyze(await db.AttendanceRecords.AsNoTracking().ToListAsync(),
                    await db.CalendarEvents.AsNoTracking().ToListAsync(), date, MealType.Lunch, hall.Id);
                var existing = await db.ForecastRecords.SingleOrDefaultAsync(x => x.ForecastDate == date && x.Meal == MealType.Lunch && x.HallId == hall.Id);
                if (existing is null)
                    db.ForecastRecords.Add(new ForecastRecord
                    {
                        ForecastDate = date,
                        Meal = MealType.Lunch,
                        HallId = hall.Id,
                        ForecastCount = result.Forecast,
                        Confidence = result.Confidence,
                        Method = result.Method
                    });
            }
        }

        if (!await db.MealBookings.AnyAsync(x => x.StudentEmail == student && x.BookingDate >= today))
        {
            db.MealBookings.AddRange(
                new MealBooking { StudentEmail = student, BookingDate = today.AddDays(1), Meal = MealType.Lunch, HallId = halls[0].Id },
                new MealBooking { StudentEmail = student, BookingDate = today.AddDays(2), Meal = MealType.Dinner, HallId = halls[1].Id });
        }

        var wasteIngredient = await db.Ingredients.SingleAsync(x => x.Name == "Rice");
        var wasteDish = await db.MenuDishes.SingleAsync(x => x.Name == "Rajma Rice");
        if (!await db.WasteRecords.AnyAsync(x => x.Notes == "Development sample waste"))
        {
            db.WasteRecords.AddRange(
                new WasteRecord { WasteDate = today.AddDays(-1), Meal = MealType.Lunch, IngredientId = wasteIngredient.Id, Quantity = 2.4m, UnitCost = wasteIngredient.UnitCost, Reason = WasteReason.Overproduction, Notes = "Development sample waste" },
                new WasteRecord { WasteDate = today.AddDays(-3), Meal = MealType.Dinner, MenuDishId = wasteDish.Id, Quantity = 7, UnitCost = 18, Reason = WasteReason.PlateWaste, Notes = "Development sample waste" });
        }

        await db.SaveChangesAsync();

        var planDate = today.AddDays(1);
        var existingPlan = await db.PreparationPlans.Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.PlanDate == planDate && x.Meal == MealType.Lunch && x.HallId == halls[0].Id);
        if (existingPlan is null || existingPlan.Lines.Count == 0)
        {
            var predicted = await db.ForecastRecords.Where(x => x.ForecastDate == planDate && x.Meal == MealType.Lunch && x.HallId == halls[0].Id)
                .Select(x => x.ForecastCount).SingleOrDefaultAsync();
            var plan = planning.BuildPlan(planDate, MealType.Lunch, predicted,
                new[] { (dishes[0], predicted / 2), (dishes[1], predicted - predicted / 2) }, 10, halls[0]);
            if (existingPlan is null)
                db.PreparationPlans.Add(plan);
            else
                db.PreparationPlanLines.AddRange(plan.Lines.Select(line => new PreparationPlanLine
                {
                    PreparationPlanId = existingPlan.Id,
                    IngredientId = line.IngredientId,
                    RequiredQuantity = line.RequiredQuantity,
                    AvailableQuantity = line.AvailableQuantity,
                    ShortageQuantity = line.ShortageQuantity
                }));
        }

        if (!await db.CalendarEvents.AnyAsync(x => x.Title == Marker))
            db.CalendarEvents.Add(new CalendarEvent
            {
                Title = Marker,
                EventDate = today,
                Description = "Idempotency marker for development sample data",
                AttendanceAdjustment = 0
            });
        await db.SaveChangesAsync();
    }

    private static async Task<List<Hall>> EnsureHallsAsync(ApplicationDbContext db)
    {
        var result = new List<Hall>();
        foreach (var item in new[] { ("North Hall", 300), ("South Hall", 220) })
        {
            var hall = await db.Halls.SingleOrDefaultAsync(x => x.Name == item.Item1);
            if (hall is null)
            {
                hall = new Hall { Name = item.Item1, Capacity = item.Item2 };
                db.Halls.Add(hall);
            }
            result.Add(hall);
        }
        await db.SaveChangesAsync();
        return result;
    }

    private static async Task<List<Ingredient>> EnsureIngredientsAsync(ApplicationDbContext db)
    {
        var values = new[] { ("Wheat flour", "kg", 55m, 60m), ("Potatoes", "kg", 35m, 75m), ("Spices", "kg", 240m, 8m) };
        var result = new List<Ingredient>();
        foreach (var value in values)
        {
            var item = await db.Ingredients.SingleOrDefaultAsync(x => x.Name == value.Item1);
            if (item is null)
            {
                item = new Ingredient { Name = value.Item1, Unit = value.Item2, UnitCost = value.Item3, CurrentStock = value.Item4 };
                db.Ingredients.Add(item);
            }
            result.Add(item);
        }
        return result;
    }

    private static async Task<List<MenuDish>> EnsureDishesAsync(ApplicationDbContext db, List<Ingredient> ingredients)
    {
        var rice = await db.Ingredients.SingleAsync(x => x.Name == "Rice");
        var dal = await db.Ingredients.SingleAsync(x => x.Name == "Dal");
        var flour = ingredients.Single(x => x.Name == "Wheat flour");
        var potatoes = ingredients.Single(x => x.Name == "Potatoes");
        var spices = ingredients.Single(x => x.Name == "Spices");
        var result = new List<MenuDish>();

        foreach (var item in new[] { ("Dal Roti", "Main", 390), ("Aloo Paratha", "Main", 450) })
        {
            var dish = await db.MenuDishes.SingleOrDefaultAsync(x => x.Name == item.Item1);
            if (dish is null)
            {
                dish = new MenuDish { Name = item.Item1, Category = item.Item2, Calories = item.Item3 };
                db.MenuDishes.Add(dish);
                db.RecipeIngredients.AddRange(
                    item.Item1 == "Dal Roti"
                        ? new[] { new RecipeIngredient { MenuDish = dish, Ingredient = flour, QuantityPerServing = .08m }, new RecipeIngredient { MenuDish = dish, Ingredient = dal, QuantityPerServing = .04m }, new RecipeIngredient { MenuDish = dish, Ingredient = spices, QuantityPerServing = .002m } }
                        : new[] { new RecipeIngredient { MenuDish = dish, Ingredient = flour, QuantityPerServing = .1m }, new RecipeIngredient { MenuDish = dish, Ingredient = potatoes, QuantityPerServing = .12m }, new RecipeIngredient { MenuDish = dish, Ingredient = spices, QuantityPerServing = .002m } });
            }
            result.Add(dish);
        }
        return result;
    }
}

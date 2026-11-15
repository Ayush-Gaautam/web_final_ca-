using Microsoft.EntityFrameworkCore;
using web_ca_3.Data;
using web_ca_3.Models;

namespace web_ca_3.Services;

public sealed record WasteSummary(DateTime From, DateTime To, decimal PreparedQuantity, decimal ServedQuantity,
    decimal LeftoverQuantity, decimal WasteQuantity, decimal WastePercentage, decimal WasteCost);

public sealed record WasteBreakdown(string Label, decimal Quantity, decimal Cost);

public sealed class WasteAnalyticsService(ApplicationDbContext db)
{
    public async Task<WasteSummary> SummaryAsync(DateTime from, DateTime to, MealType? meal = null)
    {
        var actuals = db.ServiceActuals.AsNoTracking().Where(x => x.ServiceDate >= from.Date && x.ServiceDate <= to.Date);
        if (meal.HasValue)
            actuals = actuals.Where(x => x.Meal == meal.Value);
        var actualRows = await actuals.ToListAsync();
        var waste = db.WasteRecords.AsNoTracking().Where(x => x.WasteDate >= from.Date && x.WasteDate <= to.Date);
        var wasteRows = await waste.ToListAsync();
        var prepared = actualRows.Sum(x => x.PreparedCount);
        var served = actualRows.Sum(x => x.ServedCount);
        var leftovers = actualRows.Sum(x => Math.Max(0, x.LeftoverCount));
        var wasteQuantity = wasteRows.Sum(x => x.Quantity) + leftovers;
        return new WasteSummary(from.Date, to.Date, prepared, served, leftovers, wasteQuantity,
            prepared == 0 ? 0 : Math.Round(wasteQuantity / prepared * 100m, 2),
            Math.Round(wasteRows.Sum(x => x.Cost), 2));
    }

    public async Task<IReadOnlyList<WasteBreakdown>> ByDishAsync(DateTime from, DateTime to)
    {
        return await db.WasteRecords.AsNoTracking().Where(x => x.WasteDate >= from.Date && x.WasteDate <= to.Date)
            .GroupBy(x => x.MenuDish == null ? "Ingredient / other" : x.MenuDish.Name)
            .Select(x => new WasteBreakdown(x.Key, x.Sum(y => y.Quantity), x.Sum(y => y.Quantity * y.UnitCost)))
            .OrderByDescending(x => x.Cost).ToListAsync();
    }

    public async Task<IReadOnlyList<WasteBreakdown>> ByMealAsync(DateTime from, DateTime to)
    {
        return await db.WasteRecords.AsNoTracking()
            .Where(x => x.WasteDate >= from.Date && x.WasteDate <= to.Date && x.Meal.HasValue)
            .GroupBy(x => x.Meal!.Value)
            .Select(x => new WasteBreakdown(x.Key.ToString(), x.Sum(y => y.Quantity), x.Sum(y => y.Quantity * y.UnitCost)))
            .OrderByDescending(x => x.Cost).ToListAsync();
    }
}

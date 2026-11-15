using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web_ca_3.Models;

namespace web_ca_3.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<MenuDish> MenuDishes => Set<MenuDish>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<ForecastRecord> ForecastRecords => Set<ForecastRecord>();
    public DbSet<ServiceActual> ServiceActuals => Set<ServiceActual>();
    public DbSet<WasteRecord> WasteRecords => Set<WasteRecord>();
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<MealBooking> MealBookings => Set<MealBooking>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<PreparationPlan> PreparationPlans => Set<PreparationPlan>();
    public DbSet<PreparationPlanLine> PreparationPlanLines => Set<PreparationPlanLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RecipeIngredient>().HasIndex(x => new { x.MenuDishId, x.IngredientId }).IsUnique();
        builder.Entity<Ingredient>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<MenuDish>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<AttendanceRecord>().HasIndex(x => new { x.ServiceDate, x.Meal, x.HallId }).IsUnique();
        builder.Entity<ForecastRecord>().HasIndex(x => new { x.ForecastDate, x.Meal, x.HallId }).IsUnique();
        builder.Entity<ServiceActual>().HasIndex(x => new { x.ServiceDate, x.Meal, x.HallId }).IsUnique();
        builder.Entity<Hall>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<MealBooking>().HasIndex(x => new { x.StudentEmail, x.BookingDate, x.Meal }).IsUnique();
        builder.Entity<Ingredient>().ToTable(t => t.HasCheckConstraint("CK_Ingredients_UnitCost", "[UnitCost] >= 0"));
        builder.Entity<Ingredient>().ToTable(t => t.HasCheckConstraint("CK_Ingredients_CurrentStock", "[CurrentStock] >= 0"));
        builder.Entity<WasteRecord>().ToTable(t => t.HasCheckConstraint("CK_WasteRecords_Quantity", "[Quantity] >= 0"));
        builder.Entity<ServiceActual>().ToTable(t => t.HasCheckConstraint("CK_ServiceActuals_Counts", "[PreparedCount] >= 0 AND [ServedCount] >= 0"));
        builder.Entity<WasteRecord>().Property(x => x.UnitCost).HasPrecision(18, 2);
        builder.Entity<Ingredient>().Property(x => x.UnitCost).HasPrecision(18, 2);
        builder.Entity<Ingredient>().Property(x => x.CurrentStock).HasPrecision(18, 2);
        builder.Entity<RecipeIngredient>().Property(x => x.QuantityPerServing).HasPrecision(18, 3);
        builder.Entity<WasteRecord>().Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Entity<ForecastRecord>().Property(x => x.Confidence).HasPrecision(5, 2);
        builder.Entity<PreparationPlan>().HasIndex(x => new { x.PlanDate, x.Meal, x.HallId }).IsUnique();
        builder.Entity<PreparationPlan>().Property(x => x.SafetyMarginPercent).HasPrecision(5, 2);
        builder.Entity<PreparationPlanLine>().Property(x => x.RequiredQuantity).HasPrecision(18, 3);
        builder.Entity<PreparationPlanLine>().Property(x => x.AvailableQuantity).HasPrecision(18, 3);
        builder.Entity<PreparationPlanLine>().Property(x => x.ShortageQuantity).HasPrecision(18, 3);
    }
}

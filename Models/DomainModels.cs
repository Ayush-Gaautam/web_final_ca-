using System.ComponentModel.DataAnnotations;

namespace web_ca_3.Models;

public enum MealType { Breakfast, Lunch, Dinner, Snack }
public enum WasteReason { Overproduction, Spoilage, PlateWaste, PreparationLoss, Other }
public enum BookingStatus { Booked, Cancelled, Served }

public class MenuDish
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(40)] public string Category { get; set; } = "Main";
    [Range(0, 10000)] public int Calories { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}

public class Ingredient
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(20)] public string Unit { get; set; } = "kg";
    [Range(typeof(decimal), "0", "1000000")] public decimal UnitCost { get; set; }
    [Range(typeof(decimal), "0", "1000000")] public decimal CurrentStock { get; set; }
    public ICollection<RecipeIngredient> Recipes { get; set; } = new List<RecipeIngredient>();
}

public class RecipeIngredient
{
    public int Id { get; set; }
    public int MenuDishId { get; set; }
    public MenuDish MenuDish { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    [Range(typeof(decimal), "0.0001", "1000000")] public decimal QuantityPerServing { get; set; }
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    public int? HallId { get; set; }
    public Hall? Hall { get; set; }
    [Range(0, 100000)] public int ExpectedCount { get; set; }
    [Range(0, 100000)] public int ActualCount { get; set; }
    [StringLength(200)] public string? Notes { get; set; }
}

public class CalendarEvent
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string Title { get; set; } = string.Empty;
    public DateTime EventDate { get; set; } = DateTime.Today;
    [StringLength(500)] public string? Description { get; set; }
    public int AttendanceAdjustment { get; set; }
}

public class ForecastRecord
{
    public int Id { get; set; }
    public DateTime ForecastDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    public int? HallId { get; set; }
    public Hall? Hall { get; set; }
    public int ForecastCount { get; set; }
    public decimal Confidence { get; set; }
    [StringLength(300)] public string Method { get; set; } = "Weighted moving average";
}

public class ServiceActual
{
    public int Id { get; set; }
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    public int? HallId { get; set; }
    public Hall? Hall { get; set; }
    [Range(0, 100000)] public int PreparedCount { get; set; }
    [Range(0, 100000)] public int ServedCount { get; set; }
    public int LeftoverCount { get; set; }
}

public class WasteRecord
{
    public int Id { get; set; }
    public DateTime WasteDate { get; set; } = DateTime.Today;
    public MealType? Meal { get; set; }
    public int? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public int? MenuDishId { get; set; }
    public MenuDish? MenuDish { get; set; }
    [Range(typeof(decimal), "0.0001", "1000000")] public decimal Quantity { get; set; }
    [Range(typeof(decimal), "0", "1000000")] public decimal UnitCost { get; set; }
    public WasteReason Reason { get; set; }
    [StringLength(300)] public string? Notes { get; set; }
    public decimal Cost => Quantity * UnitCost;
}

public class Hall
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Range(1, 100000)] public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MealBooking
{
    public int Id { get; set; }
    [Required, StringLength(256)] public string StudentEmail { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    public int? HallId { get; set; }
    public Hall? Hall { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Booked;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Feedback
{
    public int Id { get; set; }
    [Required, StringLength(256)] public string StudentEmail { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    [Range(1, 5)] public int Rating { get; set; }
    [Required, StringLength(1000)] public string Comment { get; set; } = string.Empty;
}

public class PreparationPlan
{
    public int Id { get; set; }
    public DateTime PlanDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    public int? HallId { get; set; }
    public Hall? Hall { get; set; }
    public int PredictedServings { get; set; }
    public decimal SafetyMarginPercent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<PreparationPlanLine> Lines { get; set; } = new List<PreparationPlanLine>();
}

public class PreparationPlanLine
{
    public int Id { get; set; }
    public int PreparationPlanId { get; set; }
    public PreparationPlan PreparationPlan { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
}

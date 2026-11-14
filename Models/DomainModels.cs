using System.ComponentModel.DataAnnotations;

namespace web_ca_3.Models;

public enum MealType { Breakfast, Lunch, Dinner, Snack }
public enum WasteReason { Overproduction, Spoilage, PlateWaste, PreparationLoss, Other }

public class MenuDish
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(40)] public string Category { get; set; } = "Main";
    public int Calories { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}

public class Ingredient
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(20)] public string Unit { get; set; } = "kg";
    public decimal UnitCost { get; set; }
    public decimal CurrentStock { get; set; }
    public ICollection<RecipeIngredient> Recipes { get; set; } = new List<RecipeIngredient>();
}

public class RecipeIngredient
{
    public int Id { get; set; }
    public int MenuDishId { get; set; }
    public MenuDish MenuDish { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal QuantityPerServing { get; set; }
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
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
    public int ForecastCount { get; set; }
    public decimal Confidence { get; set; }
    [StringLength(300)] public string Method { get; set; } = "Weighted moving average";
}

public class ServiceActual
{
    public int Id { get; set; }
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    public int PreparedCount { get; set; }
    public int ServedCount { get; set; }
    public int LeftoverCount { get; set; }
}

public class WasteRecord
{
    public int Id { get; set; }
    public DateTime WasteDate { get; set; } = DateTime.Today;
    public int? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public int? MenuDishId { get; set; }
    public MenuDish? MenuDish { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public WasteReason Reason { get; set; }
    [StringLength(300)] public string? Notes { get; set; }
    public decimal Cost => Quantity * UnitCost;
}

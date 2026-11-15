using System.ComponentModel.DataAnnotations;

namespace web_ca_3.Models;

public sealed record DishDto(int Id, string Name, string Category, int Calories, bool IsActive, IReadOnlyList<RecipeIngredientDto> Ingredients);
public sealed record RecipeIngredientDto(int IngredientId, string Name, string Unit, decimal QuantityPerServing, decimal UnitCost);

public sealed class DishRequest
{
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(40)] public string Category { get; set; } = "Main";
    [Range(0, 10000)] public int Calories { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class IngredientRequest
{
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Unit { get; set; } = "kg";
    [Range(typeof(decimal), "0", "1000000")] public decimal UnitCost { get; set; }
    [Range(typeof(decimal), "0", "1000000")] public decimal CurrentStock { get; set; }
}

public sealed class AttendanceRequest
{
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    [Range(0, 100000)] public int ExpectedCount { get; set; }
    [Range(0, 100000)] public int ActualCount { get; set; }
    [StringLength(200)] public string? Notes { get; set; }
}

public sealed class EventRequest
{
    [Required, StringLength(160)] public string Title { get; set; } = string.Empty;
    public DateTime EventDate { get; set; } = DateTime.Today;
    [StringLength(500)] public string? Description { get; set; }
    [Range(-100000, 100000)] public int AttendanceAdjustment { get; set; }
}

public sealed class ActualRequest
{
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    [Range(0, 100000)] public int PreparedCount { get; set; }
    [Range(0, 100000)] public int ServedCount { get; set; }
}

public sealed class WasteRequest
{
    public DateTime WasteDate { get; set; } = DateTime.Today;
    public int? IngredientId { get; set; }
    public int? MenuDishId { get; set; }
    [Range(typeof(decimal), "0.0001", "1000000")] public decimal Quantity { get; set; }
    public WasteReason Reason { get; set; }
    [StringLength(300)] public string? Notes { get; set; }
}

public sealed class BookingRequest
{
    public DateTime BookingDate { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
}

public sealed class FeedbackRequest
{
    [Range(1, 5)] public int Rating { get; set; }
    [Required, StringLength(1000)] public string Comment { get; set; } = string.Empty;
}

public sealed class HallRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Range(1, 100000)] public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}

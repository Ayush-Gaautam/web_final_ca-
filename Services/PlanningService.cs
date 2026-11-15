using web_ca_3.Models;

namespace web_ca_3.Services;

public sealed class PlanningService
{
    public IReadOnlyDictionary<string, decimal> IngredientPlan(MenuDish dish, int servings) =>
        dish.Ingredients.ToDictionary(x => x.Ingredient.Name, x => x.QuantityPerServing * servings);

    public PreparationPlan BuildPlan(DateTime date, MealType meal, int predictedServings,
        IEnumerable<(MenuDish Dish, int Servings)> dishes, decimal safetyMarginPercent, Hall? hall = null)
    {
        var quantities = dishes
            .SelectMany(x => x.Dish.Ingredients.Select(recipe => new
            {
                recipe.Ingredient,
                Quantity = recipe.QuantityPerServing * x.Servings
            }))
            .GroupBy(x => x.Ingredient.Id)
            .Select(group =>
            {
                var ingredient = group.First().Ingredient;
                var required = Math.Round(group.Sum(x => x.Quantity) * (1 + safetyMarginPercent / 100m), 3);
                return new PreparationPlanLine
                {
                    Ingredient = ingredient,
                    IngredientId = ingredient.Id,
                    RequiredQuantity = required,
                    AvailableQuantity = ingredient.CurrentStock,
                    ShortageQuantity = Math.Max(0, required - ingredient.CurrentStock)
                };
            }).ToList();

        return new PreparationPlan
        {
            PlanDate = date.Date,
            Meal = meal,
            Hall = hall,
            HallId = hall?.Id,
            PredictedServings = predictedServings,
            SafetyMarginPercent = safetyMarginPercent,
            Lines = quantities
        };
    }
}

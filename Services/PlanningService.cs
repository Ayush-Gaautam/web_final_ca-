using web_ca_3.Models;

namespace web_ca_3.Services;

public sealed class PlanningService
{
    public IReadOnlyDictionary<string, decimal> IngredientPlan(MenuDish dish, int servings) =>
        dish.Ingredients.ToDictionary(x => x.Ingredient.Name, x => x.QuantityPerServing * servings);
}

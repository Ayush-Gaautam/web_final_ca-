using System;
using System.Collections.Generic;
using System.Linq;
using web_ca_3.Models;
using web_ca_3.Services;
using Xunit;

namespace web_ca_3.Tests;

public class ForecastingTests
{
    [Fact]
    public void Forecast_UsesRecentWeightedHistoryAndEventAdjustment()
    {
        var service = new ForecastingService();
        var result = service.Forecast(new[]
        {
            new AttendanceRecord { ServiceDate = DateTime.Today.AddDays(-1), Meal = MealType.Lunch, ActualCount = 100 },
            new AttendanceRecord { ServiceDate = DateTime.Today.AddDays(-2), Meal = MealType.Lunch, ActualCount = 80 }
        }, new[] { new CalendarEvent { EventDate = DateTime.Today, AttendanceAdjustment = 10 } }, DateTime.Today, MealType.Lunch);
        Assert.Equal(103, result);
    }

    [Fact]
    public void WasteCost_IsQuantityTimesUnitCost()
    {
        var waste = new WasteRecord { Quantity = 2.5m, UnitCost = 120m };
        Assert.Equal(300m, waste.Cost);
    }

    [Fact]
    public void Forecast_ReportsMaeAndMape()
    {
        var service = new ForecastingService();
        var records = new[]
        {
            new AttendanceRecord { Meal = MealType.Lunch, ExpectedCount = 100, ActualCount = 90 },
            new AttendanceRecord { Meal = MealType.Lunch, ExpectedCount = 120, ActualCount = 100 }
        };
        Assert.Equal(15m, service.MeanAbsoluteError(records));
        Assert.Equal(15.56m, service.MeanAbsolutePercentageError(records));
    }

    [Fact]
    public void Forecast_ExcludesFutureRecords_AndCanFilterByHall()
    {
        var service = new ForecastingService();
        var date = new DateTime(2026, 11, 20);
        var history = new[]
        {
            new AttendanceRecord { ServiceDate = date.AddDays(-1), Meal = MealType.Lunch, HallId = 1, ActualCount = 40 },
            new AttendanceRecord { ServiceDate = date.AddDays(-1), Meal = MealType.Lunch, HallId = 2, ActualCount = 100 },
            new AttendanceRecord { ServiceDate = date.AddDays(1), Meal = MealType.Lunch, HallId = 1, ActualCount = 1000 }
        };

        Assert.Equal(40, service.Analyze(history, Array.Empty<CalendarEvent>(), date, MealType.Lunch, 1).Forecast);
    }

    [Fact]
    public void PreparationPlan_AggregatesSharedIngredientsAndCalculatesShortage()
    {
        var rice = new Ingredient { Id = 1, Name = "Rice", CurrentStock = 5, Unit = "kg" };
        var first = new MenuDish { Id = 1, Name = "Dish A", Ingredients = new List<RecipeIngredient>
        {
            new() { Ingredient = rice, IngredientId = 1, QuantityPerServing = .1m }
        }};
        var second = new MenuDish { Id = 2, Name = "Dish B", Ingredients = new List<RecipeIngredient>
        {
            new() { Ingredient = rice, IngredientId = 1, QuantityPerServing = .2m }
        }};

        var plan = new PlanningService().BuildPlan(DateTime.Today, MealType.Lunch, 40,
            new[] { (first, 20), (second, 20) }, 10);

        Assert.Single(plan.Lines);
        Assert.Equal(6.6m, plan.Lines.Single().RequiredQuantity);
        Assert.Equal(1.6m, plan.Lines.Single().ShortageQuantity);
    }
}

using System;
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
}

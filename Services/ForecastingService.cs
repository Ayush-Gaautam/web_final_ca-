using web_ca_3.Models;

namespace web_ca_3.Services;

public sealed record ForecastResult(DateTime Date, MealType Meal, int Forecast, decimal Confidence, decimal Mae, decimal Mape, string Method);

public sealed class ForecastingService
{
    public ForecastResult Analyze(IEnumerable<AttendanceRecord> history, IEnumerable<CalendarEvent> events, DateTime date, MealType meal, int? hallId = null)
    {
        var records = history.Where(x => x.Meal == meal && x.ServiceDate.Date < date.Date && (!hallId.HasValue || x.HallId == hallId))
            .OrderByDescending(x => x.ServiceDate).Take(28).ToArray();
        var samples = records.Select(x => x.ActualCount > 0 ? x.ActualCount : x.ExpectedCount).ToArray();
        var weekdaySamples = records.Where(x => x.ServiceDate.DayOfWeek == date.DayOfWeek)
            .Select(x => x.ActualCount > 0 ? x.ActualCount : x.ExpectedCount).ToArray();
        var baseline = WeightedAverage(samples.Length == 0 ? new[] { 50 } : samples);
        var weekdayBaseline = weekdaySamples.Length == 0 ? baseline : WeightedAverage(weekdaySamples);
        var weekdayAdjustment = (weekdayBaseline - baseline) * 0.35m;
        var eventAdjustment = events.Where(x => x.EventDate.Date == date.Date).Sum(x => x.AttendanceAdjustment);
        var forecast = Math.Max(0, (int)Math.Round(baseline + weekdayAdjustment + eventAdjustment));
        var mae = MeanAbsoluteError(records);
        var mape = MeanAbsolutePercentageError(records);
        var confidence = Math.Min(0.98m, 0.50m + records.Length * 0.015m + (weekdaySamples.Length > 0 ? 0.10m : 0));
        return new ForecastResult(date.Date, meal, forecast, confidence, mae, mape, "Weighted moving average + weekday/event adjustments");
    }

    public int Forecast(IEnumerable<AttendanceRecord> history, IEnumerable<CalendarEvent> events, DateTime date, MealType meal, int? hallId = null) =>
        Analyze(history, events, date, meal, hallId).Forecast;

    public decimal Confidence(int sampleCount) => Math.Min(0.98m, 0.50m + sampleCount * 0.015m);

    public decimal MeanAbsoluteError(IEnumerable<AttendanceRecord> records)
    {
        var values = records.Where(x => x.ActualCount > 0 && x.ExpectedCount > 0)
            .Select(x => Math.Abs(x.ActualCount - x.ExpectedCount)).ToArray();
        return values.Length == 0 ? 0 : Math.Round((decimal)values.Average(), 2);
    }

    public decimal MeanAbsolutePercentageError(IEnumerable<AttendanceRecord> records)
    {
        var values = records.Where(x => x.ActualCount > 0 && x.ExpectedCount > 0)
            .Select(x => Math.Abs((decimal)x.ActualCount - x.ExpectedCount) / x.ActualCount * 100).ToArray();
        return values.Length == 0 ? 0 : Math.Round(values.Average(), 2);
    }

    private static decimal WeightedAverage(IReadOnlyList<int> values)
    {
        var denominator = values.Select((_, index) => values.Count - index).Sum();
        return values.Select((value, index) => value * (values.Count - index)).Sum() / (decimal)denominator;
    }
}

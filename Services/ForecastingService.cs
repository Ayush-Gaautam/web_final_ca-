using web_ca_3.Models;

namespace web_ca_3.Services;

public sealed class ForecastingService
{
    public int Forecast(IEnumerable<AttendanceRecord> history, IEnumerable<CalendarEvent> events, DateTime date, MealType meal)
    {
        var samples = history.Where(x => x.Meal == meal && x.ServiceDate < date)
            .OrderByDescending(x => x.ServiceDate).Take(7).Select(x => x.ActualCount > 0 ? x.ActualCount : x.ExpectedCount).ToArray();
        var baseline = samples.Length == 0 ? 50 : samples.Select((value, index) => value * (samples.Length - index)).Sum() / samples.Select((_, index) => samples.Length - index).Sum();
        var adjustment = events.Where(x => x.EventDate.Date == date.Date).Sum(x => x.AttendanceAdjustment);
        return Math.Max(0, (int)Math.Round((decimal)baseline + adjustment));
    }

    public decimal Confidence(int sampleCount) => Math.Min(0.95m, 0.55m + sampleCount * 0.05m);
}

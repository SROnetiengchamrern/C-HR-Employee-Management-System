namespace HrEmployeeManagementSystem.Api.Services;

public static class DateRules
{
    public static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    public static bool MonthDayFallsIn(DateOnly source, DateOnly start, DateOnly end)
    {
        if (end < start)
            return false;

        foreach (var year in new HashSet<int> { start.Year, end.Year })
        {
            var day = Math.Min(source.Day, DateTime.DaysInMonth(year, source.Month));
            var occurrence = new DateOnly(year, source.Month, day);
            if (occurrence >= start && occurrence <= end)
                return true;
        }

        return false;
    }

    public static int InclusiveDays(DateOnly start, DateOnly end) => end.DayNumber - start.DayNumber + 1;

    public static bool RangesOverlap(DateOnly aStart, DateOnly aEnd, DateOnly bStart, DateOnly bEnd) =>
        aStart <= bEnd && bStart <= aEnd;

    public static int AnniversaryYears(DateOnly hireDate, DateOnly weekStart, DateOnly weekEnd)
    {
        foreach (var year in new HashSet<int> { weekStart.Year, weekEnd.Year })
        {
            var day = Math.Min(hireDate.Day, DateTime.DaysInMonth(year, hireDate.Month));
            var occurrence = new DateOnly(year, hireDate.Month, day);
            if (occurrence >= weekStart && occurrence <= weekEnd)
                return year - hireDate.Year;
        }

        return 0;
    }
}

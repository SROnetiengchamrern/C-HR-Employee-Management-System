using HrEmployeeManagementSystem.Api.Services;

namespace HrEmployeeManagementSystem.Api.Tests;

public class DateRulesTests
{
    [Fact]
    public void Week_starts_on_monday()
    {
        var tuesday = new DateOnly(2026, 10, 27);
        Assert.Equal(new DateOnly(2026, 10, 26), DateRules.StartOfWeek(tuesday));
        Assert.Equal(new DateOnly(2026, 10, 26), DateRules.StartOfWeek(new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void Inclusive_leave_days_count_both_ends()
    {
        Assert.Equal(2, DateRules.InclusiveDays(new DateOnly(2027, 4, 12), new DateOnly(2027, 4, 13)));
        Assert.True(DateRules.RangesOverlap(
            new DateOnly(2027, 4, 12), new DateOnly(2027, 4, 13),
            new DateOnly(2027, 4, 13), new DateOnly(2027, 4, 15)));
        Assert.False(DateRules.RangesOverlap(
            new DateOnly(2027, 4, 12), new DateOnly(2027, 4, 13),
            new DateOnly(2027, 4, 14), new DateOnly(2027, 4, 15)));
    }
}

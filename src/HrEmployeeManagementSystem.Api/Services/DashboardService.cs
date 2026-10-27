using HrEmployeeManagementSystem.Api.Data;
using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HrEmployeeManagementSystem.Api.Services;

public class DashboardService(AppDbContext db)
{
    public async Task<DashboardResponse> GetAsync(int? hiresYear)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var employees = await db.Employees.AsNoTracking().Include(e => e.Department).ToListAsync();
        var total = employees.Count;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var hiredThisMonth = employees.Count(e => e.HireDate >= monthStart && e.HireDate <= today);
        var previous = total - hiredThisMonth;
        var change = previous <= 0 ? 0 : Math.Round(hiredThisMonth * 100.0 / previous, 1);

        var present = await db.AttendanceRecords.CountAsync(a => a.WorkDate == today && a.CheckIn != null);
        var attendance = total == 0 ? 0 : Math.Round(present * 100.0 / total, 1);

        var pending = await db.LeaveRequests.AsNoTracking()
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .Include(l => l.LeaveType)
            .Where(l => l.Status == LeaveStatus.Pending)
            .OrderBy(l => l.StartDate)
            .ToListAsync();

        var openPositions = await db.JobOpenings.Where(j => j.Status == "Active").SumAsync(j => (int?)j.Openings) ?? 0;

        var departments = employees
            .GroupBy(e => e.DepartmentId)
            .Select(group =>
            {
                var department = group.First().Department;
                return new DepartmentCountDto(department.Id, department.Name, department.Code, group.Count());
            })
            .OrderBy(d => d.Name)
            .ToList();

        var weekStart = DateRules.StartOfWeek(today);
        var weekEnd = weekStart.AddDays(6);
        var birthdays = employees
            .Where(e => DateRules.MonthDayFallsIn(e.DateOfBirth, weekStart, weekEnd))
            .OrderBy(e => e.FirstName)
            .ToList();
        var anniversaries = employees
            .Where(e => DateRules.MonthDayFallsIn(e.HireDate, weekStart, weekEnd)
                && DateRules.AnniversaryYears(e.HireDate, weekStart, weekEnd) >= 5)
            .OrderBy(e => e.FirstName)
            .ToList();

        var sessions = await db.TrainingSessions.AsNoTracking().OrderBy(t => t.StartsAt).ToListAsync();
        var upcoming = sessions.Where(t => DateOnly.FromDateTime(t.StartsAt) >= today).Take(2).ToList();
        if (upcoming.Count == 0)
            upcoming = sessions.TakeLast(1).ToList();

        var distribution = new int[12];
        foreach (var employee in employees)
            distribution[employee.DateOfBirth.Month - 1]++;

        var years = employees.Select(e => e.HireDate.Year).Distinct().OrderByDescending(y => y).ToList();
        if (!years.Contains(today.Year))
            years.Insert(0, today.Year);
        var year = hiresYear is int requested && years.Contains(requested) ? requested : today.Year;
        var hires = Enumerable.Range(1, 12)
            .Select(month => new MonthCountDto(month, employees.Count(e => e.HireDate.Year == year && e.HireDate.Month == month)))
            .ToList();

        return new DashboardResponse(
            total,
            change,
            present,
            attendance,
            pending.Count,
            openPositions,
            departments,
            birthdays.Count,
            birthdays.Select(e => e.FullName).Take(8).ToList(),
            anniversaries.Count,
            anniversaries.Select(e => $"{e.FullName} · {DateRules.AnniversaryYears(e.HireDate, weekStart, weekEnd)} years").Take(8).ToList(),
            upcoming.Select(t => new TrainingItemDto(t.Id, t.Title, t.StartsAt, t.Location, DateOnly.FromDateTime(t.StartsAt) == today)).ToList(),
            distribution,
            pending.Take(6).Select(Mappers.ToLeave).ToList(),
            hires,
            year,
            years);
    }
}

using System.Text;
using HrEmployeeManagementSystem.Api.Data;
using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HrEmployeeManagementSystem.Api.Services;

public class ReportService(AppDbContext db)
{
    public async Task<HeadcountReportDto> HeadcountAsync()
    {
        var employees = await db.Employees.AsNoTracking().Include(e => e.Department).ToListAsync();
        var byDepartment = employees
            .GroupBy(e => e.DepartmentId)
            .Select(group =>
            {
                var department = group.First().Department;
                return new DepartmentCountDto(department.Id, department.Name, department.Code, group.Count());
            })
            .OrderByDescending(d => d.Count)
            .ThenBy(d => d.Name)
            .ToList();

        return new HeadcountReportDto(
            employees.Count,
            employees.Count(e => e.Status == EmploymentStatus.Active),
            employees.Count(e => e.Status == EmploymentStatus.Probation),
            employees.Count(e => e.Status == EmploymentStatus.OnLeave),
            employees.Count(e => e.Status == EmploymentStatus.Resigned),
            employees.Count(e => e.Status == EmploymentStatus.Terminated),
            byDepartment);
    }

    public async Task<AttendanceReportDto> AttendanceAsync(DateOnly from, DateOnly to)
    {
        var rows = await db.AttendanceRecords.AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e.Department)
            .Where(a => a.WorkDate >= from && a.WorkDate <= to)
            .OrderByDescending(a => a.WorkDate)
            .ThenBy(a => a.Employee.EmployeeCode)
            .Take(5000)
            .ToListAsync();

        var mapped = rows.Select(Mappers.ToAttendance).ToList();
        var hours = mapped.Where(r => r.Hours is not null).Select(r => r.Hours!.Value).ToList();
        var average = hours.Count == 0 ? 0 : Math.Round(hours.Average(), 2);
        return new AttendanceReportDto(from, to, mapped.Count, average, mapped);
    }

    public async Task<LeaveReportDto> LeaveAsync(DateOnly from, DateOnly to)
    {
        var rows = await db.LeaveRequests.AsNoTracking()
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .Include(l => l.LeaveType)
            .Where(l => l.StartDate <= to && l.EndDate >= from)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5000)
            .ToListAsync();

        var mapped = rows.Select(Mappers.ToLeave).ToList();
        return new LeaveReportDto(
            from,
            to,
            mapped.Count(r => r.Status == nameof(LeaveStatus.Pending)),
            mapped.Count(r => r.Status == nameof(LeaveStatus.Approved)),
            mapped.Count(r => r.Status == nameof(LeaveStatus.Rejected)),
            mapped.Count(r => r.Status == nameof(LeaveStatus.Cancelled)),
            mapped.Where(r => r.Status == nameof(LeaveStatus.Approved)).Sum(r => r.Days),
            mapped);
    }

    public async Task<byte[]> HeadcountCsvAsync()
    {
        var report = await HeadcountAsync();
        var builder = new StringBuilder();
        builder.AppendLine("Department,Code,Employees");
        foreach (var row in report.ByDepartment)
            builder.AppendLine($"{Cell(row.Name)},{Cell(row.Code)},{row.Count}");
        builder.AppendLine();
        builder.AppendLine("Status,Employees");
        builder.AppendLine($"Active,{report.Active}");
        builder.AppendLine($"Probation,{report.Probation}");
        builder.AppendLine($"On leave,{report.OnLeave}");
        builder.AppendLine($"Resigned,{report.Resigned}");
        builder.AppendLine($"Terminated,{report.Terminated}");
        builder.AppendLine($"Total,{report.Total}");
        return Encode(builder.ToString());
    }

    public async Task<byte[]> AttendanceCsvAsync(DateOnly from, DateOnly to)
    {
        var report = await AttendanceAsync(from, to);
        var builder = new StringBuilder();
        builder.AppendLine("Employee Code,Employee,Department,Date,Check In,Check Out,Hours");
        foreach (var row in report.Rows)
        {
            builder.AppendLine(string.Join(',',
                Cell(row.EmployeeCode),
                Cell(row.EmployeeName),
                Cell(row.Department),
                row.WorkDate.ToString("yyyy-MM-dd"),
                row.CheckIn?.ToString("yyyy-MM-dd HH:mm") ?? "",
                row.CheckOut?.ToString("yyyy-MM-dd HH:mm") ?? "",
                row.Hours?.ToString("0.00") ?? ""));
        }
        return Encode(builder.ToString());
    }

    public async Task<byte[]> LeaveCsvAsync(DateOnly from, DateOnly to)
    {
        var report = await LeaveAsync(from, to);
        var builder = new StringBuilder();
        builder.AppendLine("Employee,Department,Type,Start,End,Days,Status,Reason");
        foreach (var row in report.Rows)
        {
            builder.AppendLine(string.Join(',',
                Cell(row.EmployeeName),
                Cell(row.Department),
                Cell(row.LeaveType),
                row.StartDate.ToString("yyyy-MM-dd"),
                row.EndDate.ToString("yyyy-MM-dd"),
                row.Days,
                row.Status,
                Cell(row.Reason)));
        }
        return Encode(builder.ToString());
    }

    private static string Cell(string? value)
    {
        var text = value ?? "";
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            return $"\"{text.Replace("\"", "\"\"")}\"";
        return text;
    }

    private static byte[] Encode(string csv)
    {
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(csv);
        var bytes = new byte[preamble.Length + body.Length];
        preamble.CopyTo(bytes, 0);
        body.CopyTo(bytes, preamble.Length);
        return bytes;
    }
}

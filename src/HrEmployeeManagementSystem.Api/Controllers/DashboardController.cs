using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;
using HrEmployeeManagementSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrEmployeeManagementSystem.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public Task<DashboardResponse> Get([FromQuery] int? hiresYear) => dashboard.GetAsync(hiresYear);
}

[ApiController]
[Route("api/reports")]
[Authorize(Roles = AppRoles.ReportReaders)]
public class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("headcount")]
    public Task<HeadcountReportDto> Headcount() => reports.HeadcountAsync();

    [HttpGet("attendance")]
    public async Task<ActionResult<AttendanceReportDto>> Attendance([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (!TryRange(from, to, out var start, out var end, out var error))
            return error!;
        return await reports.AttendanceAsync(start, end);
    }

    [HttpGet("leave")]
    public async Task<ActionResult<LeaveReportDto>> Leave([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (!TryRange(from, to, out var start, out var end, out var error))
            return error!;
        return await reports.LeaveAsync(start, end);
    }

    [HttpGet("headcount.csv")]
    public async Task<IActionResult> HeadcountCsv() =>
        File(await reports.HeadcountCsvAsync(), "text/csv", "headcount.csv");

    [HttpGet("attendance.csv")]
    public async Task<IActionResult> AttendanceCsv([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (!TryRange(from, to, out var start, out var end, out var error))
            return error!;
        return File(await reports.AttendanceCsvAsync(start, end), "text/csv", "attendance.csv");
    }

    [HttpGet("leave.csv")]
    public async Task<IActionResult> LeaveCsv([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (!TryRange(from, to, out var start, out var end, out var error))
            return error!;
        return File(await reports.LeaveCsvAsync(start, end), "text/csv", "leave.csv");
    }

    private bool TryRange(DateOnly? from, DateOnly? to, out DateOnly start, out DateOnly end, out ActionResult? error)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        start = from ?? new DateOnly(today.Year, today.Month, 1);
        end = to ?? today;
        if (end < start)
        {
            error = BadRequest(new { message = "The end date must be on or after the start date." });
            return false;
        }

        error = null;
        return true;
    }
}

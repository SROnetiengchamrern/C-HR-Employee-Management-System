using HrEmployeeManagementSystem.Api.Data;
using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;
using HrEmployeeManagementSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrEmployeeManagementSystem.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceResponse>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId)
    {
        var start = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-14));
        var end = to ?? DateOnly.FromDateTime(DateTime.Today);
        if (end < start)
            return BadRequest(new { message = "The end date must be on or after the start date." });

        if (employeeId is int requested && !User.CanAccessEmployee(requested))
            return Forbid();

        var query = db.AttendanceRecords.AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e.Department)
            .Where(a => a.WorkDate >= start && a.WorkDate <= end);

        if (!User.IsHrStaff())
        {
            var ownId = User.EmployeeId();
            if (ownId is null)
                return new List<AttendanceResponse>();
            query = query.Where(a => a.EmployeeId == ownId);
        }
        else if (employeeId is int id)
        {
            query = query.Where(a => a.EmployeeId == id);
        }

        if (departmentId is int department)
            query = query.Where(a => a.Employee.DepartmentId == department);

        var rows = await query
            .OrderByDescending(a => a.WorkDate)
            .ThenBy(a => a.Employee.EmployeeCode)
            .Take(1000)
            .ToListAsync();

        return rows.Select(Mappers.ToAttendance).ToList();
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceResponse>> CheckIn(AttendanceActionRequest request)
    {
        var employee = await ResolveEmployee(request.EmployeeId);
        if (employee.Result is not null)
            return employee.Result;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var existing = await db.AttendanceRecords.FirstOrDefaultAsync(a => a.EmployeeId == employee.Value!.Id && a.WorkDate == today);
        if (existing is not null)
            return Conflict(new { message = "This employee is already checked in today." });

        var record = new AttendanceRecord
        {
            EmployeeId = employee.Value!.Id,
            WorkDate = today,
            CheckIn = DateTime.Now,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync();
        await db.Entry(record).Reference(a => a.Employee).Query().Include(e => e.Department).LoadAsync();
        return Mappers.ToAttendance(record);
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<AttendanceResponse>> CheckOut(AttendanceActionRequest request)
    {
        var employee = await ResolveEmployee(request.EmployeeId);
        if (employee.Result is not null)
            return employee.Result;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var record = await db.AttendanceRecords
            .Include(a => a.Employee).ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Value!.Id && a.WorkDate == today);
        if (record?.CheckIn is null)
            return BadRequest(new { message = "Check in before checking out." });
        if (record.CheckOut is not null)
            return Conflict(new { message = "This employee is already checked out today." });

        record.CheckOut = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            record.Notes = request.Notes.Trim();
        await db.SaveChangesAsync();
        return Mappers.ToAttendance(record);
    }

    private async Task<(Employee? Value, ActionResult? Result)> ResolveEmployee(int? requestedId)
    {
        if (requestedId is int id)
        {
            if (!User.CanAccessEmployee(id))
                return (null, Forbid());
            var match = await db.Employees.FirstOrDefaultAsync(e => e.Id == id);
            return match is null
                ? (null, NotFound(new { message = "Employee not found." }))
                : (match, null);
        }

        var ownId = User.EmployeeId();
        var employee = ownId is int claimed
            ? await db.Employees.FirstOrDefaultAsync(e => e.Id == claimed)
            : await db.Employees.FirstOrDefaultAsync(e => e.UserId == User.UserId());
        return employee is null
            ? (null, BadRequest(new { message = "No employee profile is linked to this login." }))
            : (employee, null);
    }
}

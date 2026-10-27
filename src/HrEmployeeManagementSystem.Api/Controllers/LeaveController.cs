using HrEmployeeManagementSystem.Api.Data;
using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;
using HrEmployeeManagementSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrEmployeeManagementSystem.Api.Controllers;

[ApiController]
[Route("api/leave")]
[Authorize]
public class LeaveController(AppDbContext db) : ControllerBase
{
    [HttpGet("types")]
    public async Task<IReadOnlyList<LeaveTypeDto>> Types() =>
        await db.LeaveTypes.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new LeaveTypeDto(t.Id, t.Name, t.DefaultDays))
            .ToListAsync();

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaveResponse>>> List([FromQuery] string? status, [FromQuery] int? employeeId)
    {
        if (employeeId is int requested && !User.CanAccessEmployee(requested))
            return Forbid();

        var query = db.LeaveRequests.AsNoTracking()
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .Include(l => l.LeaveType)
            .AsQueryable();

        if (!User.IsHrStaff() && !User.IsInRole(AppRoles.Manager))
        {
            var ownId = User.EmployeeId();
            if (ownId is null)
                return new List<LeaveResponse>();
            query = query.Where(l => l.EmployeeId == ownId);
        }
        else if (employeeId is int id)
        {
            query = query.Where(l => l.EmployeeId == id);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveStatus>(status, true, out var parsed))
            query = query.Where(l => l.Status == parsed);

        var rows = await query.OrderByDescending(l => l.CreatedAt).Take(500).ToListAsync();
        return rows.Select(Mappers.ToLeave).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<LeaveResponse>> Create(LeaveWriteRequest request)
    {
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "The end date must be on or after the start date." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "A reason is required." });

        Employee? employee;
        if (request.EmployeeId is int id)
        {
            if (!User.CanAccessEmployee(id))
                return Forbid();
            employee = await db.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id);
            if (employee is null)
                return NotFound(new { message = "Employee not found." });
        }
        else
        {
            var ownId = User.EmployeeId();
            employee = ownId is int claimed
                ? await db.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == claimed)
                : await db.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.UserId == User.UserId());
            if (employee is null)
                return BadRequest(new { message = "No employee profile is linked to this login." });
        }

        var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == request.LeaveTypeId);
        if (leaveType is null)
            return BadRequest(new { message = "Choose a leave type." });

        var overlaps = await db.LeaveRequests.AnyAsync(l =>
            l.EmployeeId == employee.Id &&
            l.Status != LeaveStatus.Rejected &&
            l.Status != LeaveStatus.Cancelled &&
            DateRules.RangesOverlap(l.StartDate, l.EndDate, request.StartDate, request.EndDate));
        if (overlaps)
            return Conflict(new { message = "Those dates overlap an existing leave request." });

        var leave = new LeaveRequest
        {
            EmployeeId = employee.Id,
            Employee = employee,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason.Trim(),
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();
        return Mappers.ToLeave(leave);
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = AppRoles.Approvers)]
    public Task<ActionResult<LeaveResponse>> Approve(int id, ReviewLeaveRequest request) =>
        Review(id, LeaveStatus.Approved, request.Comment);

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = AppRoles.Approvers)]
    public Task<ActionResult<LeaveResponse>> Reject(int id, ReviewLeaveRequest request) =>
        Review(id, LeaveStatus.Rejected, request.Comment);

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<LeaveResponse>> Cancel(int id)
    {
        var leave = await Load(id);
        if (leave is null)
            return NotFound(new { message = "Leave request not found." });
        if (!User.CanAccessEmployee(leave.EmployeeId) && !User.IsInRole(AppRoles.Manager))
            return Forbid();
        if (leave.Status != LeaveStatus.Pending)
            return Conflict(new { message = "Only a pending request can be cancelled." });

        leave.Status = LeaveStatus.Cancelled;
        leave.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Mappers.ToLeave(leave);
    }

    private async Task<ActionResult<LeaveResponse>> Review(int id, LeaveStatus status, string? comment)
    {
        var leave = await Load(id);
        if (leave is null)
            return NotFound(new { message = "Leave request not found." });
        if (leave.Status != LeaveStatus.Pending)
            return Conflict(new { message = "This request has already been reviewed." });

        leave.Status = status;
        leave.ReviewComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        leave.ReviewerUserId = User.UserId();
        leave.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Mappers.ToLeave(leave);
    }

    private Task<LeaveRequest?> Load(int id) =>
        db.LeaveRequests
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .Include(l => l.LeaveType)
            .FirstOrDefaultAsync(l => l.Id == id);
}

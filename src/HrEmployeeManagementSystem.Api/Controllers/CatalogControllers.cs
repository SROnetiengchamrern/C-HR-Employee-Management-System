using HrEmployeeManagementSystem.Api.Data;
using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;
using HrEmployeeManagementSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrEmployeeManagementSystem.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<NotificationDto>> List()
    {
        var userId = User.UserId();
        return await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == null || n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.CreatedAt, n.IsRead))
            .ToListAsync();
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.UserId();
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && (n.UserId == null || n.UserId == userId));
        if (notification is null)
            return NotFound(new { message = "Notification not found." });

        notification.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/recruitment")]
[Authorize]
public class RecruitmentController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<JobOpeningDto>> List() =>
        await db.JobOpenings.AsNoTracking()
            .Include(j => j.Department)
            .OrderBy(j => j.Title)
            .Select(j => new JobOpeningDto(j.Id, j.Title, j.DepartmentId, j.Department.Name, j.Openings, j.Status, j.Description))
            .ToListAsync();

    [HttpPost]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<ActionResult<JobOpeningDto>> Create(JobOpeningWriteRequest request)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId);
        if (department is null)
            return BadRequest(new { message = "Department was not found." });

        var opening = new JobOpening
        {
            Title = request.Title.Trim(),
            DepartmentId = department.Id,
            Openings = request.Openings,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            Description = request.Description
        };
        db.JobOpenings.Add(opening);
        await db.SaveChangesAsync();
        return new JobOpeningDto(opening.Id, opening.Title, department.Id, department.Name, opening.Openings, opening.Status, opening.Description);
    }
}

[ApiController]
[Route("api/training")]
[Authorize]
public class TrainingController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<TrainingDto>> List() =>
        await db.TrainingSessions.AsNoTracking()
            .OrderByDescending(t => t.StartsAt)
            .Select(t => new TrainingDto(t.Id, t.Title, t.StartsAt, t.Location, t.Description))
            .ToListAsync();

    [HttpPost]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<ActionResult<TrainingDto>> Create(TrainingWriteRequest request)
    {
        if (request.StartsAt == default)
            return BadRequest(new { message = "A start time is required." });

        var session = new TrainingSession
        {
            Title = request.Title.Trim(),
            StartsAt = request.StartsAt,
            Location = request.Location,
            Description = request.Description
        };
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        return new TrainingDto(session.Id, session.Title, session.StartsAt, session.Location, session.Description);
    }
}

[ApiController]
[Route("api/templates")]
[Authorize]
public class TemplatesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<TemplateDto>> List() =>
        await db.DocumentTemplates.AsNoTracking()
            .OrderBy(t => t.Category).ThenBy(t => t.Name)
            .Select(t => new TemplateDto(t.Id, t.Name, t.Category, t.Body))
            .ToListAsync();
}

[ApiController]
[Route("api/benefits")]
[Authorize]
public class BenefitsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<BenefitDto>> List() =>
        await db.BenefitPlans.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BenefitDto(b.Id, b.Name, b.Description, b.Eligibility))
            .ToListAsync();
}

[ApiController]
[Route("api/performance")]
[Authorize]
public class PerformanceController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<PerformanceDto>> List() =>
        await db.PerformanceReviews.AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .OrderByDescending(r => r.Year)
            .ThenBy(r => r.Employee.FirstName)
            .Select(r => new PerformanceDto(
                r.Id,
                r.EmployeeId,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.Employee.Department.Name,
                r.Year,
                r.Period,
                r.Rating,
                r.Summary,
                r.Status))
            .ToListAsync();
}

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminController(AppDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<UserListItemDto>> List()
    {
        var accounts = await users.Users.OrderBy(u => u.FullName).ToListAsync();
        var employees = await db.Employees.AsNoTracking()
            .Where(e => e.UserId != null)
            .ToDictionaryAsync(e => e.UserId!);

        var result = new List<UserListItemDto>();
        foreach (var account in accounts)
        {
            var roles = await users.GetRolesAsync(account);
            employees.TryGetValue(account.Id, out var employee);
            result.Add(new UserListItemDto(
                account.Id,
                account.FullName,
                account.Email ?? "",
                roles.ToList(),
                account.EmployeeId,
                employee?.EmployeeCode));
        }
        return result;
    }

    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> Create(CreateUserRequest request)
    {
        if (!AppRoles.All.Contains(request.Role))
            return BadRequest(new { message = "Choose a valid role." });

        Employee? employee = null;
        if (request.EmployeeId is int employeeId)
        {
            employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee is null)
                return BadRequest(new { message = "Employee was not found." });
            if (employee.UserId is not null)
                return Conflict(new { message = "That employee already has a login." });
        }

        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            EmailConfirmed = true,
            EmployeeId = employee?.Id
        };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return BadRequest(new { message = string.Join(" ", created.Errors.Select(error => error.Description)) });

        await users.AddToRoleAsync(user, request.Role);
        if (employee is not null)
        {
            employee.UserId = user.Id;
            await db.SaveChangesAsync();
        }

        return new UserListItemDto(user.Id, user.FullName, user.Email ?? "", [request.Role], user.EmployeeId, employee?.EmployeeCode);
    }
}

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { status = "ok", service = "HrEmployeeManagementSystem" });
}

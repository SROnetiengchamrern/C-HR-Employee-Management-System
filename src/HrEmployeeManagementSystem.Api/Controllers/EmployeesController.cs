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
[Route("api/employees")]
[Authorize]
public class EmployeesController(AppDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = db.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .AsQueryable();

        if (departmentId is int department)
            query = query.Where(e => e.DepartmentId == department);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmploymentStatus>(status, true, out var parsed))
            query = query.Where(e => e.Status == parsed);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.FirstName.Contains(term) ||
                e.LastName.Contains(term) ||
                e.Email.Contains(term) ||
                e.EmployeeCode.Contains(term) ||
                (e.FirstName + " " + e.LastName).Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.EmployeeCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<EmployeeResponse>(items.Select(Mappers.ToEmployee).ToList(), total, page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDetailResponse>> Get(int id)
    {
        var employee = await db.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
            return NotFound(new { message = "Employee not found." });

        var year = DateTime.Today.Year;
        var types = await db.LeaveTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        var approved = await db.LeaveRequests.AsNoTracking()
            .Where(l => l.EmployeeId == id && l.Status == LeaveStatus.Approved && l.StartDate.Year == year)
            .ToListAsync();

        var balances = types.Select(type => new LeaveBalanceDto(
            type.Id,
            type.Name,
            type.DefaultDays,
            approved.Where(l => l.LeaveTypeId == type.Id).Sum(l => DateRules.InclusiveDays(l.StartDate, l.EndDate)))).ToList();

        return new EmployeeDetailResponse(Mappers.ToEmployee(employee), balances);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<ActionResult<EmployeeResponse>> Create(EmployeeWriteRequest request)
    {
        if (!Enum.TryParse<EmploymentStatus>(request.Status, true, out var status))
            return BadRequest(new { message = "Status is not recognized." });

        var positionError = await ValidatePlacement(request, null);
        if (positionError is not null)
            return positionError;

        var email = request.Email.Trim();
        if (await db.Employees.AnyAsync(e => e.Email == email))
            return BadRequest(new { message = "An employee with this email already exists." });

        var code = await NextCodeAsync();
        var employee = new Employee { EmployeeCode = code };
        Apply(employee, request, status, email);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        if (request.CreateLogin)
        {
            var loginError = await CreateLoginAsync(employee, request.Password);
            if (loginError is not null)
            {
                db.Employees.Remove(employee);
                await db.SaveChangesAsync();
                return loginError;
            }
        }

        await db.Entry(employee).Reference(e => e.Department).LoadAsync();
        await db.Entry(employee).Reference(e => e.Position).LoadAsync();
        await db.Entry(employee).Reference(e => e.Manager).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = employee.Id }, Mappers.ToEmployee(employee));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<ActionResult<EmployeeResponse>> Update(int id, EmployeeWriteRequest request)
    {
        var employee = await db.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
            return NotFound(new { message = "Employee not found." });

        if (!Enum.TryParse<EmploymentStatus>(request.Status, true, out var status))
            return BadRequest(new { message = "Status is not recognized." });
        if (request.ManagerId == id)
            return BadRequest(new { message = "An employee cannot be their own manager." });

        var positionError = await ValidatePlacement(request, id);
        if (positionError is not null)
            return positionError;

        var email = request.Email.Trim();
        if (await db.Employees.AnyAsync(e => e.Email == email && e.Id != id))
            return BadRequest(new { message = "An employee with this email already exists." });

        Apply(employee, request, status, email);
        if (employee.UserId is not null)
        {
            var user = await users.FindByIdAsync(employee.UserId);
            if (user is not null && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.UserName = email;
                user.FullName = employee.FullName;
                var updated = await users.UpdateAsync(user);
                if (!updated.Succeeded)
                    return BadRequest(new { message = string.Join(" ", updated.Errors.Select(error => error.Description)) });
            }
        }

        await db.SaveChangesAsync();
        await db.Entry(employee).Reference(e => e.Department).LoadAsync();
        await db.Entry(employee).Reference(e => e.Position).LoadAsync();
        await db.Entry(employee).Reference(e => e.Manager).LoadAsync();
        return Mappers.ToEmployee(employee);
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<ActionResult<EmployeeResponse>> SetStatus(int id, StatusRequest request)
    {
        if (!Enum.TryParse<EmploymentStatus>(request.Status, true, out var status))
            return BadRequest(new { message = "Status is not recognized." });

        var employee = await db.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
            return NotFound(new { message = "Employee not found." });

        employee.Status = status;
        await db.SaveChangesAsync();
        return Mappers.ToEmployee(employee);
    }

    private async Task<ActionResult?> ValidatePlacement(EmployeeWriteRequest request, int? employeeId)
    {
        var departmentExists = await db.Departments.AnyAsync(d => d.Id == request.DepartmentId && d.IsActive);
        if (!departmentExists)
            return BadRequest(new { message = "Choose an active department." });

        var positionMatches = await db.Positions.AnyAsync(p => p.Id == request.PositionId && p.DepartmentId == request.DepartmentId);
        if (!positionMatches)
            return BadRequest(new { message = "Choose a position that belongs to the selected department." });

        if (request.ManagerId is int managerId)
        {
            if (managerId == employeeId)
                return BadRequest(new { message = "An employee cannot be their own manager." });
            var managerExists = await db.Employees.AnyAsync(e => e.Id == managerId);
            if (!managerExists)
                return BadRequest(new { message = "Manager was not found." });
        }

        if (request.DateOfBirth >= DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { message = "Date of birth must be in the past." });
        if (request.HireDate < request.DateOfBirth)
            return BadRequest(new { message = "Hire date cannot be before date of birth." });

        return null;
    }

    private static void Apply(Employee employee, EmployeeWriteRequest request, EmploymentStatus status, string email)
    {
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = email;
        employee.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        employee.DateOfBirth = request.DateOfBirth;
        employee.HireDate = request.HireDate;
        employee.Status = status;
        employee.DepartmentId = request.DepartmentId;
        employee.PositionId = request.PositionId;
        employee.ManagerId = request.ManagerId;
    }

    private async Task<string> NextCodeAsync()
    {
        var count = await db.Employees.CountAsync() + 1;
        var code = $"EMP-{count:0000}";
        while (await db.Employees.AnyAsync(e => e.EmployeeCode == code))
        {
            count++;
            code = $"EMP-{count:0000}";
        }
        return code;
    }

    private async Task<ActionResult?> CreateLoginAsync(Employee employee, string? password)
    {
        var user = new ApplicationUser
        {
            UserName = employee.Email,
            Email = employee.Email,
            FullName = employee.FullName,
            EmailConfirmed = true,
            EmployeeId = employee.Id
        };
        var result = await users.CreateAsync(user, string.IsNullOrWhiteSpace(password) ? "Employee@123456" : password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(error => error.Description)) });

        await users.AddToRoleAsync(user, AppRoles.Employee);
        employee.UserId = user.Id;
        await db.SaveChangesAsync();
        return null;
    }
}

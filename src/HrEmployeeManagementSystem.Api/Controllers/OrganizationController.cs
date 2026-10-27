using HrEmployeeManagementSystem.Api.Data;
using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrEmployeeManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
public class OrganizationController(AppDbContext db) : ControllerBase
{
    [HttpGet("api/departments")]
    public async Task<IReadOnlyList<DepartmentResponse>> Departments()
    {
        return await db.Departments.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name, d.Code, d.IsActive, d.Employees.Count()))
            .ToListAsync();
    }

    [HttpPost("api/departments")]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment(DepartmentWriteRequest request)
    {
        var code = request.Code.Trim();
        if (await db.Departments.AnyAsync(d => d.Code == code))
            return BadRequest(new { message = "That department code is already used." });

        var department = new Department { Name = request.Name.Trim(), Code = code, IsActive = request.IsActive };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        return new DepartmentResponse(department.Id, department.Name, department.Code, department.IsActive, 0);
    }

    [HttpPut("api/departments/{id:int}")]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(int id, DepartmentWriteRequest request)
    {
        var department = await db.Departments.Include(d => d.Employees).FirstOrDefaultAsync(d => d.Id == id);
        if (department is null)
            return NotFound(new { message = "Department not found." });

        var code = request.Code.Trim();
        if (await db.Departments.AnyAsync(d => d.Code == code && d.Id != id))
            return BadRequest(new { message = "That department code is already used." });

        department.Name = request.Name.Trim();
        department.Code = code;
        department.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return new DepartmentResponse(department.Id, department.Name, department.Code, department.IsActive, department.Employees.Count);
    }

    [HttpGet("api/positions")]
    public async Task<IReadOnlyList<PositionResponse>> Positions([FromQuery] int? departmentId)
    {
        var query = db.Positions.AsNoTracking().Include(p => p.Department).AsQueryable();
        if (departmentId is int department)
            query = query.Where(p => p.DepartmentId == department);

        return await query
            .OrderBy(p => p.Department.Name)
            .ThenBy(p => p.Title)
            .Select(p => new PositionResponse(p.Id, p.Title, p.DepartmentId, p.Department.Name))
            .ToListAsync();
    }

    [HttpPost("api/positions")]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<ActionResult<PositionResponse>> CreatePosition(PositionWriteRequest request)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId);
        if (department is null)
            return BadRequest(new { message = "Department was not found." });

        var title = request.Title.Trim();
        if (await db.Positions.AnyAsync(p => p.DepartmentId == department.Id && p.Title == title))
            return BadRequest(new { message = "That position already exists in the department." });

        var position = new Position { Title = title, DepartmentId = department.Id };
        db.Positions.Add(position);
        await db.SaveChangesAsync();
        return new PositionResponse(position.Id, position.Title, department.Id, department.Name);
    }

    [HttpDelete("api/positions/{id:int}")]
    [Authorize(Roles = AppRoles.HrStaff)]
    public async Task<IActionResult> DeletePosition(int id)
    {
        var position = await db.Positions.FirstOrDefaultAsync(p => p.Id == id);
        if (position is null)
            return NotFound(new { message = "Position not found." });
        if (await db.Employees.AnyAsync(e => e.PositionId == id))
            return Conflict(new { message = "This position is still assigned to employees." });

        db.Positions.Remove(position);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

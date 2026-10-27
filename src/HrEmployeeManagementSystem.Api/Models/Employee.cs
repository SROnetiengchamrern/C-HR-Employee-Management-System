using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class Employee
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string EmployeeCode { get; set; } = "";

    [MaxLength(80)]
    public string FirstName { get; set; } = "";

    [MaxLength(80)]
    public string LastName { get; set; } = "";

    [MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(40)]
    public string? Phone { get; set; }

    public DateOnly DateOfBirth { get; set; }
    public DateOnly HireDate { get; set; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class JobOpening
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Title { get; set; } = "";

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int Openings { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    [MaxLength(1000)]
    public string? Description { get; set; }
}

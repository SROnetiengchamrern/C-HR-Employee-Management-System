using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class Department
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = "";

    [MaxLength(20)]
    public string Code { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<JobOpening> JobOpenings { get; set; } = new List<JobOpening>();
}

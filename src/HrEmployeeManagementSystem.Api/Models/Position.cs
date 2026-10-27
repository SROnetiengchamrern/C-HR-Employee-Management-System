using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class Position
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Title { get; set; } = "";

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

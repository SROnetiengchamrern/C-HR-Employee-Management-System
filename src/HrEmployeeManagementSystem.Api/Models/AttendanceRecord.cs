using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly WorkDate { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }

    [MaxLength(400)]
    public string? Notes { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class LeaveType
{
    public int Id { get; set; }

    [MaxLength(60)]
    public string Name { get; set; } = "";

    public int DefaultDays { get; set; }
}

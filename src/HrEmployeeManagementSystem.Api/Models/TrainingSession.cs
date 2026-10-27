using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class TrainingSession
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = "";

    public DateTime StartsAt { get; set; }

    [MaxLength(120)]
    public string? Location { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace HrEmployeeManagementSystem.Api.Models;

public class AppNotification
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = "";

    [MaxLength(1000)]
    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }
}

using Microsoft.AspNetCore.Identity;

namespace HrEmployeeManagementSystem.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public int? EmployeeId { get; set; }
}

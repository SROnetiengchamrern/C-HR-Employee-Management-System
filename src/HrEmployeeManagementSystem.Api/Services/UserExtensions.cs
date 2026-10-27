using System.Security.Claims;
using HrEmployeeManagementSystem.Api.Models;

namespace HrEmployeeManagementSystem.Api.Services;

public static class UserExtensions
{
    public static string? UserId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static int? EmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("employeeId");
        return int.TryParse(value, out var id) ? id : null;
    }

    public static bool IsHrStaff(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Hr);

    public static bool CanAccessEmployee(this ClaimsPrincipal user, int employeeId) =>
        user.IsHrStaff() || user.EmployeeId() == employeeId;
}

using HrEmployeeManagementSystem.Api.Dtos;
using HrEmployeeManagementSystem.Api.Models;
using HrEmployeeManagementSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HrEmployeeManagementSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> users, TokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var roles = await users.GetRolesAsync(user);
        var (token, expires) = tokens.Create(user, roles);
        return new LoginResponse(token, expires, ToProfile(user, roles));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> Me()
    {
        var user = await users.FindByIdAsync(User.UserId() ?? "");
        if (user is null)
            return Unauthorized();

        var roles = await users.GetRolesAsync(user);
        return ToProfile(user, roles);
    }

    private static UserProfileDto ToProfile(ApplicationUser user, IList<string> roles) =>
        new(user.Id, user.Email ?? "", user.FullName, roles.ToList(), user.EmployeeId);
}

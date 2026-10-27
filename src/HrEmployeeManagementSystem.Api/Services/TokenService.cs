using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HrEmployeeManagementSystem.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace HrEmployeeManagementSystem.Api.Services;

public class TokenService(IConfiguration configuration)
{
    public (string Token, DateTime ExpiresAt) Create(ApplicationUser user, IList<string> roles)
    {
        var jwt = configuration.GetSection("Jwt");
        var keyText = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var expiresMinutes = double.TryParse(jwt["ExpiresMinutes"], out var minutes) ? minutes : 720;
        var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email ?? "")
        };

        if (user.EmployeeId is int employeeId)
            claims.Add(new Claim("employeeId", employeeId.ToString()));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText)),
                SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

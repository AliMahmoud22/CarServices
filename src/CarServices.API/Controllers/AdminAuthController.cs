using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarServices.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CarServices.API.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController(IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Authenticates the admin user and returns a JWT Bearer token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AdminLoginResponse> Login([FromBody] AdminLoginRequest request)
    {
        var expectedUsername = configuration["AdminAuth:Username"] ?? "admin";
        var expectedPassword = configuration["AdminAuth:Password"] ?? "Admin@123";

        if (!string.Equals(request.Username?.Trim(), expectedUsername, StringComparison.OrdinalIgnoreCase) ||
            request.Password != expectedPassword)
        {
            return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة." });
        }

        var jwtKey = configuration["AdminAuth:JwtKey"]
                     ?? "CarServices_SuperSecret_JwtSigningKey_2026_Min32Chars!";
        var issuer = configuration["AdminAuth:Issuer"] ?? "CarServices.API";
        var audience = configuration["AdminAuth:Audience"] ?? "CarServices.Client";
        var expiryHours = int.TryParse(configuration["AdminAuth:ExpiryHours"], out var h) ? h : 12;

        var expiresAtUtc = DateTime.UtcNow.AddHours(expiryHours);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, expectedUsername),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new AdminLoginResponse(tokenString, expiresAtUtc, expectedUsername));
    }
}


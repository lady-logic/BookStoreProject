using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookStoreApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BookStoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [HttpPost("register")]
    [AllowAnonymous]
    public IActionResult Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // TODO: Save to mock store
        return Ok("User registered successfully.");
    }

    [HttpGet("debug-claims")]
    [Authorize] // Nur [Authorize], kein [CustomRole]
    public IActionResult DebugClaims()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity.IsAuthenticated,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginModel model)
    {
        // Hardcoded User – nur als Demo
        if (model.Email == "admin@admin.de" && model.Password == "password")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, model.Email),
                new Claim(ClaimTypes.Role, "Admin") // für dein CustomRoleAttribute
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ein_sehr_langer_und_sicherer_geheimer_schluessel_fuer_jwt_123456789"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "BookStoreApi",
                audience: "BookStoreApi",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        return Unauthorized();
    }

    [HttpGet("profile")]
    [Authorize]
    public IActionResult GetProfile()
    {
        return Ok(new { Username = "testuser", Role = "Admin" });
    }
}
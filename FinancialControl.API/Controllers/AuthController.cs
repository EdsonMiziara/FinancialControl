using FinancialControl.API.Interfaces;
using FinancialControl.API.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    private readonly IJwtService _jwtService;

    public AuthController(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Realizes user login and returns a JWT token if the credentials are valid.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var user = _context.Usuarios
            .FirstOrDefault(x => x.Email == req.Email);

        if (user == null) return Unauthorized();

        if (BCrypt.Net.BCrypt.Verify(req.Password, user.HashPassword))
            return Unauthorized();

        var token = _jwtService.GenerateToken(user);

        return Ok(new { token });
    }
}
using FinancialControl.API.Interfaces;
using FinancialControl.API.Models;
using FinancialControl.Shared.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinancialControl.API.Services;
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    /// <summary>
    /// Constructor for JwtService that initializes the service with JWT settings provided through dependency injection.
    /// </summary>
    /// <param name="settings"></param>
    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>
    /// Generates a JWT token for the given user, including claims for user ID and email,
    /// and signs it with the configured secret key. The token expires after a specified number of hours.
    /// </summary>
    /// <param name="user"></param>
    /// <returns>
    /// Returns a string representation of the generated JWT token,
    /// which can be used for authentication and authorization purposes in the application.
    /// </returns>
    public string GenerateToken(User user)
    {
        var key = Encoding.ASCII.GetBytes(_settings.Key);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_settings.ExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
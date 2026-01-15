using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RoleClaimsApp.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RoleClaimsApp.Security;

public class TokenService
{
    private readonly string _jwtKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;

    public TokenService(IConfiguration config)
    {
        _jwtKey = config.GetValue<string>("Jwt:Key")
                  ?? throw new InvalidOperationException("JWT key not configured");
        _issuer = config.GetValue<string>("Jwt:Issuer")
                  ?? throw new InvalidOperationException("JWT issuer not configured");
        _audience = config.GetValue<string>("Jwt:Audience")
                    ?? throw new InvalidOperationException("JWT audience not configured");
        _accessTokenMinutes = config.GetValue<int>("Jwt:AccessTokenMinutes", 10);
    }

    public string CreateAccessToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtKey));
        
        var accessMinutes = _accessTokenMinutes;

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(accessMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string HashToken(string token)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
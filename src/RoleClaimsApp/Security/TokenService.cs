using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RoleClaimsApp.Security;

/// <summary>
/// Responsible for generating and signing JWT access tokens
/// and producing cryptographically secure refresh tokens.
/// </summary>
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

    /// <summary>
    /// Creates a signed JWT access token containing user claims, roles and a unique token identifier (jti).
    /// Each token is guaranteed to be unique even if issued within the same minute.
    /// </summary>
    /// <param name="claims">Claims to embed in the token, typically including username, roles and custom claims.</param>
    /// <returns>Serialized and signed JWT access token string.</returns>
    public string CreateAccessToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtKey));

        var jti = new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());
        claims = claims.Append(jti);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure random refresh token using <see cref="RandomNumberGenerator"/>.
    /// The returned value is base64-encoded and suitable for transmission to clients.
    /// </summary>
    /// <returns>Opaque, cryptographically strong refresh token string.</returns>
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    /// <summary>
    /// Computes a SHA-256 hash of the provided token string.
    /// Used to avoid storing raw refresh tokens in the database.
    /// </summary>
    /// <param name="token">The raw token string to hash.</param>
    /// <returns>Base64-encoded SHA-256 hash of the token.</returns>
    public string HashToken(string token)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
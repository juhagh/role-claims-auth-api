using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoleClaimsApp.Models;
using RoleClaimsApp.Security;

namespace RoleClaimsApp.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        TokenService tokenService,
        IConfiguration config)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null) return Unauthorized();

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        var jwtClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!)
        };
        jwtClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        jwtClaims.AddRange(claims);

        var accessToken = _tokenService.CreateAccessToken(jwtClaims);

        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken
        });
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken == null || storedToken.IsExpired || storedToken.IsRevoked)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null) return Unauthorized();

        // Rotate refresh token
        storedToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(newRefreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            ReplacedByTokenId = storedToken.Id
        };

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        var jwtClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!)
        };
        jwtClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        jwtClaims.AddRange(claims);

        var newAccessToken = _tokenService.CreateAccessToken(jwtClaims);

        _db.RefreshTokens.Add(newRefreshEntity);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }
    
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken == null || storedToken.IsRevoked)
            return Ok(); // idempotent logout

        // Revoke token just for this device
        // storedToken.RevokedAt = DateTime.UtcNow;
        
        // Revoke tokens on all devices
        var userId = storedToken.UserId;

        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok("Logged out");
    }
}

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);

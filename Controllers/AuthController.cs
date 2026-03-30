using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using RoleClaimsApp.Models;
using RoleClaimsApp.Security;

namespace RoleClaimsApp.Controllers;

/// <summary>
/// Handles authentication-related operations including login,
/// access token refresh, and logout.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly RefreshTokenService _refreshTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        RefreshTokenService refreshTokenService
        )
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }
    
    /// <summary>
    /// Authenticates a user using username and password credentials.
    /// Issues a short-lived JWT access token and a long-lived refresh token.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>JWT access token and refresh token.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required");
        }
        
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null) return Unauthorized();

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        var jwtClaims = BuildJwtClaims(user, roles, claims);

        var accessToken = _tokenService.CreateAccessToken(jwtClaims);

        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(refreshToken);

        await _refreshTokenService.CreateInitialTokenAsync(
            user.Id,
            refreshTokenHash
            );

        return Ok(new
        {
            accessToken,
            refreshToken
        });
    }
    
    /// <summary>
    /// Refreshes an expired or soon-to-expire access token using a valid refresh token.
    /// Performs refresh token rotation and reissues updated claims.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New JWT access token and rotated refresh token.</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required");
        
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var storedToken = await _refreshTokenService.GetValidTokenAsync(tokenHash);
        if (storedToken == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        var jwtClaims = BuildJwtClaims(user, roles, claims);
        var accessToken = _tokenService.CreateAccessToken(jwtClaims);

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshHash = _tokenService.HashToken(newRefreshToken);

        await _refreshTokenService.RotateTokenAsync(storedToken, newRefreshHash);

        return Ok(new
        {
            accessToken,
            refreshToken = newRefreshToken
        });
    }

    /// <summary>
    /// Logs out the current user by revoking all active refresh tokens
    /// associated with the user account.
    /// </summary>
    /// <param name="request">Logout request containing the refresh token.</param>
    /// <returns>Logout confirmation.</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var token = await _refreshTokenService.GetValidTokenAsync(tokenHash);
        if (token == null)
            return Ok();

        await _refreshTokenService.RevokeAllTokensForUserAsync(token.UserId);

        return Ok("Logged out");
    }
    
    private static List<Claim> BuildJwtClaims(
        ApplicationUser user,
        IEnumerable<string> roles,
        IEnumerable<Claim> claims)
    {
        var jwtClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!)
        };

        jwtClaims.AddRange(roles.Select(r =>
            new Claim(ClaimTypes.Role, r)));

        jwtClaims.AddRange(claims);

        return jwtClaims;
    }
}

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

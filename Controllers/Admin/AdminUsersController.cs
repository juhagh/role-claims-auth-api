using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoleClaimsApp.Authorization;
using RoleClaimsApp.Models;
using System.Security.Claims;

namespace RoleClaimsApp.Controllers;

/// <summary>
/// Administrative endpoints for managing users and claims.
/// Access restricted to users with the Admin role.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Adds a claim to a specified user if it does not already exist.
    /// </summary>
    /// <param name="username">Target username.</param>
    /// <param name="request">Claim type and value.</param>
    /// <returns>Result of the claim addition operation.</returns>
    [HttpPost("{username}/claims")]
    public async Task<IActionResult> AddClaim(
        string username,
        [FromBody] AddClaimRequest request)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return NotFound($"User '{username}' not found");

        var existingClaims = await _userManager.GetClaimsAsync(user);

        if (existingClaims.Any(c =>
                c.Type == request.Type && c.Value == request.Value))
        {
            return Conflict("Claim already exists for user");
        }

        var result = await _userManager.AddClaimAsync(
            user,
            new Claim(request.Type, request.Value));

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("Claim added");
    }
}

public record AddClaimRequest(string Type, string Value);
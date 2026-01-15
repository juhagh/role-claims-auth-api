using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoleClaimsApp.Authorization;
using RoleClaimsApp.Models;
using System.Security.Claims;

namespace RoleClaimsApp.Controllers;

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
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoleClaimsApp.Authorization;
using RoleClaimsApp.Models;

namespace RoleClaimsApp.Controllers;

/// <summary>
/// Demonstrates role-based and claim-based authorization
/// using policy-based access control.
/// </summary>
[ApiController]
[Route("api/users")]
public class ProtectedUsersController : ControllerBase
{
    /// <summary>
    /// Endpoint accessible only to users with the Admin role.
    /// </summary>
    /// <returns>Confirmation of Admin access.</returns>
    [Authorize(
        Policy = AuthorizationPolicies.AdminOnly
    )]
    [HttpGet("admin")]
    public IActionResult GetAdminUser()
    {
        return Ok(new
        {
            Message = "Access granted. You are an Admin.",
            User = User.Identity?.Name
        });
    }

    /// <summary>
    /// Endpoint accessible only to users with the Department=IT claim.
    /// </summary>
    /// <returns>Confirmation of IT department access.</returns>
    [Authorize(
        Policy = AuthorizationPolicies.ITDepartmentOnly
    )]
    [HttpGet("it")]
    public IActionResult GetITUser()
    {
        return Ok(new
        {
            Message = "Access granted. You belong to the IT department.",
            User = User.Identity?.Name
        });
    }
    
    [HttpDelete("claims/department")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> RemoveDepartmentClaim(
        [FromServices] UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByNameAsync("admin");
        if (user == null)
            return NotFound("User not found");

        var claim = new Claim("Department", "IT");

        var result = await userManager.RemoveClaimAsync(user, claim);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("Department claim removed");
    }
    
}
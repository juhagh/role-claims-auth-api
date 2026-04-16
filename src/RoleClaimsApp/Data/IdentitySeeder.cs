using Microsoft.AspNetCore.Identity;
using RoleClaimsApp.Models;
using System.Security.Claims;

namespace RoleClaimsApp.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // ---- Roles ----
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // ---- Admin User ----
        var user = await userManager.FindByNameAsync("admin");
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                FullName = "Jane Admin"
            };

            await userManager.CreateAsync(user, "Password123!");
            await userManager.AddToRoleAsync(user, "Admin");
            await userManager.AddClaimAsync(user, new Claim("Department", "IT"));
        }
        
        // ---- Regular User with no Role/Claim----
        var regularUser = await userManager.FindByNameAsync("user");
        if (regularUser == null)
        {
            regularUser = new ApplicationUser
            {
                UserName = "user",
                Email = "user@example.com",
                FullName = "John User"
            };

            await userManager.CreateAsync(regularUser, "Password123!");
        }
    }
}
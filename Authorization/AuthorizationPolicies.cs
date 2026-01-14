using Microsoft.AspNetCore.Authorization;

namespace RoleClaimsApp.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ITDepartmentOnly = "ITDepartmentOnly";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
        {
            policy.RequireRole("Admin");
        });

        options.AddPolicy(ITDepartmentOnly, policy =>
        {
            policy.RequireClaim("Department", "IT");
        });
    }
}
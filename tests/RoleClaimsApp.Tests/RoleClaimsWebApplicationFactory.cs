using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace RoleClaimsApp.Tests;

public class RoleClaimsWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString = 
        "Host=localhost;Port=5433;Database=roleclaimstestdb;Username=appuser;Password=apppassword";
    private const string TestJwtKey = "test-ci-secret-key-at-least-32-characters-long";
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string and JWT configs
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "RoleClaimsApp",
                ["Jwt:Audience"] = "RoleClaimsAppClient",
                ["Jwt:AccessTokenMinutes"] = "10",
                ["Jwt:RefreshTokenDays"] = "7",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(TestJwtKey));
                });
        });
    }
}
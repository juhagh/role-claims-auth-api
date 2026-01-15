using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RoleClaimsApp;
using RoleClaimsApp.Authorization;
using RoleClaimsApp.Data;
using RoleClaimsApp.Models;
using RoleClaimsApp.Security;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") 
             ?? throw new InvalidOperationException("JWT key is not configured");

var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer");
var jwtAudience = builder.Configuration.GetValue<string>("Jwt:Audience");


// Add token services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RefreshTokenService>();

// Controllers
builder.Services.AddControllers();

// Database - Postgres
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Prevent redirection to enable 403 response
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicies.AddPolicies(options);
});

// JWT authentication setup
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))

        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Create users and roles
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
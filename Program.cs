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

var jwtKey = "THIS_IS_A_DEMO_SECRET_KEY_CHANGE_LATER";
var jwtIssuer = "RoleClaimsApp";
var jwtAudience = "RoleClaimsAppClient";

var builder = WebApplication.CreateBuilder(args);

// Add token service
builder.Services.AddScoped<TokenService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Controllers
builder.Services.AddControllers();

// Database (In-Memory for demo)
// builder.Services.AddDbContext<ApplicationDbContext>(options => 
//     options.UseInMemoryDatabase("UserDirectoryDb")
// );
// Postgres
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

// Fake authentication for assignment
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

app.Use(async (context, next) =>
{
    Console.WriteLine("----- REQUEST START -----");

    Console.WriteLine($"Path: {context.Request.Path}");
    Console.WriteLine($"Method: {context.Request.Method}");

    Console.WriteLine($"User authenticated: {context.User?.Identity?.IsAuthenticated}");

    if (context.User?.Claims != null)
    {
        foreach (var claim in context.User.Claims)
        {
            Console.WriteLine($"Claim BEFORE auth: {claim.Type} = {claim.Value}");
        }
    }

    await next();

    Console.WriteLine($"Response: {context.Response.Headers}");
    Console.WriteLine("----- REQUEST END -----");
    Console.WriteLine($"Response status: {context.Response.StatusCode}");
});

app.UseRouting();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    Console.WriteLine("----- AFTER AUTHENTICATION -----");
    Console.WriteLine($"User authenticated: {context.User.Identity?.IsAuthenticated}");
    Console.WriteLine($"User name: {context.User.Identity?.Name}");

    foreach (var claim in context.User.Claims)
    {
        Console.WriteLine($"Claim AFTER auth: {claim.Type} = {claim.Value}");
    }

    await next();
});

app.UseAuthorization();

app.Use(async (context, next) =>
{
    Console.WriteLine("----- AFTER AUTHORIZATION -----");
    Console.WriteLine($"Endpoint: {context.GetEndpoint()?.DisplayName}");
    Console.WriteLine($"Response status so far: {context.Response.StatusCode}");

    await next();
});

app.MapControllers();

// Create users and roles
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
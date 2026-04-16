using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoleClaimsApp.Controllers;
using RoleClaimsApp.Tests.Models;

namespace RoleClaimsApp.Tests;

public class AuthControllerTests : IClassFixture<RoleClaimsWebApplicationFactory>
{
    
    private const string LoginRoute = "/api/auth/login";
    private const string RefreshRoute = "/api/auth/refresh";
    private const string LogoutRoute = "/api/auth/logout";
    private const string GetAdminRoute = "/api/users/admin";
    private const string GetItRoute = "/api/users/it";
    private readonly RoleClaimsWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public AuthControllerTests(RoleClaimsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var tokens = await LoginAsync("admin", "Password123!");
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var request = new LoginRequest("admin", "WrongPassword");
        var response = await _client.PostAsJsonAsync(LoginRoute, request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        var request = new LoginRequest("idontexist", "somepassword");
        var response = await _client.PostAsJsonAsync(LoginRoute, request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminEndpoint_WithAdminToken_ReturnsOk()
    {
        var authenticatedClient = await GetAuthenticatedClientAsync("admin", "Password123!");
        
        var response = await authenticatedClient.GetAsync(GetAdminRoute);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminEndpoint_WithNoToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(GetAdminRoute);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task AccessAdminEndpoint_WithUserToken_ReturnsForbidden()
    {
        var authenticatedClient = await GetAuthenticatedClientAsync("user", "Password123!");
        
        var response = await authenticatedClient.GetAsync(GetAdminRoute);
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    
    [Fact]
    public async Task AccessItEndpoint_WithAdminToken_ReturnsOk()
    {
        var authenticatedClient = await GetAuthenticatedClientAsync("admin", "Password123!");
        
        var response = await authenticatedClient.GetAsync(GetItRoute);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task AccessItEndpoint_WithUserToken_ReturnsForbidden()
    {
        var authenticatedClient = await GetAuthenticatedClientAsync("user", "Password123!");
        
        var response = await authenticatedClient.GetAsync(GetItRoute);
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        var tokens = await LoginAsync("admin", "Password123!");
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        var oldRefreshToken = tokens.RefreshToken;
        var refreshRequest = new RefreshRequest(RefreshToken: oldRefreshToken);
        
        var response = await _client.PostAsJsonAsync(RefreshRoute, refreshRequest);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var newTokens = await response.Content.ReadFromJsonAsync<JwtResponse>(JsonOptions);
        Assert.NotNull(newTokens);
        Assert.False(string.IsNullOrWhiteSpace(newTokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(newTokens.RefreshToken));
        Assert.NotEqual(newTokens.RefreshToken, oldRefreshToken);
    }
    

    [Fact]
    public async Task RefreshToken_WithUsedToken_ReturnsUnauthorized()
    {
        var tokens = await LoginAsync("admin", "Password123!");
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        var oldRefreshToken = tokens.RefreshToken;
        var refreshRequest = new RefreshRequest(RefreshToken: oldRefreshToken);
        
        var firstRefreshResponse = await _client.PostAsJsonAsync(RefreshRoute, refreshRequest);
        Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);
        
        var secondRefreshResponse = await _client.PostAsJsonAsync(RefreshRoute, refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshResponse.StatusCode);
    }
    

    [Fact]
    public async Task Logout_RevokesTokens_RefreshReturnsUnauthorized()
    {
        var tokens = await LoginAsync("admin", "Password123!");
        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var logoutResponse = await authenticatedClient.PostAsync(LogoutRoute, new StringContent(string.Empty));
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var refreshRequest = new RefreshRequest(tokens.RefreshToken);
        var response = await _client.PostAsJsonAsync(RefreshRoute, refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    private async Task<JwtResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest(username, password);
        var response = await _client.PostAsJsonAsync(LoginRoute, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var tokens = await response.Content.ReadFromJsonAsync<JwtResponse>(JsonOptions);
        Assert.NotNull(tokens);

        return tokens;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync(string username, string password)
    {
        var tokens = await LoginAsync(username, password);
        
        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        
        return authenticatedClient;
    }
}
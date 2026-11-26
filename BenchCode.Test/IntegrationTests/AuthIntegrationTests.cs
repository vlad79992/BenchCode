using BenchCode.AuthService.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BenchCode.Test.IntegrationTests;

[Collection("AuthTests")]
public sealed class AuthIntegrationTests(Fixture fixture)
{
    private readonly Fixture fixture = fixture;

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedUser()
    {
        using var client = CreateClient();
        var user = CreateUser();

        using var response = await RegisterAsync(client, user);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(user.UserName, body.RootElement.GetProperty("userName").GetString());
        Assert.Equal(user.Email, body.RootElement.GetProperty("email").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        using var client = CreateClient();
        var user = CreateUser();

        using var firstResponse = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var duplicateResponse = await RegisterAsync(
            client,
            user with { UserName = $"{user.UserName}-duplicate" });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        using var client = CreateClient();
        var user = CreateUser() with { Email = "not-an-email" };

        using var response = await RegisterAsync(client, user);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresPasswordAsHash()
    {
        using var client = CreateClient();
        var user = CreateUser();

        using var response = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = fixture.AuthServiceFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var storedUser = await userManager.FindByNameAsync(user.UserName);

        Assert.NotNull(storedUser);
        Assert.NotNull(storedUser.PasswordHash);
        Assert.NotEqual(user.Password, storedUser.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(
                storedUser,
                storedUser.PasswordHash,
                user.Password));
    }

    [Fact]
    public async Task PasswordGrant_WithInvalidPassword_ReturnsInvalidGrant()
    {
        using var client = CreateClient();
        var user = CreateUser();

        using var registration = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var response = await RequestTokenAsync(client, user, "Wrong-password-123!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(
            body.RootElement.GetProperty("error").GetString() == "invalid_grant",
            body.RootElement.GetRawText());
    }

    [Fact]
    public async Task GetProfilesByIds_ReturnsRequestedPublicProfiles()
    {
        using var client = CreateClient();
        var user = CreateUser();

        using var registration = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var registrationBody = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        var userId = registrationBody.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(userId));

        var accessToken = await GetAccessTokenAsync(client, user);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/profiles/by-ids")
        {
            Content = JsonContent.Create(new { userIds = new[] { userId, "unknown-user-id", userId } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var profile = Assert.Single(body.RootElement.EnumerateArray());
        Assert.Equal(userId, profile.GetProperty("id").GetString());
        Assert.Equal(user.FirstName, profile.GetProperty("firstName").GetString());
        Assert.Equal(user.LastName, profile.GetProperty("lastName").GetString());
        Assert.False(profile.TryGetProperty("email", out _));
        Assert.False(profile.TryGetProperty("userName", out _));
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PasswordGrant_WithValidCredentials_ReturnsAccessToken()
    {
        using var client = CreateClient();
        var user = CreateUser();

        using var registration = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var accessToken = await GetAccessTokenAsync(client, user);

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
    }

    [Fact]
    public async Task PasswordGrant_OverHttp_IsRejected()
    {
        using var httpsClient = CreateClient();
        var user = CreateUser();

        using var registration = await RegisterAsync(httpsClient, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var httpClient = fixture.AuthServiceFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost")
            });
        using var response = await RequestTokenAsync(httpClient, user, user.Password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_request", body.RootElement.GetProperty("error").GetString());
        Assert.Contains(
            "HTTPS",
            body.RootElement.GetProperty("error_description").GetString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsCurrentUser()
    {
        using var client = CreateClient();
        var user = CreateUser();

        using var registration = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var accessToken = await GetAccessTokenAsync(client, user);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(user.UserName, body.RootElement.GetProperty("userName").GetString());
        Assert.Equal(user.Email, body.RootElement.GetProperty("email").GetString());
    }

    private static TestUser CreateUser()
    {
        var userName = $"test-{Guid.NewGuid():N}";
        return new TestUser(
            userName,
            $"{userName}@example.com",
            "Test-password-123!",
            $"First-{userName}",
            $"Last-{userName}");
    }

    private HttpClient CreateClient() =>
        fixture.AuthServiceFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, TestUser user) =>
        client.PostAsJsonAsync(
            "/users/register",
            new
            {
                userName = user.UserName,
                email = user.Email,
                password = user.Password,
                firstName = user.FirstName,
                lastName = user.LastName
            });

    private static Task<HttpResponseMessage> RequestTokenAsync(
        HttpClient client,
        TestUser user,
        string password) =>
        client.PostAsync(
            "/authorization/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "compose-web-app",
                ["username"] = user.Email,
                ["password"] = password,
                ["scope"] = "benchcode-api"
            }));

    private static async Task<string> GetAccessTokenAsync(HttpClient client, TestUser user)
    {
        using var response = await RequestTokenAsync(client, user, user.Password);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected token success, got {(int)response.StatusCode}: {body.RootElement.GetRawText()}");

        return body.RootElement.GetProperty("access_token").GetString()
               ?? throw new InvalidOperationException("The token response has no access_token.");
    }

    private sealed record TestUser(
        string UserName,
        string Email,
        string Password,
        string FirstName,
        string LastName);
}
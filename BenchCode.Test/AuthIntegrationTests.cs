using BenchCode.AuthService.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BenchCode.Test;

[Collection("AuthTests")]
public sealed class AuthIntegrationTests
{
    private readonly AuthFixture fixture;

    public AuthIntegrationTests(AuthFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedUser()
    {
        using HttpClient client = CreateClient();
        TestUser user = CreateUser();

        using HttpResponseMessage response = await RegisterAsync(client, user);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(user.UserName, body.RootElement.GetProperty("userName").GetString());
        Assert.Equal(user.Email, body.RootElement.GetProperty("email").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        using HttpClient client = CreateClient();
        TestUser user = CreateUser();

        using HttpResponseMessage firstResponse = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using HttpResponseMessage duplicateResponse =
            await RegisterAsync(client, user with { UserName = $"{user.UserName}-duplicate" });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        using HttpClient client = CreateClient();
        TestUser user = CreateUser() with { Email = "not-an-email" };

        using HttpResponseMessage response = await RegisterAsync(client, user);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresPasswordAsHash()
    {
        using HttpClient client = CreateClient();
        TestUser user = CreateUser();

        using HttpResponseMessage response = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using IServiceScope scope = fixture.Factory.Services.CreateScope();
        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        User? storedUser = await userManager.FindByNameAsync(user.UserName);

        Assert.NotNull(storedUser);
        Assert.NotNull(storedUser.PasswordHash);
        Assert.NotEqual(user.Password, storedUser.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
                     new PasswordHasher<User>().VerifyHashedPassword(storedUser, storedUser.PasswordHash,
                                                                     user.Password));
    }

    [Fact]
    public async Task PasswordGrant_WithInvalidPassword_ReturnsInvalidGrant()
    {
        using HttpClient client = CreateClient();
        TestUser user = CreateUser();

        using HttpResponseMessage registration = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using HttpResponseMessage response = await RequestTokenAsync(client, user, "Wrong-password-123!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("error").GetString() == "invalid_grant",
                    body.RootElement.GetRawText());
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PasswordGrant_WithValidCredentials_ReturnsAccessToken()
    {
        using HttpClient client = CreateClient();
        TestUser user = CreateUser();

        using HttpResponseMessage registration = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var accessToken = await GetAccessTokenAsync(client, user);

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
    }

    [Fact]
    public async Task PasswordGrant_OverHttp_IsRejected()
    {
        using HttpClient httpsClient = CreateClient();
        TestUser user = CreateUser();

        using HttpResponseMessage registration = await RegisterAsync(httpsClient, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using HttpClient httpClient = fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        using HttpResponseMessage response = await RequestTokenAsync(httpClient, user, user.Password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_request", body.RootElement.GetProperty("error").GetString());
        Assert.Contains("HTTPS", body.RootElement.GetProperty("error_description").GetString(),
                        StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsCurrentUser()
    {
        using HttpClient client = CreateClient();
        TestUser user = CreateUser();

        using HttpResponseMessage registration = await RegisterAsync(client, user);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var accessToken = await GetAccessTokenAsync(client, user);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(user.UserName, body.RootElement.GetProperty("userName").GetString());
        Assert.Equal(user.Email, body.RootElement.GetProperty("email").GetString());
    }

    private static TestUser CreateUser()
    {
        var userName = $"test-{Guid.NewGuid():N}";
        return new TestUser(userName, $"{userName}@example.com", "Test-password-123!");
    }

    private HttpClient CreateClient() => fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, TestUser user) =>
        client.PostAsJsonAsync("/api/users/register", new
        {
            userName = user.UserName,
            email = user.Email,
            password = user.Password
        });

    private static Task<HttpResponseMessage> RequestTokenAsync(HttpClient client, TestUser user,
                                                               string password) =>
        client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "compose-web-app",
                ["username"] = user.UserName,
                ["password"] = password,
                ["scope"] = "benchcode-api"
            }));

    private static async Task<string> GetAccessTokenAsync(HttpClient client, TestUser user)
    {
        using HttpResponseMessage response = await RequestTokenAsync(client, user, user.Password);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected token success, got {(int)response.StatusCode}: {body.RootElement.GetRawText()}");

        return body.RootElement.GetProperty("access_token").GetString() ??
               throw new InvalidOperationException("The token response has no access_token.");
    }

    private sealed record TestUser(string UserName, string Email, string Password);
}
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IssueTrackingAPI.Tests;

public class AuthTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AuthTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_FirstUser_IsAutomaticallyAdmin()
    {
        var auth = await _client.RegisterAsync("alice", "alice@test.com");

        Assert.Equal("Admin", auth.User.Role);
        Assert.False(string.IsNullOrEmpty(auth.Token));
    }

    [Fact]
    public async Task Register_SecondUser_IsMember()
    {
        await _client.RegisterAsync("alice", "alice@test.com");
        var second = await _client.RegisterAsync("bob", "bob@test.com");

        Assert.Equal("Member", second.User.Role);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns409Conflict()
    {
        await _client.RegisterAsync("alice", "alice@test.com");

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = "alice",
            Email = "another@test.com",
            Password = "Password123"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400BadRequest()
    {
        // Password < 6 ký tự phải bị chặn bởi [StringLength(MinimumLength = 6)] trên RegisterDto.
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = "charlie",
            Email = "charlie@test.com",
            Password = "123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsToken()
    {
        await _client.RegisterAsync("alice", "alice@test.com", "Password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            UsernameOrEmail = "alice",
            Password = "Password123"
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401Unauthorized()
    {
        await _client.RegisterAsync("alice", "alice@test.com", "Password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            UsernameOrEmail = "alice",
            Password = "SaiMatKhau999"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}

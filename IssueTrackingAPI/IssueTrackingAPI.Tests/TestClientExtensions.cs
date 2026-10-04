using System.Net.Http.Headers;
using System.Net.Http.Json;
using IssueTrackingAPI.DTOs.Auth;

namespace IssueTrackingAPI.Tests;

public static class TestClientExtensions
{
    /// <summary>Đăng ký user mới, trả về token + thông tin user (bao gồm Role).</summary>
    public static async Task<AuthResponseDto> RegisterAsync(
        this HttpClient client, string username, string email, string password = "Password123")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = username,
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!;
    }

    /// <summary>Gắn Bearer token vào header của client, dùng cho các request tiếp theo.</summary>
    public static void AuthorizeAs(this HttpClient client, AuthResponseDto auth)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);
    }

    public static void ClearAuth(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }
}

using IssueTrackingAPI.DTOs.Auth;

namespace IssueTrackingAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IssueTrackingAPI.Common;
using IssueTrackingAPI.Data;
using IssueTrackingAPI.DTOs.Auth;
using IssueTrackingAPI.Models;
using IssueTrackingAPI.Models.Enums;
using IssueTrackingAPI.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IssueTrackingAPI.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwtSettings;

    public AuthService(AppDbContext db, IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var usernameOrEmailTaken = await _db.Users.AnyAsync(u =>
            u.Username == dto.Username || u.Email == dto.Email);

        if (usernameOrEmailTaken)
        {
            throw new ConflictException("Username hoặc email đã được sử dụng.");
        }

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            // Người dùng đầu tiên trong hệ thống tự động là Admin.
            Role = await _db.Users.AnyAsync() ? UserRole.Member : UserRole.Admin
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Username/email hoặc mật khẩu không đúng.");
        }

        return GenerateAuthResponse(user);
    }

    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString()
            }
        };
    }
}

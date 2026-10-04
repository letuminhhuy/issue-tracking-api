using System.Security.Claims;
using IssueTrackingAPI.Models.Enums;

namespace IssueTrackingAPI.Common;

public static class CurrentUserExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var id))
        {
            throw new UnauthorizedAppException("Không tìm thấy thông tin người dùng trong token.");
        }
        return id;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        var roleValue = principal.FindFirstValue(ClaimTypes.Role);
        return string.Equals(roleValue, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

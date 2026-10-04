using IssueTrackingAPI.Models.Enums;

namespace IssueTrackingAPI.Models;

/// <summary>
/// Bảng nối n-n giữa User và Project, kèm vai trò trong project.
/// </summary>
public class ProjectMember
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public ProjectRole Role { get; set; } = ProjectRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

using IssueTrackingAPI.Models.Enums;

namespace IssueTrackingAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
    public ICollection<Issue> ReportedIssues { get; set; } = new List<Issue>();
    public ICollection<Issue> AssignedIssues { get; set; } = new List<Issue>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

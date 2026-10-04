using IssueTrackingAPI.Models.Enums;

namespace IssueTrackingAPI.Models;

public class Issue
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.Open;
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int ReporterId { get; set; }
    public User? Reporter { get; set; }

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<IssueLabel> IssueLabels { get; set; } = new List<IssueLabel>();
}

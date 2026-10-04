namespace IssueTrackingAPI.Models;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;

    public int IssueId { get; set; }
    public Issue? Issue { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

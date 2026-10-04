using System.ComponentModel.DataAnnotations;

namespace IssueTrackingAPI.DTOs.Comments;

public class CreateCommentDto
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentDto
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int IssueId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace IssueTrackingAPI.DTOs.Issues;

public class CreateIssueDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    /// <summary>Open, InProgress, InReview, Closed. Mặc định Open.</summary>
    public string? Status { get; set; }

    /// <summary>Low, Medium, High, Critical. Mặc định Medium.</summary>
    public string? Priority { get; set; }

    public int? AssigneeId { get; set; }

    public List<int>? LabelIds { get; set; }
}

public class UpdateIssueDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required]
    public string Status { get; set; } = "Open";

    [Required]
    public string Priority { get; set; } = "Medium";

    public int? AssigneeId { get; set; }

    public List<int>? LabelIds { get; set; }
}

public class IssueDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public int ReporterId { get; set; }
    public string ReporterUsername { get; set; } = string.Empty;

    public int? AssigneeId { get; set; }
    public string? AssigneeUsername { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<LabelSummaryDto> Labels { get; set; } = new();
    public int CommentCount { get; set; }
}

public class LabelSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

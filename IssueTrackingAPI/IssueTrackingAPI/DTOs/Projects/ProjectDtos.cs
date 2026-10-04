using System.ComponentModel.DataAnnotations;

namespace IssueTrackingAPI.DTOs.Projects;

public class CreateProjectDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }
}

public class UpdateProjectDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public int IssueCount { get; set; }
}

public class ProjectMemberDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class AddProjectMemberDto
{
    [Required]
    public int UserId { get; set; }

    /// <summary>"Owner" hoặc "Member". Mặc định là Member.</summary>
    public string Role { get; set; } = "Member";
}

public class UpdateProjectMemberRoleDto
{
    [Required]
    public string Role { get; set; } = "Member";
}

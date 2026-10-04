using IssueTrackingAPI.DTOs.Projects;

namespace IssueTrackingAPI.Services;

public interface IProjectService
{
    Task<List<ProjectDto>> GetProjectsForUserAsync(int userId, bool isAdmin);
    Task<ProjectDto> GetProjectByIdAsync(int projectId, int userId, bool isAdmin);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, int userId);
    Task<ProjectDto> UpdateProjectAsync(int projectId, UpdateProjectDto dto, int userId, bool isAdmin);
    Task DeleteProjectAsync(int projectId, int userId, bool isAdmin);

    Task<List<ProjectMemberDto>> GetMembersAsync(int projectId, int userId, bool isAdmin);
    Task<ProjectMemberDto> AddMemberAsync(int projectId, AddProjectMemberDto dto, int userId, bool isAdmin);
    Task<ProjectMemberDto> UpdateMemberRoleAsync(int projectId, int targetUserId, UpdateProjectMemberRoleDto dto, int userId, bool isAdmin);
    Task RemoveMemberAsync(int projectId, int targetUserId, int userId, bool isAdmin);

    /// <summary>Kiểm tra user có phải thành viên (bất kỳ vai trò) của project không. Admin hệ thống luôn coi như có quyền.</summary>
    Task EnsureMemberAsync(int projectId, int userId, bool isAdmin);

    /// <summary>Kiểm tra user có phải Owner của project không (hoặc Admin hệ thống).</summary>
    Task EnsureOwnerAsync(int projectId, int userId, bool isAdmin);
}

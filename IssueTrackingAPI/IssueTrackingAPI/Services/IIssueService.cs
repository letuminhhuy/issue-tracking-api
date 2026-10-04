using IssueTrackingAPI.DTOs.Issues;

namespace IssueTrackingAPI.Services;

public interface IIssueService
{
    Task<List<IssueDto>> GetIssuesAsync(int projectId, int userId, bool isAdmin, string? status, string? priority, int? assigneeId);
    Task<IssueDto> GetIssueByIdAsync(int projectId, int issueId, int userId, bool isAdmin);
    Task<IssueDto> CreateIssueAsync(int projectId, CreateIssueDto dto, int userId, bool isAdmin);
    Task<IssueDto> UpdateIssueAsync(int projectId, int issueId, UpdateIssueDto dto, int userId, bool isAdmin);
    Task DeleteIssueAsync(int projectId, int issueId, int userId, bool isAdmin);
}

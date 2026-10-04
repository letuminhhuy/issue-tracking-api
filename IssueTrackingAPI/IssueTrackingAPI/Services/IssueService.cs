using IssueTrackingAPI.Common;
using IssueTrackingAPI.Data;
using IssueTrackingAPI.DTOs.Issues;
using IssueTrackingAPI.Models;
using IssueTrackingAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackingAPI.Services;

public class IssueService : IIssueService
{
    private readonly AppDbContext _db;
    private readonly IProjectService _projectService;

    public IssueService(AppDbContext db, IProjectService projectService)
    {
        _db = db;
        _projectService = projectService;
    }

    public async Task<List<IssueDto>> GetIssuesAsync(int projectId, int userId, bool isAdmin, string? status, string? priority, int? assigneeId)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);

        var query = BaseQuery().Where(i => i.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<IssueStatus>(status, true, out var statusEnum))
            {
                throw new BadRequestException("Status không hợp lệ. Chỉ nhận Open, InProgress, InReview, Closed.");
            }
            query = query.Where(i => i.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            if (!Enum.TryParse<IssuePriority>(priority, true, out var priorityEnum))
            {
                throw new BadRequestException("Priority không hợp lệ. Chỉ nhận Low, Medium, High, Critical.");
            }
            query = query.Where(i => i.Priority == priorityEnum);
        }

        if (assigneeId.HasValue)
        {
            query = query.Where(i => i.AssigneeId == assigneeId.Value);
        }

        var issues = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
        return issues.Select(ToDto).ToList();
    }

    public async Task<IssueDto> GetIssueByIdAsync(int projectId, int issueId, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);

        var issue = await BaseQuery()
            .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == projectId)
            ?? throw new NotFoundException($"Không tìm thấy issue #{issueId} trong project #{projectId}.");

        return ToDto(issue);
    }

    public async Task<IssueDto> CreateIssueAsync(int projectId, CreateIssueDto dto, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);

        var status = ParseStatusOrDefault(dto.Status);
        var priority = ParsePriorityOrDefault(dto.Priority);

        if (dto.AssigneeId.HasValue)
        {
            await EnsureUserIsProjectMemberAsync(projectId, dto.AssigneeId.Value);
        }

        var labelIds = await ValidateLabelIdsAsync(projectId, dto.LabelIds);

        var issue = new Issue
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = status,
            Priority = priority,
            ProjectId = projectId,
            ReporterId = userId,
            AssigneeId = dto.AssigneeId
        };

        foreach (var labelId in labelIds)
        {
            issue.IssueLabels.Add(new IssueLabel { LabelId = labelId });
        }

        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(i => i.Id == issue.Id);
        return ToDto(created);
    }

    public async Task<IssueDto> UpdateIssueAsync(int projectId, int issueId, UpdateIssueDto dto, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);

        var issue = await _db.Issues
            .Include(i => i.IssueLabels)
            .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == projectId)
            ?? throw new NotFoundException($"Không tìm thấy issue #{issueId} trong project #{projectId}.");

        await EnsureCanModifyIssueAsync(issue, projectId, userId, isAdmin);

        if (!Enum.TryParse<IssueStatus>(dto.Status, true, out var status))
        {
            throw new BadRequestException("Status không hợp lệ. Chỉ nhận Open, InProgress, InReview, Closed.");
        }

        if (!Enum.TryParse<IssuePriority>(dto.Priority, true, out var priority))
        {
            throw new BadRequestException("Priority không hợp lệ. Chỉ nhận Low, Medium, High, Critical.");
        }

        if (dto.AssigneeId.HasValue)
        {
            await EnsureUserIsProjectMemberAsync(projectId, dto.AssigneeId.Value);
        }

        var labelIds = await ValidateLabelIdsAsync(projectId, dto.LabelIds);

        issue.Title = dto.Title;
        issue.Description = dto.Description;
        issue.Status = status;
        issue.Priority = priority;
        issue.AssigneeId = dto.AssigneeId;
        issue.UpdatedAt = DateTime.UtcNow;

        issue.IssueLabels.Clear();
        foreach (var labelId in labelIds)
        {
            issue.IssueLabels.Add(new IssueLabel { IssueId = issue.Id, LabelId = labelId });
        }

        await _db.SaveChangesAsync();

        var updated = await BaseQuery().FirstAsync(i => i.Id == issue.Id);
        return ToDto(updated);
    }

    public async Task DeleteIssueAsync(int projectId, int issueId, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);

        var issue = await _db.Issues
            .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == projectId)
            ?? throw new NotFoundException($"Không tìm thấy issue #{issueId} trong project #{projectId}.");

        await EnsureCanModifyIssueAsync(issue, projectId, userId, isAdmin);

        _db.Issues.Remove(issue);
        await _db.SaveChangesAsync();
    }

    private async Task EnsureCanModifyIssueAsync(Issue issue, int projectId, int userId, bool isAdmin)
    {
        if (isAdmin) return;
        if (issue.ReporterId == userId || issue.AssigneeId == userId) return;

        var isOwner = await _db.ProjectMembers.AnyAsync(m =>
            m.ProjectId == projectId && m.UserId == userId && m.Role == ProjectRole.Owner);

        if (!isOwner)
        {
            throw new ForbiddenException("Chỉ người tạo, người được gán, hoặc Owner của project mới có thể sửa/xóa issue này.");
        }
    }

    private async Task EnsureUserIsProjectMemberAsync(int projectId, int assigneeId)
    {
        var isMember = await _db.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == assigneeId);

        if (!isMember)
        {
            throw new BadRequestException("Người được gán (assignee) phải là thành viên của project.");
        }
    }

    private async Task<List<int>> ValidateLabelIdsAsync(int projectId, List<int>? labelIds)
    {
        if (labelIds is null || labelIds.Count == 0)
        {
            return new List<int>();
        }

        var distinctIds = labelIds.Distinct().ToList();
        var validCount = await _db.Labels.CountAsync(l => l.ProjectId == projectId && distinctIds.Contains(l.Id));

        if (validCount != distinctIds.Count)
        {
            throw new BadRequestException("Một hoặc nhiều Label không thuộc project này.");
        }

        return distinctIds;
    }

    private static IssueStatus ParseStatusOrDefault(string? status) =>
        !string.IsNullOrWhiteSpace(status) && Enum.TryParse<IssueStatus>(status, true, out var parsed)
            ? parsed
            : IssueStatus.Open;

    private static IssuePriority ParsePriorityOrDefault(string? priority) =>
        !string.IsNullOrWhiteSpace(priority) && Enum.TryParse<IssuePriority>(priority, true, out var parsed)
            ? parsed
            : IssuePriority.Medium;

    private IQueryable<Issue> BaseQuery() => _db.Issues
        .Include(i => i.Project)
        .Include(i => i.Reporter)
        .Include(i => i.Assignee)
        .Include(i => i.Comments)
        .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
        .AsQueryable();

    private static IssueDto ToDto(Issue issue) => new()
    {
        Id = issue.Id,
        Title = issue.Title,
        Description = issue.Description,
        Status = issue.Status.ToString(),
        Priority = issue.Priority.ToString(),
        ProjectId = issue.ProjectId,
        ProjectName = issue.Project?.Name ?? string.Empty,
        ReporterId = issue.ReporterId,
        ReporterUsername = issue.Reporter?.Username ?? string.Empty,
        AssigneeId = issue.AssigneeId,
        AssigneeUsername = issue.Assignee?.Username,
        CreatedAt = issue.CreatedAt,
        UpdatedAt = issue.UpdatedAt,
        CommentCount = issue.Comments.Count,
        Labels = issue.IssueLabels
            .Where(il => il.Label != null)
            .Select(il => new LabelSummaryDto
            {
                Id = il.Label!.Id,
                Name = il.Label.Name,
                Color = il.Label.Color
            })
            .ToList()
    };
}

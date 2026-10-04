using IssueTrackingAPI.Common;
using IssueTrackingAPI.Data;
using IssueTrackingAPI.DTOs.Comments;
using IssueTrackingAPI.Models;
using IssueTrackingAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackingAPI.Services;

public class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly IProjectService _projectService;

    public CommentService(AppDbContext db, IProjectService projectService)
    {
        _db = db;
        _projectService = projectService;
    }

    public async Task<List<CommentDto>> GetCommentsAsync(int projectId, int issueId, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);
        await EnsureIssueBelongsToProjectAsync(projectId, issueId);

        var comments = await _db.Comments
            .Include(c => c.User)
            .Where(c => c.IssueId == issueId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(ToDto).ToList();
    }

    public async Task<CommentDto> AddCommentAsync(int projectId, int issueId, CreateCommentDto dto, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);
        await EnsureIssueBelongsToProjectAsync(projectId, issueId);

        var comment = new Comment
        {
            IssueId = issueId,
            UserId = userId,
            Content = dto.Content
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        comment.User = await _db.Users.FindAsync(userId);
        return ToDto(comment);
    }

    public async Task<CommentDto> UpdateCommentAsync(int projectId, int issueId, int commentId, UpdateCommentDto dto, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);
        await EnsureIssueBelongsToProjectAsync(projectId, issueId);

        var comment = await _db.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.IssueId == issueId)
            ?? throw new NotFoundException($"Không tìm thấy comment #{commentId}.");

        await EnsureCanModifyCommentAsync(comment, projectId, userId, isAdmin);

        comment.Content = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(comment);
    }

    public async Task DeleteCommentAsync(int projectId, int issueId, int commentId, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);
        await EnsureIssueBelongsToProjectAsync(projectId, issueId);

        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.IssueId == issueId)
            ?? throw new NotFoundException($"Không tìm thấy comment #{commentId}.");

        await EnsureCanModifyCommentAsync(comment, projectId, userId, isAdmin);

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
    }

    private async Task EnsureCanModifyCommentAsync(Comment comment, int projectId, int userId, bool isAdmin)
    {
        if (isAdmin) return;
        if (comment.UserId == userId) return;

        var isOwner = await _db.ProjectMembers.AnyAsync(m =>
            m.ProjectId == projectId && m.UserId == userId && m.Role == ProjectRole.Owner);

        if (!isOwner)
        {
            throw new ForbiddenException("Chỉ tác giả comment hoặc Owner của project mới có thể sửa/xóa comment này.");
        }
    }

    private async Task EnsureIssueBelongsToProjectAsync(int projectId, int issueId)
    {
        var exists = await _db.Issues.AnyAsync(i => i.Id == issueId && i.ProjectId == projectId);
        if (!exists)
        {
            throw new NotFoundException($"Không tìm thấy issue #{issueId} trong project #{projectId}.");
        }
    }

    private static CommentDto ToDto(Comment comment) => new()
    {
        Id = comment.Id,
        Content = comment.Content,
        IssueId = comment.IssueId,
        UserId = comment.UserId,
        Username = comment.User?.Username ?? string.Empty,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt
    };
}

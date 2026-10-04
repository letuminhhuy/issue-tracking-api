using IssueTrackingAPI.DTOs.Comments;

namespace IssueTrackingAPI.Services;

public interface ICommentService
{
    Task<List<CommentDto>> GetCommentsAsync(int projectId, int issueId, int userId, bool isAdmin);
    Task<CommentDto> AddCommentAsync(int projectId, int issueId, CreateCommentDto dto, int userId, bool isAdmin);
    Task<CommentDto> UpdateCommentAsync(int projectId, int issueId, int commentId, UpdateCommentDto dto, int userId, bool isAdmin);
    Task DeleteCommentAsync(int projectId, int issueId, int commentId, int userId, bool isAdmin);
}

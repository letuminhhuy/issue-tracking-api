using IssueTrackingAPI.Common;
using IssueTrackingAPI.DTOs.Comments;
using IssueTrackingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IssueTrackingAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/issues/{issueId:int}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetAll(int projectId, int issueId)
    {
        var result = await _commentService.GetCommentsAsync(projectId, issueId, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create(int projectId, int issueId, CreateCommentDto dto)
    {
        var result = await _commentService.AddCommentAsync(projectId, issueId, dto, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpPut("{commentId:int}")]
    public async Task<ActionResult<CommentDto>> Update(int projectId, int issueId, int commentId, UpdateCommentDto dto)
    {
        var result = await _commentService.UpdateCommentAsync(projectId, issueId, commentId, dto, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int projectId, int issueId, int commentId)
    {
        await _commentService.DeleteCommentAsync(projectId, issueId, commentId, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }
}

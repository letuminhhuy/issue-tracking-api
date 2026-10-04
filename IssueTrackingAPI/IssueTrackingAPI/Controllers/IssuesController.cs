using IssueTrackingAPI.Common;
using IssueTrackingAPI.DTOs.Issues;
using IssueTrackingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IssueTrackingAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/issues")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;

    public IssuesController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    [HttpGet]
    public async Task<ActionResult<List<IssueDto>>> GetAll(
        int projectId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? assigneeId)
    {
        var result = await _issueService.GetIssuesAsync(projectId, User.GetUserId(), User.IsAdmin(), status, priority, assigneeId);
        return Ok(result);
    }

    [HttpGet("{issueId:int}")]
    public async Task<ActionResult<IssueDto>> GetById(int projectId, int issueId)
    {
        var result = await _issueService.GetIssueByIdAsync(projectId, issueId, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<IssueDto>> Create(int projectId, CreateIssueDto dto)
    {
        var result = await _issueService.CreateIssueAsync(projectId, dto, User.GetUserId(), User.IsAdmin());
        return CreatedAtAction(nameof(GetById), new { projectId, issueId = result.Id }, result);
    }

    [HttpPut("{issueId:int}")]
    public async Task<ActionResult<IssueDto>> Update(int projectId, int issueId, UpdateIssueDto dto)
    {
        var result = await _issueService.UpdateIssueAsync(projectId, issueId, dto, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpDelete("{issueId:int}")]
    public async Task<IActionResult> Delete(int projectId, int issueId)
    {
        await _issueService.DeleteIssueAsync(projectId, issueId, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }
}

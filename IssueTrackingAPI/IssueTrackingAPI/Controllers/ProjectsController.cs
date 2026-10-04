using IssueTrackingAPI.Common;
using IssueTrackingAPI.DTOs.Projects;
using IssueTrackingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IssueTrackingAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll()
    {
        var result = await _projectService.GetProjectsForUserAsync(User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpGet("{projectId:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(int projectId)
    {
        var result = await _projectService.GetProjectByIdAsync(projectId, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectDto dto)
    {
        var result = await _projectService.CreateProjectAsync(dto, User.GetUserId());
        return CreatedAtAction(nameof(GetById), new { projectId = result.Id }, result);
    }

    [HttpPut("{projectId:int}")]
    public async Task<ActionResult<ProjectDto>> Update(int projectId, UpdateProjectDto dto)
    {
        var result = await _projectService.UpdateProjectAsync(projectId, dto, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpDelete("{projectId:int}")]
    public async Task<IActionResult> Delete(int projectId)
    {
        await _projectService.DeleteProjectAsync(projectId, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }

    [HttpGet("{projectId:int}/members")]
    public async Task<ActionResult<List<ProjectMemberDto>>> GetMembers(int projectId)
    {
        var result = await _projectService.GetMembersAsync(projectId, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpPost("{projectId:int}/members")]
    public async Task<ActionResult<ProjectMemberDto>> AddMember(int projectId, AddProjectMemberDto dto)
    {
        var result = await _projectService.AddMemberAsync(projectId, dto, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpPut("{projectId:int}/members/{targetUserId:int}")]
    public async Task<ActionResult<ProjectMemberDto>> UpdateMemberRole(int projectId, int targetUserId, UpdateProjectMemberRoleDto dto)
    {
        var result = await _projectService.UpdateMemberRoleAsync(projectId, targetUserId, dto, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpDelete("{projectId:int}/members/{targetUserId:int}")]
    public async Task<IActionResult> RemoveMember(int projectId, int targetUserId)
    {
        await _projectService.RemoveMemberAsync(projectId, targetUserId, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }
}

using IssueTrackingAPI.Common;
using IssueTrackingAPI.DTOs.Labels;
using IssueTrackingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IssueTrackingAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/labels")]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labelService;

    public LabelsController(ILabelService labelService)
    {
        _labelService = labelService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LabelDto>>> GetAll(int projectId)
    {
        var result = await _labelService.GetLabelsAsync(projectId, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LabelDto>> Create(int projectId, CreateLabelDto dto)
    {
        var result = await _labelService.CreateLabelAsync(projectId, dto, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpPut("{labelId:int}")]
    public async Task<ActionResult<LabelDto>> Update(int projectId, int labelId, UpdateLabelDto dto)
    {
        var result = await _labelService.UpdateLabelAsync(projectId, labelId, dto, User.GetUserId(), User.IsAdmin());
        return Ok(result);
    }

    [HttpDelete("{labelId:int}")]
    public async Task<IActionResult> Delete(int projectId, int labelId)
    {
        await _labelService.DeleteLabelAsync(projectId, labelId, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }
}

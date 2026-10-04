using IssueTrackingAPI.Common;
using IssueTrackingAPI.Data;
using IssueTrackingAPI.DTOs.Labels;
using IssueTrackingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackingAPI.Services;

public class LabelService : ILabelService
{
    private readonly AppDbContext _db;
    private readonly IProjectService _projectService;

    public LabelService(AppDbContext db, IProjectService projectService)
    {
        _db = db;
        _projectService = projectService;
    }

    public async Task<List<LabelDto>> GetLabelsAsync(int projectId, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);

        var labels = await _db.Labels
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Name)
            .ToListAsync();

        return labels.Select(ToDto).ToList();
    }

    public async Task<LabelDto> CreateLabelAsync(int projectId, CreateLabelDto dto, int userId, bool isAdmin)
    {
        // Bất kỳ thành viên nào cũng có thể tạo label để phân loại issue.
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);

        var nameTaken = await _db.Labels.AnyAsync(l => l.ProjectId == projectId && l.Name == dto.Name);
        if (nameTaken)
        {
            throw new ConflictException($"Label '{dto.Name}' đã tồn tại trong project này.");
        }

        var label = new Label
        {
            ProjectId = projectId,
            Name = dto.Name,
            Color = dto.Color
        };

        _db.Labels.Add(label);
        await _db.SaveChangesAsync();

        return ToDto(label);
    }

    public async Task<LabelDto> UpdateLabelAsync(int projectId, int labelId, UpdateLabelDto dto, int userId, bool isAdmin)
    {
        await _projectService.EnsureMemberAsync(projectId, userId, isAdmin);

        var label = await _db.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId && l.ProjectId == projectId)
            ?? throw new NotFoundException($"Không tìm thấy label #{labelId} trong project #{projectId}.");

        var nameTaken = await _db.Labels.AnyAsync(l =>
            l.ProjectId == projectId && l.Name == dto.Name && l.Id != labelId);

        if (nameTaken)
        {
            throw new ConflictException($"Label '{dto.Name}' đã tồn tại trong project này.");
        }

        label.Name = dto.Name;
        label.Color = dto.Color;

        await _db.SaveChangesAsync();
        return ToDto(label);
    }

    public async Task DeleteLabelAsync(int projectId, int labelId, int userId, bool isAdmin)
    {
        await _projectService.EnsureOwnerAsync(projectId, userId, isAdmin);

        var label = await _db.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId && l.ProjectId == projectId)
            ?? throw new NotFoundException($"Không tìm thấy label #{labelId} trong project #{projectId}.");

        // FK IssueLabel -> Label là Restrict (để tránh multiple cascade paths ở SQL Server),
        // nên phải tự xóa các liên kết IssueLabel trước khi xóa Label.
        var links = await _db.IssueLabels.Where(il => il.LabelId == labelId).ToListAsync();
        _db.IssueLabels.RemoveRange(links);

        _db.Labels.Remove(label);
        await _db.SaveChangesAsync();
    }

    private static LabelDto ToDto(Label label) => new()
    {
        Id = label.Id,
        Name = label.Name,
        Color = label.Color,
        ProjectId = label.ProjectId
    };
}

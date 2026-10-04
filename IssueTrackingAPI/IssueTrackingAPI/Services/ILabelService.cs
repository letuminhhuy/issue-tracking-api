using IssueTrackingAPI.DTOs.Labels;

namespace IssueTrackingAPI.Services;

public interface ILabelService
{
    Task<List<LabelDto>> GetLabelsAsync(int projectId, int userId, bool isAdmin);
    Task<LabelDto> CreateLabelAsync(int projectId, CreateLabelDto dto, int userId, bool isAdmin);
    Task<LabelDto> UpdateLabelAsync(int projectId, int labelId, UpdateLabelDto dto, int userId, bool isAdmin);
    Task DeleteLabelAsync(int projectId, int labelId, int userId, bool isAdmin);
}

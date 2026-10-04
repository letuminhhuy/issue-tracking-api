using System.ComponentModel.DataAnnotations;

namespace IssueTrackingAPI.DTOs.Labels;

public class CreateLabelDto
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color phải là mã hex, ví dụ #FF5733")]
    public string Color { get; set; } = "#808080";
}

public class UpdateLabelDto
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color phải là mã hex, ví dụ #FF5733")]
    public string Color { get; set; } = "#808080";
}

public class LabelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int ProjectId { get; set; }
}

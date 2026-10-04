namespace IssueTrackingAPI.Models;

public class Label
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Mã màu hex, ví dụ #FF5733</summary>
    public string Color { get; set; } = "#808080";

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public ICollection<IssueLabel> IssueLabels { get; set; } = new List<IssueLabel>();
}

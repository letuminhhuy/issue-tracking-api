using System.Net;
using System.Net.Http.Json;
using IssueTrackingAPI.DTOs.Issues;
using IssueTrackingAPI.DTOs.Labels;
using IssueTrackingAPI.DTOs.Projects;
using Xunit;

namespace IssueTrackingAPI.Tests;

public class LabelTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public LabelTests()
    {
        _client = _factory.CreateClient();
    }

    private async Task<ProjectDto> CreateProjectAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { Name = "Du an Label" });
        return (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
    }

    [Fact]
    public async Task CreateLabel_DuplicateNameInSameProject_Returns409()
    {
        var owner = await _client.RegisterAsync("lowner1", "lowner1@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        await _client.PostAsJsonAsync($"/api/projects/{project.Id}/labels", new { Name = "bug", Color = "#FF0000" });
        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/labels",
            new { Name = "bug", Color = "#00FF00" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabel_InvalidColorFormat_Returns400()
    {
        var owner = await _client.RegisterAsync("lowner2", "lowner2@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/labels",
            new { Name = "urgent", Color = "khong-phai-hex" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonOwnerMember_CannotDeleteLabel_Returns403()
    {
        var owner = await _client.RegisterAsync("lowner3", "lowner3@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var labelResp = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/labels",
            new { Name = "bug", Color = "#FF0000" });
        var label = await labelResp.Content.ReadFromJsonAsync<LabelDto>();

        var member = await _client.RegisterAsync("lmember", "lmember@test.com");
        await _client.PostAsJsonAsync($"/api/projects/{project.Id}/members",
            new { UserId = member.User.Id, Role = "Member" });

        _client.AuthorizeAs(member);
        var response = await _client.DeleteAsync($"/api/projects/{project.Id}/labels/{label!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLabel_RemovesItFromIssuesUsingIt()
    {
        var owner = await _client.RegisterAsync("lowner4", "lowner4@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var labelResp = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/labels",
            new { Name = "bug", Color = "#FF0000" });
        var label = await labelResp.Content.ReadFromJsonAsync<LabelDto>();

        var issueResp = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/issues",
            new { Title = "Issue co label", LabelIds = new[] { label!.Id } });
        var issue = await issueResp.Content.ReadFromJsonAsync<IssueDto>();
        Assert.Single(issue!.Labels);

        var deleteResp = await _client.DeleteAsync($"/api/projects/{project.Id}/labels/{label.Id}");
        deleteResp.EnsureSuccessStatusCode();

        var getIssueResp = await _client.GetAsync($"/api/projects/{project.Id}/issues/{issue.Id}");
        var updatedIssue = await getIssueResp.Content.ReadFromJsonAsync<IssueDto>();

        Assert.Empty(updatedIssue!.Labels);
    }

    public void Dispose() => _factory.Dispose();
}

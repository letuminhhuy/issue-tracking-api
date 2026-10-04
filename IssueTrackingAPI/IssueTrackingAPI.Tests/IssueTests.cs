using System.Net;
using System.Net.Http.Json;
using IssueTrackingAPI.DTOs.Issues;
using IssueTrackingAPI.DTOs.Projects;
using Xunit;

namespace IssueTrackingAPI.Tests;

public class IssueTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public IssueTests()
    {
        _client = _factory.CreateClient();
    }

    private async Task<ProjectDto> CreateProjectAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { Name = "Du an Issue", Description = "" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
    }

    [Fact]
    public async Task CreateIssue_WithoutStatusPriority_DefaultsToOpenAndMedium()
    {
        var owner = await _client.RegisterAsync("iowner1", "iowner1@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/issues",
            new { Title = "Bug dang nhap" });
        response.EnsureSuccessStatusCode();
        var issue = await response.Content.ReadFromJsonAsync<IssueDto>();

        Assert.Equal("Open", issue!.Status);
        Assert.Equal("Medium", issue.Priority);
    }

    [Fact]
    public async Task CreateIssue_AssigneeNotProjectMember_Returns400()
    {
        var owner = await _client.RegisterAsync("iowner2", "iowner2@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var outsider = await _client.RegisterAsync("ioutsider", "ioutsider@test.com");

        _client.AuthorizeAs(owner);
        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/issues",
            new { Title = "Bug gi do", AssigneeId = outsider.User.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonMember_CannotCreateIssue_Returns403()
    {
        var owner = await _client.RegisterAsync("iowner3", "iowner3@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var outsider = await _client.RegisterAsync("ioutsider2", "ioutsider2@test.com");
        _client.AuthorizeAs(outsider);

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/issues",
            new { Title = "Issue cua outsider" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RandomMember_CannotUpdateIssue_NotReporterOrAssigneeOrOwner_Returns403()
    {
        var owner = await _client.RegisterAsync("iowner4", "iowner4@test.com"); // Admin, Owner project
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var createIssueResp = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/issues",
            new { Title = "Issue cua owner" });
        var issue = await createIssueResp.Content.ReadFromJsonAsync<IssueDto>();

        // Thêm member bình thường vào project (không phải reporter/assignee/owner của issue này).
        var randomMember = await _client.RegisterAsync("irandom", "irandom@test.com");
        await _client.PostAsJsonAsync($"/api/projects/{project.Id}/members",
            new { UserId = randomMember.User.Id, Role = "Member" });

        _client.AuthorizeAs(randomMember);
        var response = await _client.PutAsJsonAsync($"/api/projects/{project.Id}/issues/{issue!.Id}",
            new { Title = "Sua bai", Status = "InProgress", Priority = "High" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FilterByStatus_OnlyReturnsMatchingIssues()
    {
        var owner = await _client.RegisterAsync("iowner5", "iowner5@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        await _client.PostAsJsonAsync($"/api/projects/{project.Id}/issues", new { Title = "Issue Open 1" });
        var createResp = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/issues",
            new { Title = "Issue InProgress 1", Status = "InProgress" });
        var created = await createResp.Content.ReadFromJsonAsync<IssueDto>();

        var listResp = await _client.GetAsync($"/api/projects/{project.Id}/issues?status=InProgress");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.Content.ReadFromJsonAsync<List<IssueDto>>();

        Assert.Single(list!);
        Assert.Equal(created!.Id, list![0].Id);
    }

    [Fact]
    public async Task InvalidStatusFilter_Returns400()
    {
        var owner = await _client.RegisterAsync("iowner6", "iowner6@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var response = await _client.GetAsync($"/api/projects/{project.Id}/issues?status=KhongTonTai");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}

using System.Net;
using System.Net.Http.Json;
using IssueTrackingAPI.DTOs.Comments;
using IssueTrackingAPI.DTOs.Issues;
using IssueTrackingAPI.DTOs.Projects;
using Xunit;

namespace IssueTrackingAPI.Tests;

public class CommentTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public CommentTests()
    {
        _client = _factory.CreateClient();
    }

    private async Task<(ProjectDto project, IssueDto issue)> SetupProjectWithIssueAsync()
    {
        var projResp = await _client.PostAsJsonAsync("/api/projects", new { Name = "Du an Comment" });
        var project = (await projResp.Content.ReadFromJsonAsync<ProjectDto>())!;

        var issueResp = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/issues",
            new { Title = "Issue co comment" });
        var issue = (await issueResp.Content.ReadFromJsonAsync<IssueDto>())!;

        return (project, issue);
    }

    [Fact]
    public async Task AddComment_Success()
    {
        var owner = await _client.RegisterAsync("cowner1", "cowner1@test.com");
        _client.AuthorizeAs(owner);
        var (project, issue) = await SetupProjectWithIssueAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/issues/{issue.Id}/comments",
            new { Content = "Binh luan dau tien" });

        response.EnsureSuccessStatusCode();
        var comment = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.Equal("Binh luan dau tien", comment!.Content);
    }

    [Fact]
    public async Task NonAuthorNonOwner_CannotEditComment_Returns403()
    {
        var owner = await _client.RegisterAsync("cowner2", "cowner2@test.com");
        _client.AuthorizeAs(owner);
        var (project, issue) = await SetupProjectWithIssueAsync();

        var commentResp = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/issues/{issue.Id}/comments",
            new { Content = "Comment cua owner" });
        var comment = await commentResp.Content.ReadFromJsonAsync<CommentDto>();

        var otherMember = await _client.RegisterAsync("cmember", "cmember@test.com");
        await _client.PostAsJsonAsync($"/api/projects/{project.Id}/members",
            new { UserId = otherMember.User.Id, Role = "Member" });

        _client.AuthorizeAs(otherMember);
        var response = await _client.PutAsJsonAsync(
            $"/api/projects/{project.Id}/issues/{issue.Id}/comments/{comment!.Id}",
            new { Content = "Sua trom" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CommentOnIssue_NotBelongingToProject_Returns404()
    {
        var owner = await _client.RegisterAsync("cowner3", "cowner3@test.com");
        _client.AuthorizeAs(owner);
        var (project, _) = await SetupProjectWithIssueAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/issues/999999/comments",
            new { Content = "Issue khong ton tai" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}

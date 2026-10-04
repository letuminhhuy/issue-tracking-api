using System.Net;
using System.Net.Http.Json;
using IssueTrackingAPI.DTOs.Projects;
using Xunit;

namespace IssueTrackingAPI.Tests;

public class ProjectTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public ProjectTests()
    {
        _client = _factory.CreateClient();
    }

    private async Task<ProjectDto> CreateProjectAsync(string name = "Du an A")
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { Name = name, Description = "mo ta" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
    }

    [Fact]
    public async Task CreateProject_CreatorBecomesOwner()
    {
        var admin = await _client.RegisterAsync("owner1", "owner1@test.com"); // user đầu = Admin
        _client.AuthorizeAs(admin);

        var project = await CreateProjectAsync();

        var membersResponse = await _client.GetAsync($"/api/projects/{project.Id}/members");
        membersResponse.EnsureSuccessStatusCode();
        var members = await membersResponse.Content.ReadFromJsonAsync<List<ProjectMemberDto>>();

        Assert.Single(members!);
        Assert.Equal("Owner", members![0].Role);
    }

    [Fact]
    public async Task NonMember_CannotAccessProject_Returns403()
    {
        var owner = await _client.RegisterAsync("owner2", "owner2@test.com"); // Admin
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        // User thứ 2, không phải Admin, không phải thành viên project.
        var outsider = await _client.RegisterAsync("outsider", "outsider@test.com");
        _client.AuthorizeAs(outsider);

        var response = await _client.GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_GetNonExistentProject_Returns404NotCrash()
    {
        // Bug đã sửa: trước đây Admin xem project không tồn tại bị 500
        // (InvalidOperationException ở LoadProjectAsync.FirstAsync).
        var admin = await _client.RegisterAsync("admin1", "admin1@test.com");
        _client.AuthorizeAs(admin);

        var response = await _client.GetAsync("/api/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NonOwnerMember_CannotAddMember_Returns403()
    {
        var owner = await _client.RegisterAsync("owner3", "owner3@test.com"); // Admin
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var member = await _client.RegisterAsync("member1", "member1@test.com");

        // Owner thêm member1 vào project với role Member.
        _client.AuthorizeAs(owner);
        var addResp = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/members",
            new { UserId = member.User.Id, Role = "Member" });
        addResp.EnsureSuccessStatusCode();

        // member1 (role Member trong project) thử tự thêm người khác -> phải bị cấm.
        var another = await _client.RegisterAsync("member2", "member2@test.com");
        _client.AuthorizeAs(member);
        var forbiddenResp = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/members",
            new { UserId = another.User.Id, Role = "Member" });

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);
    }

    [Fact]
    public async Task LastOwner_CannotDemoteSelf_Returns400()
    {
        // Bug đã sửa: UpdateMemberRoleAsync trước đây không chặn việc hạ quyền
        // Owner cuối cùng, khiến project có thể mất Owner hoàn toàn.
        var owner = await _client.RegisterAsync("owner4", "owner4@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/projects/{project.Id}/members/{owner.User.Id}",
            new { Role = "Member" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LastOwner_CannotBeRemoved_Returns400()
    {
        var owner = await _client.RegisterAsync("owner5", "owner5@test.com");
        _client.AuthorizeAs(owner);
        var project = await CreateProjectAsync();

        var response = await _client.DeleteAsync($"/api/projects/{project.Id}/members/{owner.User.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}

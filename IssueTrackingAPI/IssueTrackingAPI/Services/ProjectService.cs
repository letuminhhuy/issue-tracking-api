using IssueTrackingAPI.Common;
using IssueTrackingAPI.Data;
using IssueTrackingAPI.DTOs.Projects;
using IssueTrackingAPI.Models;
using IssueTrackingAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackingAPI.Services;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProjectDto>> GetProjectsForUserAsync(int userId, bool isAdmin)
    {
        var query = _db.Projects
            .Include(p => p.CreatedByUser)
            .Include(p => p.Members)
            .Include(p => p.Issues)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(p => p.Members.Any(m => m.UserId == userId));
        }

        var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return projects.Select(ToDto).ToList();
    }

    public async Task<ProjectDto> GetProjectByIdAsync(int projectId, int userId, bool isAdmin)
    {
        await EnsureMemberAsync(projectId, userId, isAdmin);

        var project = await LoadProjectAsync(projectId);
        return ToDto(project);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, int userId)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedByUserId = userId
        };

        project.Members.Add(new ProjectMember
        {
            UserId = userId,
            Role = ProjectRole.Owner
        });

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var created = await LoadProjectAsync(project.Id);
        return ToDto(created);
    }

    public async Task<ProjectDto> UpdateProjectAsync(int projectId, UpdateProjectDto dto, int userId, bool isAdmin)
    {
        await EnsureOwnerAsync(projectId, userId, isAdmin);

        var project = await _db.Projects.FindAsync(projectId)
            ?? throw new NotFoundException($"Không tìm thấy project #{projectId}.");

        project.Name = dto.Name;
        project.Description = dto.Description;

        await _db.SaveChangesAsync();

        var updated = await LoadProjectAsync(projectId);
        return ToDto(updated);
    }

    public async Task DeleteProjectAsync(int projectId, int userId, bool isAdmin)
    {
        await EnsureOwnerAsync(projectId, userId, isAdmin);

        var project = await _db.Projects.FindAsync(projectId)
            ?? throw new NotFoundException($"Không tìm thấy project #{projectId}.");

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ProjectMemberDto>> GetMembersAsync(int projectId, int userId, bool isAdmin)
    {
        await EnsureMemberAsync(projectId, userId, isAdmin);

        var members = await _db.ProjectMembers
            .Include(m => m.User)
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();

        return members.Select(ToMemberDto).ToList();
    }

    public async Task<ProjectMemberDto> AddMemberAsync(int projectId, AddProjectMemberDto dto, int userId, bool isAdmin)
    {
        await EnsureOwnerAsync(projectId, userId, isAdmin);

        if (!await _db.Projects.AnyAsync(p => p.Id == projectId))
        {
            throw new NotFoundException($"Không tìm thấy project #{projectId}.");
        }

        var targetUser = await _db.Users.FindAsync(dto.UserId)
            ?? throw new NotFoundException($"Không tìm thấy user #{dto.UserId}.");

        var alreadyMember = await _db.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == dto.UserId);

        if (alreadyMember)
        {
            throw new ConflictException("User này đã là thành viên của project.");
        }

        if (!Enum.TryParse<ProjectRole>(dto.Role, true, out var role))
        {
            throw new BadRequestException("Role không hợp lệ. Chỉ nhận 'Owner' hoặc 'Member'.");
        }

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = dto.UserId,
            Role = role
        };

        _db.ProjectMembers.Add(member);
        await _db.SaveChangesAsync();

        member.User = targetUser;
        return ToMemberDto(member);
    }

    public async Task<ProjectMemberDto> UpdateMemberRoleAsync(int projectId, int targetUserId, UpdateProjectMemberRoleDto dto, int userId, bool isAdmin)
    {
        await EnsureOwnerAsync(projectId, userId, isAdmin);

        var member = await _db.ProjectMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == targetUserId)
            ?? throw new NotFoundException("Không tìm thấy thành viên này trong project.");

        if (!Enum.TryParse<ProjectRole>(dto.Role, true, out var role))
        {
            throw new BadRequestException("Role không hợp lệ. Chỉ nhận 'Owner' hoặc 'Member'.");
        }

        if (member.Role == ProjectRole.Owner && role != ProjectRole.Owner)
        {
            var ownerCount = await _db.ProjectMembers
                .CountAsync(m => m.ProjectId == projectId && m.Role == ProjectRole.Owner);

            if (ownerCount <= 1)
            {
                throw new BadRequestException("Không thể hạ quyền của Owner cuối cùng trong project.");
            }
        }

        member.Role = role;
        await _db.SaveChangesAsync();

        return ToMemberDto(member);
    }

    public async Task RemoveMemberAsync(int projectId, int targetUserId, int userId, bool isAdmin)
    {
        await EnsureOwnerAsync(projectId, userId, isAdmin);

        var member = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == targetUserId)
            ?? throw new NotFoundException("Không tìm thấy thành viên này trong project.");

        var ownerCount = await _db.ProjectMembers
            .CountAsync(m => m.ProjectId == projectId && m.Role == ProjectRole.Owner);

        if (member.Role == ProjectRole.Owner && ownerCount <= 1)
        {
            throw new BadRequestException("Không thể xóa Owner cuối cùng của project.");
        }

        _db.ProjectMembers.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task EnsureMemberAsync(int projectId, int userId, bool isAdmin)
    {
        // Luôn kiểm tra project có tồn tại trước, bất kể Admin hay không.
        // Trước đây Admin được return sớm, khiến project không tồn tại
        // bị crash ở LoadProjectAsync (FirstAsync) với lỗi 500 thay vì 404.
        var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
        {
            throw new NotFoundException($"Không tìm thấy project #{projectId}.");
        }

        if (isAdmin) return;

        var isMember = await _db.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);

        if (!isMember)
        {
            throw new ForbiddenException("Bạn không phải thành viên của project này.");
        }
    }

    public async Task EnsureOwnerAsync(int projectId, int userId, bool isAdmin)
    {
        var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
        {
            throw new NotFoundException($"Không tìm thấy project #{projectId}.");
        }

        if (isAdmin) return;

        var isOwner = await _db.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId && m.Role == ProjectRole.Owner);

        if (!isOwner)
        {
            throw new ForbiddenException("Chỉ Owner của project mới có quyền thực hiện hành động này.");
        }
    }

    private Task<Project> LoadProjectAsync(int projectId)
    {
        return _db.Projects
            .Include(p => p.CreatedByUser)
            .Include(p => p.Members)
            .Include(p => p.Issues)
            .FirstAsync(p => p.Id == projectId);
    }

    private static ProjectDto ToDto(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        CreatedByUserId = project.CreatedByUserId,
        CreatedByUsername = project.CreatedByUser?.Username ?? string.Empty,
        CreatedAt = project.CreatedAt,
        MemberCount = project.Members.Count,
        IssueCount = project.Issues.Count
    };

    private static ProjectMemberDto ToMemberDto(ProjectMember member) => new()
    {
        UserId = member.UserId,
        Username = member.User?.Username ?? string.Empty,
        Email = member.User?.Email ?? string.Empty,
        Role = member.Role.ToString(),
        JoinedAt = member.JoinedAt
    };
}

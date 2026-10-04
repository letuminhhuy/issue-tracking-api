using IssueTrackingAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IssueTrackingAPI.Tests;

/// <summary>
/// Factory dựng app lên trong bộ nhớ để test, thay DbContext SQL Server
/// bằng SQLite in-memory — không cần SQL Server thật, không đụng tới
/// database thật của bạn. Mỗi instance factory = 1 database riêng, sạch.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        // Giữ connection mở suốt đời factory để DB in-memory không bị xóa.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Test phải tự chứa, không phụ thuộc Jwt:Key thật từ User Secrets của máy
        // đang chạy — nếu không, test pass trên máy có secret (Development tự load
        // User Secrets) nhưng fail 500 trên CI/máy khác (Jwt:Key trong
        // appsettings.json chỉ là placeholder rỗng). Phải set bằng env var (không
        // phải ConfigureAppConfiguration) vì Program.cs đọc Jwt:Key RA TRƯỚC khi
        // gọi builder.Build() — lúc đó các hook của WebApplicationFactory
        // (ConfigureAppConfiguration/ConfigureServices) chưa kịp áp dụng. Env var
        // thì được "WebApplication.CreateBuilder()" đọc ngay từ dòng đầu Program.cs,
        // miễn là set trước khi factory tạo host (ở đây, trong constructor).
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-not-a-real-secret-32chars+");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "IssueTrackingAPI");
        Environment.SetEnvironmentVariable("Jwt__Audience", "IssueTrackingAPIClient");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "120");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

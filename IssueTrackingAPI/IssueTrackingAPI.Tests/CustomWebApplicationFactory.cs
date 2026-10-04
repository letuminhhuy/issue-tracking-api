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

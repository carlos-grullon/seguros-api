using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SegurosApi.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                  ?? throw new InvalidOperationException(
                      "Set env var ConnectionStrings__DefaultConnection before running EF tools.\n" +
                      "Example: export ConnectionStrings__DefaultConnection=\"postgresql://user:pass@host:5432/db\"");

        var connectionString = NormalizeConnectionString(raw);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static string NormalizeConnectionString(string raw)
    {
        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo.Split(':', 2);
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(database)) database = "postgres";
            var port = uri.IsDefaultPort ? 5432 : uri.Port;
            return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Require;Trust Server Certificate=true";
        }
        return raw;
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OmniFlow.Infrastructure.Contexts;

namespace OmniFlow.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = ResolveConnectionString();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var environmentConnection =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrWhiteSpace(environmentConnection))
        {
            return environmentConnection;
        }

        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory is not null)
        {
            var appSettingsPath = Path.Combine(
                currentDirectory.FullName,
                "OmniFlow.WebApi",
                "appsettings.json");

            if (File.Exists(appSettingsPath))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));

                if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings) &&
                    connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection) &&
                    !string.IsNullOrWhiteSpace(defaultConnection.GetString()))
                {
                    return defaultConnection.GetString()!;
                }
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException(
            "No database connection string was found for EF design-time operations. " +
            "Set ConnectionStrings__DefaultConnection or DATABASE_URL, " +
            "or ensure OmniFlow.WebApi/appsettings.json contains ConnectionStrings:DefaultConnection.");
    }
}

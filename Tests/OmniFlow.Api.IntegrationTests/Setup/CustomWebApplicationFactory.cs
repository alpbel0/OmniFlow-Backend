using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OmniFlow.Application.DTOs.Routes;
using OmniFlow.Application.Interfaces;
using OmniFlow.Infrastructure.Contexts;

namespace OmniFlow.Api.IntegrationTests.Setup;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    public TestEmailService EmailService { get; } = new();

    static CustomWebApplicationFactory()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public CustomWebApplicationFactory()
    {
        _connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=omniflow_dev;Username=postgres;Password=postgres";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the real DbContext with the test database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_connectionString));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(EmailService);
            services.RemoveAll<IBlobService>();
            services.AddSingleton<IBlobService, TestBlobService>();
            services.RemoveAll<IGeocodingService>();
            services.AddSingleton<IGeocodingService, TestGeocodingService>();
            services.RemoveAll<IRoutingService>();
            services.AddSingleton<IRoutingService, TestRoutingService>();

            // Ensure test DB schema is up to date.
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }

    private sealed class TestBlobService : IBlobService
    {
        public Task<string> UploadAsync(
            Stream stream,
            string contentType,
            string? originalFileName,
            string? folder = null,
            CancellationToken cancellationToken = default)
        {
            var safeFolder = string.IsNullOrWhiteSpace(folder) ? "root" : folder.Trim('/');
            var safeFileName = string.IsNullOrWhiteSpace(originalFileName) ? "upload.bin" : originalFileName;
            return Task.FromResult($"https://blob.test/{safeFolder}/{safeFileName}");
        }
    }

    private sealed class TestRoutingService : IRoutingService
    {
        public Task<RouteDetailDto> GetRouteAsync(
            string profile,
            double fromLatitude,
            double fromLongitude,
            double toLatitude,
            double toLongitude,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RouteDetailDto
            {
                Coordinates =
                [
                    [fromLongitude, fromLatitude],
                    [toLongitude, toLatitude]
                ],
                DistanceMeters = 1000,
                DurationSeconds = profile switch
                {
                    "driving-car" => 300,
                    "foot-hiking" => 700,
                    _ => 900
                }
            });
        }
    }
}

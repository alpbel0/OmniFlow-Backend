using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Api.IntegrationTests.Setup;

public static class TestDatabaseSeeder
{
    public const string TestUserEmail = "testuser@omniflow.com";
    public const string TestUserPassword = "TestUser123!";
    public const string TestUserUsername = "testuser";

    public const string AdminEmail = "admin@omniflow.com";
    public const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();

        // Seed roles
        foreach (var role in new[] { Roles.Traveler.ToString(), Roles.Admin.ToString() })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        // Seed test traveler user
        if (await userManager.FindByEmailAsync(TestUserEmail) is null)
        {
            var appUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = TestUserUsername,
                Email = TestUserEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(appUser, TestUserPassword);
            await userManager.AddToRoleAsync(appUser, Roles.Traveler.ToString());

            dbContext.Users.Add(new User
            {
                Id = appUser.Id,
                Username = TestUserUsername,
                Email = TestUserEmail,
                Role = Roles.Traveler,
                IsVerified = false
            });
            await dbContext.SaveChangesAsync();
        }

        // Seed admin user
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                Email = AdminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, AdminPassword);
            await userManager.AddToRoleAsync(adminUser, Roles.Admin.ToString());

            dbContext.Users.Add(new User
            {
                Id = adminUser.Id,
                Username = "admin",
                Email = AdminEmail,
                Role = Roles.Admin,
                IsVerified = true
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Clears feed-related tables for test isolation.
    /// Use before tests that depend on specific feed state.
    /// </summary>
    public static async Task ClearFeedDataAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();

        // Clear in correct order to avoid foreign key violations
        dbContext.CommentUpvotes.RemoveRange(dbContext.CommentUpvotes);
        dbContext.PostUpvotes.RemoveRange(dbContext.PostUpvotes);
        dbContext.Comments.RemoveRange(dbContext.Comments);
        dbContext.Posts.RemoveRange(dbContext.Posts);
        dbContext.Follows.RemoveRange(dbContext.Follows);

        await dbContext.SaveChangesAsync();
    }

    public static async Task CleanRefreshTokensAsync(IServiceProvider serviceProvider, Guid userId)
    {
        var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();
        var tokens = dbContext.RefreshTokens.Where(t => t.UserId == userId).ToList();
        dbContext.RefreshTokens.RemoveRange(tokens);
        await dbContext.SaveChangesAsync();
    }
}

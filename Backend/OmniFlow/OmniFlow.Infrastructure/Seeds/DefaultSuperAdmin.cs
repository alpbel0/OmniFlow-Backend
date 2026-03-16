using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Infrastructure.Seeds;

public static class DefaultSuperAdmin
{
	public static async Task SeedAsync(
		UserManager<ApplicationUser> userManager,
		RoleManager<IdentityRole<Guid>> roleManager,
		IApplicationDbContext dbContext)
	{
		const string adminEmail = "admin@omniflow.com";
		const string adminUserName = "admin";
		const string adminPassword = "Admin123!";

		if (!await roleManager.RoleExistsAsync(Roles.Admin.ToString()))
		{
			await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin.ToString()));
		}

		var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

		if (existingAdmin is null)
		{
			existingAdmin = new ApplicationUser
			{
				Id = Guid.NewGuid(),
				UserName = adminUserName,
				Email = adminEmail,
				EmailConfirmed = true
			};

			var createResult = await userManager.CreateAsync(existingAdmin, adminPassword);
			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(
					$"Super admin user could not be created: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
			}
		}

		if (!await userManager.IsInRoleAsync(existingAdmin, Roles.Admin.ToString()))
		{
			await userManager.AddToRoleAsync(existingAdmin, Roles.Admin.ToString());
		}

		var domainUserExists = await dbContext.Users.AnyAsync(u => u.Id == existingAdmin.Id);
		if (!domainUserExists)
		{
			dbContext.Users.Add(new User
			{
				Id = existingAdmin.Id,
				Username = existingAdmin.UserName ?? adminUserName,
				Email = existingAdmin.Email ?? adminEmail,
				Bio = "OmniFlow Super Admin",
				KarmaScore = 0,
				Role = Roles.Admin,
				IsVerified = true
			});

			await dbContext.SaveChangesAsync();
		}
	}
}

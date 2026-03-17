using Microsoft.AspNetCore.Identity;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Seeds;

public static class DefaultRoles
{
	public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
	{
		var roles = new[]
		{
			Roles.Traveler.ToString(),
			Roles.Admin.ToString()
		};

		foreach (var roleName in roles)
		{
			if (!await roleManager.RoleExistsAsync(roleName))
			{
				await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
			}
		}
	}
}

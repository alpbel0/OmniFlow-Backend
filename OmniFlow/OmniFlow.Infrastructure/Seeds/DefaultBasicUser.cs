using Microsoft.AspNetCore.Identity;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Infrastructure.Seeds;

public static class DefaultBasicUser
{
	public static async Task SeedAsync(
		UserManager<ApplicationUser> userManager,
		RoleManager<IdentityRole<Guid>> roleManager)
	{
		const string travelerEmail = "traveler@omniflow.com";
		const string travelerUserName = "traveler";
		const string travelerPassword = "Traveler123!";

		if (!await roleManager.RoleExistsAsync(Roles.Traveler.ToString()))
		{
			await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Traveler.ToString()));
		}

		var existingTraveler = await userManager.FindByEmailAsync(travelerEmail);
		if (existingTraveler is null)
		{
			existingTraveler = new ApplicationUser
			{
				Id = Guid.NewGuid(),
				UserName = travelerUserName,
				Email = travelerEmail,
				EmailConfirmed = true
			};

			var createResult = await userManager.CreateAsync(existingTraveler, travelerPassword);
			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(
					$"Basic traveler user could not be created: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
			}
		}

		if (!await userManager.IsInRoleAsync(existingTraveler, Roles.Traveler.ToString()))
		{
			await userManager.AddToRoleAsync(existingTraveler, Roles.Traveler.ToString());
		}
	}
}

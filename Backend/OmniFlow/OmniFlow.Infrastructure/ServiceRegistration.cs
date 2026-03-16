using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Application.Interfaces;
using OmniFlow.Infrastructure.Contexts;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Infrastructure;

public static class ServiceRegistration
{
	public static IServiceCollection AddInfrastructureLayer(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

		services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
		services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

		services
			.AddIdentityCore<ApplicationUser>()
			.AddRoles<IdentityRole<Guid>>()
			.AddEntityFrameworkStores<ApplicationDbContext>();

		return services;
	}
}

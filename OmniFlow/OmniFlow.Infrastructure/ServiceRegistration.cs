using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Settings;
using OmniFlow.Infrastructure.Contexts;
using OmniFlow.Infrastructure.Models;
using OmniFlow.Infrastructure.Repositories;
using OmniFlow.Infrastructure.Services;

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

		services.Configure<JWTSettings>(options =>
			configuration.GetSection("JWTSettings").Bind(options));
		services.AddScoped<IAccountService, AccountService>();

		// Open-Generic DI registration for Generic Repository
		services.AddScoped(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));

		// Specific Repository registrations
		services.AddScoped<IPlaceRepositoryAsync, PlaceRepositoryAsync>();
		services.AddScoped<ITripRepositoryAsync, TripRepositoryAsync>();

		return services;
	}
}
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
		services.Configure<AzureStorageSettings>(options =>
			configuration.GetSection("AzureStorageSettings").Bind(options));
		services.AddSingleton(sp =>
		{
			var opts = sp.GetRequiredService<IOptions<AzureStorageSettings>>().Value;
			if (string.IsNullOrWhiteSpace(opts.ConnectionString))
				throw new InvalidOperationException("AzureStorageSettings:ConnectionString is missing.");
			return new BlobServiceClient(opts.ConnectionString);
		});
		services.AddScoped<IAccountService, AccountService>();
		services.AddScoped<IBlobService, BlobService>();

		// Open-Generic DI registration for Generic Repository
		services.AddScoped(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));

		// Specific Repository registrations
		services.AddScoped<IPlaceRepositoryAsync, PlaceRepositoryAsync>();
		services.AddScoped<ITripRepositoryAsync, TripRepositoryAsync>();
		services.AddScoped<IStopRepositoryAsync, StopRepositoryAsync>();
		services.AddScoped<IFlightRepositoryAsync, FlightRepositoryAsync>();
		services.AddScoped<IHotelRepositoryAsync, HotelRepositoryAsync>();
		services.AddScoped<IUserRepositoryAsync, UserRepositoryAsync>();
		services.AddScoped<IFollowRepositoryAsync, FollowRepositoryAsync>();
		services.AddScoped<IPostRepositoryAsync, PostRepositoryAsync>();
		services.AddScoped<ICommentRepositoryAsync, CommentRepositoryAsync>();
		services.AddScoped<ICommunityTipRepositoryAsync, CommunityTipRepositoryAsync>();
		services.AddScoped<INotificationRepositoryAsync, NotificationRepositoryAsync>();

		return services;
	}
}
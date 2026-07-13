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
using OmniFlow.Infrastructure.Settings;

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
			.AddEntityFrameworkStores<ApplicationDbContext>()
			.AddDefaultTokenProviders();

		services.Configure<JWTSettings>(options =>
			configuration.GetSection("JWTSettings").Bind(options));
		services.Configure<MailSettings>(options =>
			configuration.GetSection("MailSettings").Bind(options));
		services
			.AddOptions<GoogleAuthSettings>()
			.Bind(configuration.GetSection("GoogleAuth"))
			.Validate(settings => settings.AllowedClientIds.Any(id => !string.IsNullOrWhiteSpace(id)),
				"GoogleAuth:AllowedClientIds must contain at least one client id.")
			.ValidateOnStart();
		services.Configure<DataProtectionTokenProviderOptions>(options =>
		{
			options.TokenLifespan = TimeSpan.FromHours(24);
		});
		services.Configure<AzureStorageSettings>(options =>
			configuration.GetSection("AzureStorageSettings").Bind(options));
		services.Configure<GeocodingSettings>(options =>
			configuration.GetSection("Geocoding").Bind(options));
		services.Configure<OpenRouteServiceSettings>(options =>
			configuration.GetSection("Routing:OpenRouteService").Bind(options));
		services.AddSingleton(sp =>
		{
			var opts = sp.GetRequiredService<IOptions<AzureStorageSettings>>().Value;
			if (string.IsNullOrWhiteSpace(opts.ConnectionString))
				throw new InvalidOperationException("AzureStorageSettings:ConnectionString is missing.");
			return new BlobServiceClient(opts.ConnectionString);
		});
		services.AddScoped<IAccountService, AccountService>();
		services.AddSingleton<IDateTimeService, SystemDateTimeService>();
		services.AddScoped<IEmailService, EmailService>();
		services.AddScoped<IBlobService, BlobService>();
		services.AddSingleton<IGoogleJsonWebSignatureValidator, GoogleJsonWebSignatureValidator>();
		services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
		services.AddSingleton<IScoringService, ScoringService>();
		services.AddScoped<IBudgetCalculationService, BudgetCalculationService>();
		services.AddSingleton<ITimelineService, TimelineService>();
		services.AddScoped<IRecommendationService, RecommendationService>();
		services.AddHttpClient<IGeocodingService, NominatimGeocodingService>((sp, client) =>
		{
			var settings = sp.GetRequiredService<IOptions<GeocodingSettings>>().Value;
			client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(settings.BaseUrl)
				? "https://nominatim.openstreetmap.org"
				: settings.BaseUrl.TrimEnd('/'));
			client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds <= 0 ? 5 : settings.TimeoutSeconds);
			client.DefaultRequestHeaders.UserAgent.ParseAdd(string.IsNullOrWhiteSpace(settings.UserAgent)
				? "OmniFlow/1.0 (+omniflowinc@gmail.com)"
				: settings.UserAgent);
		});
		services.AddHttpClient<IRoutingService, OpenRouteServiceRoutingService>((sp, client) =>
		{
			var settings = sp.GetRequiredService<IOptions<OpenRouteServiceSettings>>().Value;
			client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(settings.BaseUrl)
				? "https://api.openrouteservice.org"
				: settings.BaseUrl.TrimEnd('/'));
			client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds <= 0 ? 8 : settings.TimeoutSeconds);
		});
		services.AddMemoryCache();

		// Open-Generic DI registration for Generic Repository
		services.AddScoped(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));

		// Specific Repository registrations
		services.AddScoped<IPlaceRepositoryAsync, PlaceRepositoryAsync>();
		services.AddScoped<ITripRepositoryAsync, TripRepositoryAsync>();
		services.AddScoped<ITripDestinationRepositoryAsync, TripDestinationRepositoryAsync>();
		services.AddScoped<ITimelineEntryRepositoryAsync, TimelineEntryRepositoryAsync>();
		services.AddScoped<IFlightRepositoryAsync, FlightRepositoryAsync>();
		services.AddScoped<IHotelRepositoryAsync, HotelRepositoryAsync>();
		services.AddScoped<IProviderFlightRepositoryAsync, ProviderFlightRepositoryAsync>();
		services.AddScoped<IProviderHotelRepositoryAsync, ProviderHotelRepositoryAsync>();
		services.AddScoped<IUserRepositoryAsync, UserRepositoryAsync>();
		services.AddScoped<IFollowRepositoryAsync, FollowRepositoryAsync>();
		services.AddScoped<IPostRepositoryAsync, PostRepositoryAsync>();
		services.AddScoped<ICommentRepositoryAsync, CommentRepositoryAsync>();
		services.AddScoped<ICommunityTipRepositoryAsync, CommunityTipRepositoryAsync>();
		services.AddScoped<INotificationRepositoryAsync, NotificationRepositoryAsync>();

		return services;
	}
}

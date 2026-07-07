using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OmniFlow.Application.Settings;
using OmniFlow.Infrastructure;

namespace OmniFlow.UnitTests.Auth;

public class GoogleAuthServiceRegistrationTests
{
	[Fact]
	public void AddInfrastructureLayer_WhenGoogleClientIdsAreMissing_RegistersFailingOptionsValidation()
	{
		var configuration = BuildConfiguration(new Dictionary<string, string?>
		{
			["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=omniflow_dev;Username=postgres;Password=postgres",
			["AzureStorageSettings:ConnectionString"] = "UseDevelopmentStorage=true",
			["AzureStorageSettings:ContainerName"] = "images"
		});
		var services = new ServiceCollection();

		services.AddInfrastructureLayer(configuration);
		using var provider = services.BuildServiceProvider();

		var act = () => provider.GetRequiredService<IOptions<GoogleAuthSettings>>().Value;

		act.Should().Throw<OptionsValidationException>()
			.WithMessage("*GoogleAuth:AllowedClientIds must contain at least one client id.*");
	}

	[Fact]
	public void AddInfrastructureLayer_WhenGoogleClientIdsExist_OptionsResolveSuccessfully()
	{
		var configuration = BuildConfiguration(new Dictionary<string, string?>
		{
			["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=omniflow_dev;Username=postgres;Password=postgres",
			["AzureStorageSettings:ConnectionString"] = "UseDevelopmentStorage=true",
			["AzureStorageSettings:ContainerName"] = "images",
			["GoogleAuth:AllowedClientIds:0"] = "android-client-id"
		});
		var services = new ServiceCollection();

		services.AddInfrastructureLayer(configuration);
		using var provider = services.BuildServiceProvider();

		var options = provider.GetRequiredService<IOptions<GoogleAuthSettings>>().Value;

		options.AllowedClientIds.Should().ContainSingle().Which.Should().Be("android-client-id");
	}

	private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
	{
		return new ConfigurationBuilder()
			.AddInMemoryCollection(values)
			.Build();
	}
}

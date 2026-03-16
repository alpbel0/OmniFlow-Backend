using Microsoft.AspNetCore.Identity;
using OmniFlow.Application.Interfaces;
using OmniFlow.Infrastructure;
using OmniFlow.Infrastructure.Models;
using OmniFlow.Infrastructure.Seeds;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructureLayer(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
	var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
	var dbContext = services.GetRequiredService<IApplicationDbContext>();

	await DefaultRoles.SeedAsync(roleManager);
	await DefaultSuperAdmin.SeedAsync(userManager, roleManager, dbContext);
	await DefaultBasicUser.SeedAsync(userManager, roleManager);
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Ok("OmniFlow WebApi"));

app.Run();

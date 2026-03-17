using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Infrastructure;
using OmniFlow.Infrastructure.Contexts;
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
	try
	{
		var context = services.GetRequiredService<ApplicationDbContext>();

		// 1. ADIM: Önce tabloları oluştur (Migration)
		Console.WriteLine("Migration uygulanıyor...");
		await context.Database.MigrateAsync();

		// 2. ADIM: Tablolar oluştuktan sonra verileri ekle (Seed)
		var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
		var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
		var dbContext = services.GetRequiredService<IApplicationDbContext>();

		Console.WriteLine("Seed datalar ekleniyor...");
		await DefaultRoles.SeedAsync(roleManager);
		await DefaultSuperAdmin.SeedAsync(userManager, roleManager, dbContext);
		await DefaultBasicUser.SeedAsync(userManager, roleManager);

		Console.WriteLine("Veritabanı işlemleri başarıyla tamamlandı!");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"HATA: Veritabanı yapılandırılırken bir sorun oluştu: {ex.Message}");
		// Hata varsa burada kalsın, uygulama patlamasın diye throw etmiyoruz şimdilik
	}
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Ok("OmniFlow WebApi"));

app.Run();

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Settings;
using OmniFlow.Infrastructure;
using OmniFlow.Infrastructure.Contexts;
using OmniFlow.Infrastructure.Models;
using OmniFlow.Infrastructure.Seeds;
using OmniFlow.WebApi.Middlewares;
using OmniFlow.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Application (MediatR, AutoMapper, FluentValidation, ValidationBehaviour) ──
builder.Services.AddApplicationLayer();

// ── Infrastructure (DbContext, Identity, AccountService) ──────────────────────
builder.Services.AddInfrastructureLayer(builder.Configuration);

// ── AuthenticatedUserService ──────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JWTSettings").Get<JWTSettings>()
    ?? throw new InvalidOperationException("JWTSettings section is missing from configuration.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "https://omniflow.app")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── MVC / Controllers / Swagger ──────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OmniFlow API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddFluentValidationRulesToSwagger();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Database migration + seed ─────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        Console.WriteLine("Migration uygulanıyor...");
        await context.Database.MigrateAsync();

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
    }
}

// ── Middleware pipeline: ErrorHandler → CORS → Auth → Authorization → Controllers
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
//SwaggerUI
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Ok("OmniFlow WebApi"));

app.Run();

// Exposed for WebApplicationFactory in integration tests
public partial class Program { }

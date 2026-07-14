using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Application.Behaviours;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Mappings;
using OmniFlow.Application.Services;

namespace OmniFlow.Application;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IKarmaService, KarmaService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITripVisibilityService, TripVisibilityService>();
        services.AddScoped<ITripTemporalService, TripTemporalService>();
        services.AddScoped<IVisitLogConversionService, VisitLogConversionService>();

        return services;
    }
}

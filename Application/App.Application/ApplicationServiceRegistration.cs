//using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace App.Application { }

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile));

        return services;
    }
}


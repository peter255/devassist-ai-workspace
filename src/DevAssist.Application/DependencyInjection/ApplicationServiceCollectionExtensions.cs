using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DevAssist.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(typeof(ApplicationServiceCollectionExtensions).Assembly);
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        return services;
    }
}

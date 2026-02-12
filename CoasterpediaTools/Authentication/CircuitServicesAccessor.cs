using Microsoft.AspNetCore.Components.Server.Circuits;

namespace CoasterpediaTools.Authentication;

public class CircuitServicesAccessor
{
    private static readonly AsyncLocal<IServiceProvider> BlazorServices = new();

    public IServiceProvider? Services
    {
        get => BlazorServices.Value;
        set => BlazorServices.Value = value;
    }
}

public class ServicesAccessorCircuitHandler : CircuitHandler
{
    readonly IServiceProvider _services;
    readonly CircuitServicesAccessor _circuitServicesAccessor;

    public ServicesAccessorCircuitHandler(IServiceProvider services, CircuitServicesAccessor servicesAccessor)
    {
        _services = services;
        _circuitServicesAccessor = servicesAccessor;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            _circuitServicesAccessor.Services = _services;
            await next(context);
            _circuitServicesAccessor.Services = null;
        };
    }
}

public static class CircuitServicesServiceCollectionExtensions
{
    public static IServiceCollection AddCircuitServicesAccessor(this IServiceCollection services)
    {
        services.AddScoped<CircuitServicesAccessor>();
        services.AddScoped<CircuitHandler, ServicesAccessorCircuitHandler>();
        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;

namespace RentalX.Helpers;

public static class ServiceHelper
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static void Initialize(IServiceProvider services)
    {
        Services = services;
    }

    public static T GetRequiredService<T>() where T : notnull =>
        Services.GetRequiredService<T>();
}

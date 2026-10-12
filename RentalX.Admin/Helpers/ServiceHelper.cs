using Microsoft.Extensions.DependencyInjection;

namespace RentalX.Admin.Helpers;

public static class ServiceHelper
{
    private static IServiceProvider? _provider;

    public static void Initialize(IServiceProvider provider) => _provider = provider;

    public static T GetRequiredService<T>() where T : notnull
    {
        if (_provider is null)
        {
            throw new InvalidOperationException("Services not initialized.");
        }

        return _provider.GetRequiredService<T>();
    }
}

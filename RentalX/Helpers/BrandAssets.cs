using System.IO;
using System.Windows;

namespace RentalX.Helpers;

public static class BrandAssets
{
    private static readonly object Gate = new();
    private static byte[]? _logoBytes;
    private static byte[]? _logoLightBytes;
    private static byte[]? _markBytes;
    private static byte[]? _markLightBytes;

    public static byte[]? LogoPng => Load(ref _logoBytes, "pack://application:,,,/Assets/logo.png");
    public static byte[]? LogoLightPng => Load(ref _logoLightBytes, "pack://application:,,,/Assets/logo-light.png");
    public static byte[]? MarkPng => Load(ref _markBytes, "pack://application:,,,/Assets/favicon.png");
    public static byte[]? MarkLightPng => Load(ref _markLightBytes, "pack://application:,,,/Assets/favicon-light.png");

    private static byte[]? Load(ref byte[]? cache, string packUri)
    {
        if (cache is not null)
        {
            return cache;
        }

        lock (Gate)
        {
            if (cache is not null)
            {
                return cache;
            }

            try
            {
                var info = Application.GetResourceStream(new Uri(packUri, UriKind.Absolute));
                if (info?.Stream is null)
                {
                    return null;
                }

                using var ms = new MemoryStream();
                info.Stream.CopyTo(ms);
                cache = ms.ToArray();
                return cache;
            }
            catch
            {
                return null;
            }
        }
    }
}

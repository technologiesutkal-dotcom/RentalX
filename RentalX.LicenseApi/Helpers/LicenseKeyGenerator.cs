using System.Security.Cryptography;
using System.Text;

namespace RentalX.LicenseApi.Helpers;

public static class LicenseKeyGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        Span<char> chars = stackalloc char[19];
        var bytes = RandomNumberGenerator.GetBytes(16);
        var idx = 0;
        for (var group = 0; group < 4; group++)
        {
            if (group > 0)
            {
                chars[idx++] = '-';
            }

            for (var i = 0; i < 4; i++)
            {
                chars[idx++] = Alphabet[bytes[group * 4 + i] % Alphabet.Length];
            }
        }

        return new string(chars);
    }

    public static string Normalize(string key) =>
        key.Trim().ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
}

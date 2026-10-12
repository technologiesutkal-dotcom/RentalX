using System.Text;

namespace RentalX.LicenseApi.Helpers;

/// <summary>
/// Neon/Railway often provide URI form (postgresql://...). Npgsql needs keyword form (Host=...).
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "PostgreSQL connection string is empty. Set Database__ConnectionString " +
                "(keyword form) or a postgresql:// URI.");
        }

        var value = raw.Trim().Trim('"');
        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return FromUri(value);
        }

        // Already keyword-style (Host=...;Database=...)
        if (!value.Contains("SSL Mode=", StringComparison.OrdinalIgnoreCase) &&
            !value.Contains("Ssl Mode=", StringComparison.OrdinalIgnoreCase))
        {
            value += ";SSL Mode=Require;Trust Server Certificate=true";
        }

        return value;
    }

    private static string FromUri(string uriText)
    {
        // Ensure Uri can parse query even if Neon truncates "?sslmode"
        if (uriText.EndsWith("?sslmode", StringComparison.OrdinalIgnoreCase))
        {
            uriText += "=require";
        }

        if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Invalid PostgreSQL URI connection string.");
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
        var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
        var database = uri.AbsolutePath.Trim('/').Split('?', 2)[0];
        if (string.IsNullOrWhiteSpace(database))
        {
            database = "neondb";
        }

        var sb = new StringBuilder();
        sb.Append("Host=").Append(uri.Host).Append(';');
        if (uri.Port > 0 && !uri.IsDefaultPort)
        {
            sb.Append("Port=").Append(uri.Port).Append(';');
        }

        sb.Append("Database=").Append(database).Append(';');
        sb.Append("Username=").Append(username).Append(';');
        sb.Append("Password=").Append(password).Append(';');
        sb.Append("SSL Mode=Require;Trust Server Certificate=true;Pooling=true");
        return sb.ToString();
    }
}

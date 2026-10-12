namespace RentalX.Helpers;

public static class SerialFormatter
{
    public static string Normalize(string? serial)
    {
        if (string.IsNullOrWhiteSpace(serial))
        {
            return string.Empty;
        }

        var n = new string(serial.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

        while (n.StartsWith("RX", StringComparison.Ordinal) &&
               n.Length > 10 &&
               n[2..].All(IsHex))
        {
            n = n[2..];
        }

        return n;
    }

    public static string Format(string? serial)
    {
        var n = Normalize(serial);
        if (n.Length < 8)
        {
            return n;
        }

        var parts = new List<string>();
        for (var i = 0; i < n.Length; i += 4)
        {
            parts.Add(n.Substring(i, Math.Min(4, n.Length - i)));
        }

        return "RX-" + string.Join("-", parts);
    }

    private static bool IsHex(char c) =>
        c is >= '0' and <= '9' or >= 'A' and <= 'F';
}

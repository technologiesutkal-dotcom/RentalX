namespace RentalX.LicenseApi.Helpers;

/// <summary>
/// Neon/Npgsql requires DateTime Kind=UTC for timestamp with time zone columns.
/// WPF DatePickers and .Date truncations often produce Local/Unspecified.
/// </summary>
public static class DateTimeUtc
{
    public static DateTime AsUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    /// <summary>Date-only value stored as UTC midnight.</summary>
    public static DateTime AsUtcDate(DateTime value) =>
        DateTime.SpecifyKind(AsUtc(value).Date, DateTimeKind.Utc);

    public static DateTime? AsUtcDate(DateTime? value) =>
        value.HasValue ? AsUtcDate(value.Value) : null;

    public static DateTime UtcToday =>
        DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
}

using System.Globalization;
using System.Text.RegularExpressions;

namespace RentalX.Helpers;

public static partial class ValidationHelper
{
    private static readonly Regex EmailRegex = MyEmailRegex();
    private static readonly Regex PhoneRegex = MyPhoneRegex();
    private static readonly Regex NameRegex = MyNameRegex();
    private static readonly Regex ItemNameRegex = MyItemNameRegex();

    public static bool IsRequired(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    public static bool IsValidName(string? value) =>
        IsRequired(value) &&
        value!.Trim().Length >= 3 &&
        value.Trim().Length <= 120 &&
        NameRegex.IsMatch(value.Trim());

    public static bool IsValidItemName(string? value) =>
        IsRequired(value) &&
        value!.Trim().Length >= 2 &&
        value.Trim().Length <= 120 &&
        ItemNameRegex.IsMatch(value.Trim());

    public static bool IsValidEmail(string? value) =>
        IsRequired(value) &&
        value!.Trim().Length <= 120 &&
        EmailRegex.IsMatch(value.Trim());

    public static bool IsValidOptionalEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) || IsValidEmail(value);

    public static bool IsValidPhone(string? value)
    {
        if (!IsRequired(value))
        {
            return false;
        }

        var trimmed = value!.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length is >= 10 and <= 15 && PhoneRegex.IsMatch(trimmed);
    }

    public static bool IsValidOptionalPhone(string? value) =>
        string.IsNullOrWhiteSpace(value) || IsValidPhone(value);

    public static bool IsValidSelection(string? value, IEnumerable<string> allowed) =>
        IsRequired(value) && allowed.Contains(value!);

    public static bool IsValidPositiveInt(string? value, out int number)
    {
        number = 0;
        if (!IsRequired(value))
        {
            return false;
        }

        return int.TryParse(value!.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
               && number > 0
               && number <= 100000;
    }

    public static bool IsValidPositiveDecimal(string? value, out decimal amount)
    {
        amount = 0;
        if (!IsRequired(value))
        {
            return false;
        }

        return decimal.TryParse(value!.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
               && amount > 0
               && amount <= 100000000;
    }

    public static bool IsValidDateRange(DateTime? from, DateTime? to) =>
        from.HasValue && to.HasValue && from.Value.Date <= to.Value.Date;

    [GeneratedRegex(@"^[A-Za-z][A-Za-z\s.''-]{2,119}$")]
    private static partial Regex MyNameRegex();

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9\s.&()\-]{1,119}$")]
    private static partial Regex MyItemNameRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex MyEmailRegex();

    [GeneratedRegex(@"^[0-9+\-\s()]{10,20}$")]
    private static partial Regex MyPhoneRegex();
}

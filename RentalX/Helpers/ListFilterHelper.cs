using System.Collections.ObjectModel;

namespace RentalX.Helpers;

public static class ListFilterHelper
{
    public static bool Matches(string? searchText, params string?[] fields)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var term = searchText.Trim();
        return fields.Any(field =>
            !string.IsNullOrWhiteSpace(field) &&
            field.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}

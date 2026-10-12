using RentalX.Data.Repositories;
using RentalX.Models;

namespace RentalX.Services;

public class SearchService : ISearchService
{
    private readonly IBookingRepository _bookings;
    private readonly ICustomerRepository _customers;
    private readonly IInventoryRepository _inventory;

    public SearchService(
        IBookingRepository bookings,
        ICustomerRepository customers,
        IInventoryRepository inventory)
    {
        _bookings = bookings;
        _customers = customers;
        _inventory = inventory;
    }

    public async Task<List<SearchResultItem>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        var q = query.Trim();
        var results = new List<SearchResultItem>();

        foreach (var booking in await _bookings.GetAllAsync())
        {
            if (Contains(booking.BookingNumber, q) ||
                Contains(booking.CustomerName, q) ||
                Contains(booking.ItemsSummary, q))
            {
                results.Add(new SearchResultItem
                {
                    Category = "Booking",
                    Title = booking.BookingNumber,
                    Subtitle = $"{booking.CustomerName} · {booking.AmountText}",
                    PageKey = "Bookings"
                });
            }
        }

        foreach (var customer in await _customers.GetAllAsync())
        {
            if (Contains(customer.Name, q) || Contains(customer.Phone, q) || Contains(customer.Email, q))
            {
                results.Add(new SearchResultItem
                {
                    Category = "Customer",
                    Title = customer.Name,
                    Subtitle = $"{customer.Phone} · {customer.Email}",
                    PageKey = "Customers"
                });
            }
        }

        foreach (var item in await _inventory.GetAllAsync())
        {
            if (Contains(item.Name, q) || Contains(item.Category, q))
            {
                results.Add(new SearchResultItem
                {
                    Category = "Inventory",
                    Title = item.Name,
                    Subtitle = $"Available {item.AvailableQuantity} · ₹{item.DailyRate:N0}/day",
                    PageKey = "Inventory"
                });
            }
        }

        return results.Take(20).ToList();
    }

    private static bool Contains(string? source, string query) =>
        !string.IsNullOrWhiteSpace(source) &&
        source.Contains(query, StringComparison.OrdinalIgnoreCase);
}

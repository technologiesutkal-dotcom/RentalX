using RentalX.Data.Repositories;
using RentalX.Models;
using RentalX.Models.Enums;

namespace RentalX.Services;

public class NotificationService : INotificationService
{
    private readonly IBookingRepository _bookings;
    private readonly IInventoryRepository _inventory;
    private readonly IPaymentRepository _payments;

    public NotificationService(
        IBookingRepository bookings,
        IInventoryRepository inventory,
        IPaymentRepository payments)
    {
        _bookings = bookings;
        _inventory = inventory;
        _payments = payments;
    }

    public async Task<List<AppNotification>> GetNotificationsAsync()
    {
        var list = new List<AppNotification>();

        var overdue = await _bookings.GetByStatusAsync(nameof(BookingStatus.Overdue));
        foreach (var booking in overdue.Take(5))
        {
            list.Add(new AppNotification
            {
                Title = "Overdue rental",
                Message = $"{booking.BookingNumber} · {booking.CustomerName} · {booking.ItemsSummary}",
                Type = "Danger",
                CreatedAt = DateTime.Now
            });
        }

        var lowStock = await _inventory.GetLowStockAsync(5);
        foreach (var item in lowStock.Take(5))
        {
            list.Add(new AppNotification
            {
                Title = "Low stock",
                Message = $"{item.Name} has only {item.AvailableQuantity} available",
                Type = "Warning",
                CreatedAt = DateTime.Now
            });
        }

        var pending = await _payments.GetPendingAsync();
        if (pending > 0)
        {
            list.Add(new AppNotification
            {
                Title = "Pending collections",
                Message = $"₹{pending:N0} is still pending from customers",
                Type = "Info",
                CreatedAt = DateTime.Now
            });
        }

        if (list.Count == 0)
        {
            list.Add(new AppNotification
            {
                Title = "All clear",
                Message = "No urgent alerts right now",
                Type = "Success",
                CreatedAt = DateTime.Now
            });
        }

        return list;
    }
}

using RentalX.Models;

namespace RentalX.Services;

public interface INotificationService
{
    Task<List<AppNotification>> GetNotificationsAsync();
}

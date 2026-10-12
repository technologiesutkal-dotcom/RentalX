using RentalX.Models;

namespace RentalX.Data.Repositories;

public interface IDashboardRepository
{
    Task<DashboardSummary> GetSummaryAsync();
    Task<List<UpcomingReturnItem>> GetUpcomingReturnsAsync();
    Task<List<LowStockItem>> GetLowStockAsync();
}

using RentalX.Models;

namespace RentalX.Data.Repositories;

public interface IInventoryRepository
{
    Task<List<InventoryItem>> GetAllAsync();
    Task<List<InventoryItem>> GetLowStockAsync(int threshold = 15);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task AddAsync(InventoryItem item);
    Task AddStockAsync(int itemId, int quantity);
    Task UpdateDailyRateAsync(int itemId, decimal dailyRate);
    Task UpdateItemAsync(InventoryItem item);
    Task UpdateStatusAsync(int itemId, string status);
    Task DeleteAsync(int itemId);
    Task<InventoryItem?> GetByIdAsync(int id);
    Task ReserveAsync(int itemId, int quantity);
    Task ReleaseAsync(int itemId, int quantity);
}

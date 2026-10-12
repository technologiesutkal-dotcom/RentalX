using Microsoft.EntityFrameworkCore;
using RentalX.Models;

namespace RentalX.Data.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _db;

    public InventoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<InventoryItem>> GetAllAsync() =>
        _db.InventoryItems.AsNoTracking().OrderBy(x => x.Name).ToListAsync();

    public async Task<List<InventoryItem>> GetLowStockAsync(int threshold = 15)
    {
        var items = await GetAllAsync();
        return items
            .Where(x => x.AvailableQuantity <= threshold)
            .OrderBy(x => x.AvailableQuantity)
            .ToList();
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        var normalized = name.Trim();
        var items = await _db.InventoryItems.AsNoTracking().ToListAsync();
        return items.Any(x =>
            (!excludeId.HasValue || x.Id != excludeId.Value) &&
            string.Equals(x.Name, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(InventoryItem item)
    {
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();
    }

    public async Task AddStockAsync(int itemId, int quantity)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == itemId);
        if (item is null || quantity <= 0)
        {
            return;
        }

        item.TotalQuantity += quantity;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateDailyRateAsync(int itemId, decimal dailyRate)
    {
        if (dailyRate <= 0)
        {
            throw new InvalidOperationException("Daily rate must be greater than zero.");
        }

        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Inventory item not found.");

        item.DailyRate = dailyRate;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(InventoryItem updated)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == updated.Id)
            ?? throw new InvalidOperationException("Inventory item not found.");

        if (string.IsNullOrWhiteSpace(updated.Name) || updated.Name.Trim().Length < 2)
        {
            throw new InvalidOperationException("Enter a valid item name.");
        }

        if (updated.DailyRate <= 0)
        {
            throw new InvalidOperationException("Daily rate must be greater than zero.");
        }

        if (updated.TotalQuantity < item.RentedQuantity)
        {
            throw new InvalidOperationException($"Total quantity cannot be less than rented ({item.RentedQuantity}).");
        }

        var duplicate = await NameExistsAsync(updated.Name, updated.Id);
        if (duplicate)
        {
            throw new InvalidOperationException("Another item already uses this name.");
        }

        item.Name = updated.Name.Trim();
        item.Category = updated.Category;
        item.DailyRate = updated.DailyRate;
        item.TotalQuantity = updated.TotalQuantity;
        item.Status = string.IsNullOrWhiteSpace(updated.Status) ? item.Status : updated.Status;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int itemId)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Inventory item not found.");

        if (item.RentedQuantity > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete {item.Name} — {item.RentedQuantity} units are still out with customers. Receive them first.");
        }

        var activeStatuses = new[]
        {
            Models.Enums.BookingStatus.Confirmed,
            Models.Enums.BookingStatus.Dispatched,
            Models.Enums.BookingStatus.Overdue,
            Models.Enums.BookingStatus.Pending
        };

        var activeBookings = await _db.Bookings.CountAsync(x =>
            x.InventoryItemId == itemId && activeStatuses.Contains(x.Status));

        var activeLineBookings = await _db.BookingLines.CountAsync(l =>
            l.InventoryItemId == itemId &&
            l.Booking != null &&
            activeStatuses.Contains(l.Booking.Status));

        if (activeBookings > 0 || activeLineBookings > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete {item.Name} — active booking(s) still use this item.");
        }

        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int itemId, string status)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Inventory item not found.");

        item.Status = string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim();
        await _db.SaveChangesAsync();
    }

    public Task<InventoryItem?> GetByIdAsync(int id) =>
        _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id);

    public async Task ReserveAsync(int itemId, int quantity)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Inventory item not found.");

        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        if (item.AvailableQuantity < quantity)
        {
            throw new InvalidOperationException($"Only {item.AvailableQuantity} units available for {item.Name}.");
        }

        item.RentedQuantity += quantity;
        await _db.SaveChangesAsync();
    }

    public async Task ReleaseAsync(int itemId, int quantity)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == itemId);
        if (item is null || quantity <= 0)
        {
            return;
        }

        item.RentedQuantity = Math.Max(0, item.RentedQuantity - quantity);
        await _db.SaveChangesAsync();
    }
}

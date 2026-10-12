using RentalX.Models;

namespace RentalX.Services;

public interface IRentalOperationsService
{
    Task<Booking> CreateBookingAsync(
        int customerId,
        int inventoryItemId,
        int quantity,
        int rentalDays,
        DateTime bookingDate,
        DateTime returnDate,
        decimal advanceAmount);

    /// <summary>One customer booking with multiple inventory items.</summary>
    Task<Booking> CreateBookingAsync(
        int customerId,
        IReadOnlyList<(int InventoryItemId, int Quantity)> lines,
        int rentalDays,
        DateTime bookingDate,
        DateTime returnDate,
        decimal advanceAmount);

    Task DispatchBookingAsync(int bookingId);

    /// <summary>Preview late/early settlement before receiving.</summary>
    Task<ReturnSettlement> PreviewReturnSettlementAsync(
        int bookingId,
        DateTime actualReturnDate,
        decimal manualAdjustment,
        bool applyEarlyCredit);

    /// <summary>
    /// Receive items, apply settlement (late charge / early credit / manual),
    /// optionally collect balance or record refund, generate invoice.
    /// </summary>
    Task<Booking> ReceiveBookingAsync(
        int bookingId,
        DateTime actualReturnDate,
        decimal manualAdjustment,
        bool applyEarlyCredit,
        bool collectSettlement,
        bool generateInvoice,
        string? settlementNote);

    Task CollectBalanceAsync(int bookingId, decimal? amount = null);
    Task ConfirmBookingAsync(int bookingId);
    Task<string> GenerateInvoicePdfAsync(int bookingId);
    Task MarkOverdueBookingsAsync();
}

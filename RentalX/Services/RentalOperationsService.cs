using RentalX.Data.Repositories;
using RentalX.Models;
using RentalX.Models.Enums;

namespace RentalX.Services;

public class RentalOperationsService : IRentalOperationsService
{
    private readonly IBookingRepository _bookings;
    private readonly IInventoryRepository _inventory;
    private readonly IPaymentRepository _payments;
    private readonly ISettingsRepository _settings;
    private readonly IInvoicePdfService _invoicePdf;

    public RentalOperationsService(
        IBookingRepository bookings,
        IInventoryRepository inventory,
        IPaymentRepository payments,
        ISettingsRepository settings,
        IInvoicePdfService invoicePdf)
    {
        _bookings = bookings;
        _inventory = inventory;
        _payments = payments;
        _settings = settings;
        _invoicePdf = invoicePdf;
    }

    public Task<Booking> CreateBookingAsync(
        int customerId,
        int inventoryItemId,
        int quantity,
        int rentalDays,
        DateTime bookingDate,
        DateTime returnDate,
        decimal advanceAmount) =>
        CreateBookingAsync(
            customerId,
            [(inventoryItemId, quantity)],
            rentalDays,
            bookingDate,
            returnDate,
            advanceAmount);

    public async Task<Booking> CreateBookingAsync(
        int customerId,
        IReadOnlyList<(int InventoryItemId, int Quantity)> lines,
        int rentalDays,
        DateTime bookingDate,
        DateTime returnDate,
        decimal advanceAmount)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new InvalidOperationException("Add at least one item to the booking.");
        }

        if (rentalDays <= 0)
        {
            throw new InvalidOperationException("Rental days must be greater than zero.");
        }

        if (returnDate.Date < bookingDate.Date)
        {
            throw new InvalidOperationException("Return date cannot be before booking date.");
        }

        // Merge duplicate item ids (same item added twice).
        var merged = lines
            .GroupBy(l => l.InventoryItemId)
            .Select(g => (InventoryItemId: g.Key, Quantity: g.Sum(x => x.Quantity)))
            .ToList();

        var bookingLines = new List<BookingLine>();
        decimal amount = 0;

        foreach (var (inventoryItemId, quantity) in merged)
        {
            if (quantity <= 0)
            {
                throw new InvalidOperationException("Each item quantity must be greater than zero.");
            }

            var item = await _inventory.GetByIdAsync(inventoryItemId)
                ?? throw new InvalidOperationException("Please select a valid inventory item.");

            if (!string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{item.Name} is marked {item.Status} and cannot be booked.");
            }

            if (item.AvailableQuantity < quantity)
            {
                throw new InvalidOperationException($"Only {item.AvailableQuantity} units available for {item.Name}.");
            }

            if (item.DailyRate <= 0)
            {
                throw new InvalidOperationException($"Set a daily rate for {item.Name} in Inventory Stock first.");
            }

            var lineAmount = item.DailyRate * quantity * rentalDays;
            amount += lineAmount;
            bookingLines.Add(new BookingLine
            {
                InventoryItemId = item.Id,
                ItemName = item.Name,
                Quantity = quantity,
                DailyRate = item.DailyRate,
                LineAmount = lineAmount
            });
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Booking amount must be greater than zero.");
        }

        if (advanceAmount < 0)
        {
            throw new InvalidOperationException("Advance cannot be negative.");
        }

        if (advanceAmount > amount)
        {
            throw new InvalidOperationException($"Advance cannot exceed total rental ₹{amount:N0}.");
        }

        var first = bookingLines[0];
        var summary = string.Join(", ", bookingLines.Select(l => $"{l.Quantity}×{l.ItemName}"));
        if (summary.Length > 480)
        {
            summary = summary[..477] + "...";
        }

        var booking = new Booking
        {
            BookingNumber = await _bookings.GetNextBookingNumberAsync(),
            CustomerId = customerId,
            InventoryItemId = first.InventoryItemId,
            ItemsSummary = summary,
            Quantity = bookingLines.Sum(l => l.Quantity),
            RentalDays = rentalDays,
            DailyRate = first.DailyRate,
            BookingDate = bookingDate.Date,
            ReturnDate = returnDate.Date,
            ExpectedReturnDate = returnDate.Date,
            OriginalAmount = amount,
            Amount = amount,
            PaidAmount = 0,
            Status = BookingStatus.Confirmed,
            Lines = bookingLines
        };

        await _bookings.AddAsync(booking);

        if (advanceAmount > 0)
        {
            await AddPaymentAndUpdatePaidAsync(
                booking.Id,
                advanceAmount,
                isAdvance: true,
                description: advanceAmount >= amount
                    ? $"Full payment for {booking.BookingNumber}"
                    : $"Advance for {booking.BookingNumber}",
                allowExceedAmount: false);
        }

        return (await _bookings.GetByIdAsync(booking.Id))!;
    }

    public async Task ConfirmBookingAsync(int bookingId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only pending bookings can be confirmed.");
        }

        await _bookings.UpdateStatusAsync(bookingId, BookingStatus.Confirmed);
    }

    public async Task DispatchBookingAsync(int bookingId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only booked (confirmed) orders can be dispatched.");
        }

        var lines = GetDispatchLines(booking);
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Booking has no inventory item/quantity to dispatch.");
        }

        foreach (var (itemId, qty) in lines)
        {
            await _inventory.ReserveAsync(itemId, qty);
        }

        booking.Status = BookingStatus.Dispatched;
        await _bookings.UpdateAsync(booking);
    }

    public async Task MarkOverdueBookingsAsync()
    {
        var outBookings = await _bookings.GetByStatusAsync("Dispatched");
        foreach (var booking in outBookings.Where(b => b.IsOverdueToday))
        {
            await _bookings.UpdateStatusAsync(booking.Id, BookingStatus.Overdue);
        }
    }

    public async Task<ReturnSettlement> PreviewReturnSettlementAsync(
        int bookingId,
        DateTime actualReturnDate,
        decimal manualAdjustment,
        bool applyEarlyCredit)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status is not (BookingStatus.Dispatched or BookingStatus.Overdue))
        {
            throw new InvalidOperationException("Only items currently with the customer can be settled.");
        }

        return BuildSettlement(booking, actualReturnDate.Date, manualAdjustment, applyEarlyCredit);
    }

    public async Task<Booking> ReceiveBookingAsync(
        int bookingId,
        DateTime actualReturnDate,
        decimal manualAdjustment,
        bool applyEarlyCredit,
        bool collectSettlement,
        bool generateInvoice,
        string? settlementNote)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status is not (BookingStatus.Dispatched or BookingStatus.Overdue))
        {
            throw new InvalidOperationException("Only items currently with the customer can be received back.");
        }

        if (actualReturnDate.Date < booking.BookingDate.Date)
        {
            throw new InvalidOperationException("Actual return date cannot be before booking date.");
        }

        var settlement = BuildSettlement(booking, actualReturnDate.Date, manualAdjustment, applyEarlyCredit);

        foreach (var (itemId, qty) in GetDispatchLines(booking))
        {
            await _inventory.ReleaseAsync(itemId, qty);
        }

        if (booking.OriginalAmount <= 0)
        {
            booking.OriginalAmount = booking.Amount;
        }

        booking.ExpectedReturnDate ??= booking.ReturnDate;
        booking.ActualReturnDate = settlement.ActualReturnDate;
        booking.LateDays = settlement.LateDays;
        booking.EarlyDays = settlement.EarlyDays;
        booking.AdjustmentAmount = settlement.AutoAdjustment + settlement.ManualAdjustment;
        booking.Amount = settlement.FinalAmount;
        booking.RentalDays = Math.Max(1, settlement.ActualDays);
        booking.Status = BookingStatus.Returned;
        booking.SettlementNote = string.IsNullOrWhiteSpace(settlementNote)
            ? settlement.Summary
            : $"{settlement.Summary} | {settlementNote.Trim()}";

        if (generateInvoice && string.IsNullOrWhiteSpace(booking.InvoiceNumber))
        {
            booking.InvoiceNumber = await _bookings.GetNextInvoiceNumberAsync();
            booking.IsInvoiced = true;
        }

        await _bookings.UpdateAsync(booking);

        if (collectSettlement)
        {
            if (settlement.AmountToCollect > 0)
            {
                await AddPaymentAndUpdatePaidAsync(
                    booking.Id,
                    settlement.AmountToCollect,
                    isAdvance: false,
                    description: settlement.LateDays > 0
                        ? $"Settlement (incl. late) for {booking.BookingNumber}"
                        : $"Settlement balance for {booking.BookingNumber}",
                    allowExceedAmount: false);
            }
            else if (settlement.RefundDue > 0)
            {
                await RecordRefundAsync(booking.Id, settlement.RefundDue, booking.BookingNumber);
            }
        }

        return (await _bookings.GetByIdAsync(booking.Id))!;
    }

    public async Task CollectBalanceAsync(int bookingId, decimal? amount = null)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.BalanceDue <= 0)
        {
            throw new InvalidOperationException($"{booking.BookingNumber} is already fully paid.");
        }

        var toCollect = amount ?? booking.BalanceDue;
        if (toCollect <= 0)
        {
            throw new InvalidOperationException("Enter an amount greater than zero.");
        }

        if (toCollect > booking.BalanceDue)
        {
            throw new InvalidOperationException($"Only ₹{booking.BalanceDue:N0} is remaining.");
        }

        var isFullBalance = toCollect >= booking.BalanceDue;
        await AddPaymentAndUpdatePaidAsync(
            booking.Id,
            toCollect,
            isAdvance: false,
            description: isFullBalance
                ? $"Balance payment for {booking.BookingNumber}"
                : $"Partial payment for {booking.BookingNumber}",
            allowExceedAmount: false);
    }

    private static ReturnSettlement BuildSettlement(
        Booking booking,
        DateTime actualReturnDate,
        decimal manualAdjustment,
        bool applyEarlyCredit)
    {
        var expected = (booking.ExpectedReturnDate ?? booking.ReturnDate ?? booking.BookingDate.AddDays(Math.Max(1, booking.RentalDays))).Date;
        if (actualReturnDate < booking.BookingDate.Date)
        {
            actualReturnDate = booking.BookingDate.Date;
        }

        var plannedDays = Math.Max(1, (int)(expected - booking.BookingDate.Date).TotalDays);
        if (booking.RentalDays > 0)
        {
            plannedDays = booking.RentalDays;
        }

        var actualDays = Math.Max(1, (int)(actualReturnDate - booking.BookingDate.Date).TotalDays);
        var dayDiff = (actualReturnDate - expected).Days;

        var lateDays = Math.Max(0, dayDiff);
        var earlyDays = Math.Max(0, -dayDiff);
        var perDay = booking.PerDayCharge;
        var original = booking.OriginalAmount > 0 ? booking.OriginalAmount : booking.Amount;
        if (original <= 0)
        {
            original = perDay * plannedDays;
        }

        decimal autoAdjustment = 0;
        string type;
        string summary;

        if (lateDays > 0)
        {
            type = "Late";
            autoAdjustment = lateDays * perDay;
            summary = $"Late {lateDays} day(s) × ₹{perDay:N0}/day (all items) = +₹{autoAdjustment:N0}";
        }
        else if (earlyDays > 0)
        {
            type = "Early";
            if (applyEarlyCredit)
            {
                autoAdjustment = -(earlyDays * perDay);
                // Never reduce below 1 day charge for all items.
                var minAmount = perDay;
                if (original + autoAdjustment < minAmount)
                {
                    autoAdjustment = minAmount - original;
                }

                summary = $"Early {earlyDays} day(s) credit × ₹{perDay:N0}/day (all items) = -₹{Math.Abs(autoAdjustment):N0}";
            }
            else
            {
                autoAdjustment = 0;
                summary = $"Early {earlyDays} day(s) — no credit (kept original amount)";
            }
        }
        else
        {
            type = "OnTime";
            summary = "Returned on planned date — no auto adjustment";
        }

        if (manualAdjustment != 0)
        {
            summary += manualAdjustment > 0
                ? $" | Manual +₹{manualAdjustment:N0}"
                : $" | Manual -₹{Math.Abs(manualAdjustment):N0}";
        }

        var finalAmount = Math.Max(0, original + autoAdjustment + manualAdjustment);
        var paid = booking.PaidAmount;
        var toCollect = Math.Max(0, finalAmount - paid);
        var refundDue = Math.Max(0, paid - finalAmount);

        return new ReturnSettlement
        {
            BookingId = booking.Id,
            BookingNumber = booking.BookingNumber,
            ExpectedReturnDate = expected,
            ActualReturnDate = actualReturnDate,
            PlannedDays = plannedDays,
            ActualDays = actualDays,
            LateDays = lateDays,
            EarlyDays = earlyDays,
            SettlementType = type,
            DailyRate = perDay,
            Quantity = booking.Quantity,
            OriginalAmount = original,
            AutoAdjustment = autoAdjustment,
            ManualAdjustment = manualAdjustment,
            FinalAmount = finalAmount,
            PaidAmount = paid,
            AmountToCollect = toCollect,
            RefundDue = refundDue,
            ApplyEarlyCredit = applyEarlyCredit,
            Summary = summary
        };
    }

    private static List<(int ItemId, int Quantity)> GetDispatchLines(Booking booking)
    {
        if (booking.Lines is { Count: > 0 })
        {
            return booking.Lines
                .Where(l => l.InventoryItemId > 0 && l.Quantity > 0)
                .Select(l => (l.InventoryItemId, l.Quantity))
                .ToList();
        }

        if (booking.InventoryItemId is int id && booking.Quantity > 0)
        {
            return [(id, booking.Quantity)];
        }

        return [];
    }

    private async Task AddPaymentAndUpdatePaidAsync(
        int bookingId,
        decimal amount,
        bool isAdvance,
        string description,
        bool allowExceedAmount)
    {
        var tracked = await _bookings.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        await _payments.AddAsync(new Payment
        {
            BookingId = bookingId,
            Description = description,
            Amount = amount,
            Status = PaymentStatus.Collected,
            PaymentDate = DateTime.Today,
            IsDeposit = isAdvance
        });

        tracked.PaidAmount += amount;
        if (!allowExceedAmount)
        {
            tracked.PaidAmount = Math.Min(tracked.Amount, tracked.PaidAmount);
        }

        await _bookings.UpdateAsync(tracked);
    }

    private async Task RecordRefundAsync(int bookingId, decimal refundAmount, string bookingNumber)
    {
        var tracked = await _bookings.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        await _payments.AddAsync(new Payment
        {
            BookingId = bookingId,
            Description = $"Early-return refund for {bookingNumber}",
            Amount = refundAmount,
            Status = PaymentStatus.Refunded,
            PaymentDate = DateTime.Today,
            IsDeposit = false
        });

        tracked.PaidAmount = Math.Max(0, tracked.PaidAmount - refundAmount);
        await _bookings.UpdateAsync(tracked);
    }

    public async Task<string> GenerateInvoicePdfAsync(int bookingId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        if (string.IsNullOrWhiteSpace(booking.InvoiceNumber))
        {
            booking.InvoiceNumber = await _bookings.GetNextInvoiceNumberAsync();
            booking.IsInvoiced = true;
            await _bookings.UpdateAsync(booking);
            booking = (await _bookings.GetByIdAsync(booking.Id))!;
        }

        var company = await _settings.GetAsync();
        return _invoicePdf.GenerateInvoice(company, booking);
    }
}

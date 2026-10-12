using Microsoft.EntityFrameworkCore;
using RentalX.Models;

namespace RentalX.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingLine> BookingLines => Set<BookingLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<LoginAccount> LoginAccounts => Set<LoginAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginAccount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordSalt).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(120);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.DailyRate).HasPrecision(18, 2);
            entity.Ignore(x => x.AvailableQuantity);
            entity.Ignore(x => x.AvailablePercent);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BookingNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ItemsSummary).HasMaxLength(500);
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.Booking)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Ignore(x => x.PerDayCharge);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.OriginalAmount).HasPrecision(18, 2);
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            entity.Property(x => x.AdjustmentAmount).HasPrecision(18, 2);
            entity.Property(x => x.DailyRate).HasPrecision(18, 2);
            entity.Property(x => x.InvoiceNumber).HasMaxLength(40);
            entity.Property(x => x.SettlementNote).HasMaxLength(300);
            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryItem)
                .WithMany()
                .HasForeignKey(x => x.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Ignore(x => x.CustomerName);
            entity.Ignore(x => x.StatusText);
            entity.Ignore(x => x.AmountText);
            entity.Ignore(x => x.PaidText);
            entity.Ignore(x => x.BalanceText);
            entity.Ignore(x => x.BalanceDue);
            entity.Ignore(x => x.RefundDue);
            entity.Ignore(x => x.IsFullyPaid);
            entity.Ignore(x => x.PaymentStatusText);
            entity.Ignore(x => x.BookingDateText);
            entity.Ignore(x => x.ExpectedReturnText);
            entity.Ignore(x => x.ActualReturnText);
            entity.Ignore(x => x.InvoiceText);
            entity.Ignore(x => x.CalculatedAmount);
            entity.Ignore(x => x.IsOverdueToday);
        });

        modelBuilder.Entity<BookingLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ItemName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.DailyRate).HasPrecision(18, 2);
            entity.Property(x => x.LineAmount).HasPrecision(18, 2);
            entity.HasOne(x => x.InventoryItem)
                .WithMany()
                .HasForeignKey(x => x.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(x => x.LineText);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(200);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasOne(x => x.Booking)
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Ignore(x => x.AmountText);
            entity.Ignore(x => x.StatusText);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(60);
            entity.Property(x => x.Email).HasMaxLength(120);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Department).HasMaxLength(80);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(300);
            entity.Ignore(x => x.AmountText);
            entity.Ignore(x => x.ExpenseDateText);
        });

        modelBuilder.Entity<CompanySettings>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BusinessType).HasMaxLength(80);
            entity.Property(x => x.CompanyName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.OwnerName).HasMaxLength(120);
            entity.Property(x => x.Address).HasMaxLength(250);
            entity.Property(x => x.City).HasMaxLength(80);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(120);
            entity.Property(x => x.GstNumber).HasMaxLength(60);
            entity.Property(x => x.Currency).HasMaxLength(40);
            entity.Property(x => x.ReportFooter).HasMaxLength(250);
        });
    }
}

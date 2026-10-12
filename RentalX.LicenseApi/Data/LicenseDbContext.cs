using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Models;

namespace RentalX.LicenseApi.Data;

public class LicenseDbContext(DbContextOptions<LicenseDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();
    public DbSet<AdminSerial> AdminSerials => Set<AdminSerial>();
    public DbSet<ClientUser> ClientUsers => Set<ClientUser>();
    public DbSet<ClientSession> ClientSessions => Set<ClientSession>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Activation> Activations => Set<Activation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SerialRegistration> SerialRegistrations => Set<SerialRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(64);
            e.Property(x => x.DisplayName).HasMaxLength(80);
        });

        modelBuilder.Entity<AdminSession>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(128);
            e.HasOne(x => x.AdminUser).WithMany().HasForeignKey(x => x.AdminUserId);
        });

        modelBuilder.Entity<AdminSerial>(e =>
        {
            e.HasIndex(x => x.SerialNumber).IsUnique();
            e.Property(x => x.SerialNumber).HasMaxLength(128).IsRequired();
            e.Property(x => x.MachineName).HasMaxLength(120);
            e.Property(x => x.Notes).HasMaxLength(250);
        });

        modelBuilder.Entity<Client>(e =>
        {
            e.HasIndex(x => x.Email);
            e.Property(x => x.CompanyName).HasMaxLength(120);
            e.Property(x => x.ContactName).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<ClientUser>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(64);
            e.Property(x => x.DisplayName).HasMaxLength(80);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId);
        });

        modelBuilder.Entity<ClientSession>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(128);
            e.HasOne(x => x.ClientUser).WithMany().HasForeignKey(x => x.ClientUserId);
        });

        modelBuilder.Entity<License>(e =>
        {
            e.HasIndex(x => x.LicenseKey).IsUnique();
            e.Property(x => x.LicenseKey).HasMaxLength(64);
            e.Property(x => x.PlanType).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Client).WithMany(c => c.Licenses).HasForeignKey(x => x.ClientId);
        });

        modelBuilder.Entity<Activation>(e =>
        {
            e.HasIndex(x => new { x.LicenseId, x.MachineId }).IsUnique();
            e.Property(x => x.MachineId).HasMaxLength(128);
            e.HasOne(x => x.License).WithMany(l => l.Activations).HasForeignKey(x => x.LicenseId);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(8);
            e.Property(x => x.Method).HasMaxLength(30);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Client).WithMany(c => c.Payments).HasForeignKey(x => x.ClientId);
            e.HasOne(x => x.License).WithMany(l => l.Payments).HasForeignKey(x => x.LicenseId);
        });

        modelBuilder.Entity<SerialRegistration>(e =>
        {
            e.HasIndex(x => x.SerialNumber).IsUnique();
            e.Property(x => x.SerialNumber).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.CompanyName).HasMaxLength(120);
            e.Property(x => x.ContactName).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.Address).HasMaxLength(250);
            e.Property(x => x.Pincode).HasMaxLength(12);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId);
            e.HasOne(x => x.License).WithMany().HasForeignKey(x => x.LicenseId);
        });
    }
}

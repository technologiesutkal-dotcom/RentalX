using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RentalX.Models;
using RentalX.Models.Enums;

namespace RentalX.Data;

public static class DatabaseInitializer
{
    private static readonly string[] RequiredTables =
    [
        "Customers",
        "InventoryItems",
        "Bookings",
        "BookingLines",
        "Payments",
        "Users",
        "Expenses",
        "CompanySettings",
        "LoginAccounts"
    ];

    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (!AllTablesExist(db))
        {
            // Prefer additive create for LoginAccounts on existing DBs.
            EnsureLoginAccountsTable(db);
            EnsureBookingLinesTable(db);
            if (!AllTablesExist(db))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
        }

        EnsureStaffColumns(db);
        EnsureBookingColumns(db);
        EnsureBookingLinesTable(db);
        EnsureCompanySettings(db);
        EnsureCompanyBusinessTypeColumn(db);
        EnsureCompanyShowGstColumn(db);
        EnsureDefaultLoginAccount(db);

        // Fresh install: empty customers / inventory / bookings for real testing.
        // Demo seed data is intentionally not loaded.
    }

    private static void EnsureCompanyBusinessTypeColumn(AppDbContext db)
    {
        TryAlter(db, """ALTER TABLE "CompanySettings" ADD COLUMN "BusinessType" TEXT NOT NULL DEFAULT 'General rental';""");
    }

    private static void EnsureCompanyShowGstColumn(AppDbContext db)
    {
        TryAlter(db, """ALTER TABLE "CompanySettings" ADD COLUMN "ShowGstOnReports" INTEGER NOT NULL DEFAULT 1;""");
    }

    private static void EnsureLoginAccountsTable(AppDbContext db)
    {
        try
        {
            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "LoginAccounts" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_LoginAccounts" PRIMARY KEY AUTOINCREMENT,
                    "Username" TEXT NOT NULL,
                    "PasswordHash" TEXT NOT NULL,
                    "PasswordSalt" TEXT NOT NULL,
                    "DisplayName" TEXT NOT NULL,
                    "MustChangePassword" INTEGER NOT NULL,
                    "UpdatedAt" TEXT NOT NULL
                );
                """);
        }
        catch
        {
            // Ignore if provider rejects IF NOT EXISTS variant.
        }
    }

    private static void EnsureDefaultLoginAccount(AppDbContext db)
    {
        if (db.LoginAccounts.Any())
        {
            return;
        }

        var (hash, salt) = Helpers.PasswordHasher.HashPassword(Services.AuthService.DefaultPasswordValue);
        db.LoginAccounts.Add(new LoginAccount
        {
            Username = Services.AuthService.DefaultUsernameValue,
            PasswordHash = hash,
            PasswordSalt = salt,
            DisplayName = "Administrator",
            MustChangePassword = true,
            UpdatedAt = DateTime.Now
        });
        db.SaveChanges();
    }

    private static bool AllTablesExist(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            foreach (var table in RequiredTables)
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=$name;";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "$name";
                parameter.Value = table;
                command.Parameters.Add(parameter);

                var result = Convert.ToInt32(command.ExecuteScalar());
                if (result == 0)
                {
                    return false;
                }
            }

            return true;
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    private static void EnsureStaffColumns(AppDbContext db)
    {
        TryAlter(db, "ALTER TABLE Users ADD COLUMN Phone TEXT NOT NULL DEFAULT ''");
        TryAlter(db, "ALTER TABLE Users ADD COLUMN Department TEXT NOT NULL DEFAULT ''");
    }

    private static void EnsureBookingLinesTable(AppDbContext db)
    {
        try
        {
            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "BookingLines" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_BookingLines" PRIMARY KEY AUTOINCREMENT,
                    "BookingId" INTEGER NOT NULL,
                    "InventoryItemId" INTEGER NOT NULL,
                    "ItemName" TEXT NOT NULL,
                    "Quantity" INTEGER NOT NULL,
                    "DailyRate" TEXT NOT NULL,
                    "LineAmount" TEXT NOT NULL,
                    CONSTRAINT "FK_BookingLines_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_BookingLines_InventoryItems_InventoryItemId" FOREIGN KEY ("InventoryItemId") REFERENCES "InventoryItems" ("Id") ON DELETE RESTRICT
                );
                """);
        }
        catch
        {
            // ignore
        }
    }

    private static void EnsureBookingColumns(AppDbContext db)
    {
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN InventoryItemId INTEGER NULL");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN Quantity INTEGER NOT NULL DEFAULT 1");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN RentalDays INTEGER NOT NULL DEFAULT 1");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN DailyRate TEXT NOT NULL DEFAULT '0'");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN InvoiceNumber TEXT NULL");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN IsInvoiced INTEGER NOT NULL DEFAULT 0");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN PaidAmount TEXT NOT NULL DEFAULT '0'");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN ExpectedReturnDate TEXT NULL");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN ActualReturnDate TEXT NULL");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN OriginalAmount TEXT NOT NULL DEFAULT '0'");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN AdjustmentAmount TEXT NOT NULL DEFAULT '0'");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN LateDays INTEGER NOT NULL DEFAULT 0");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN EarlyDays INTEGER NOT NULL DEFAULT 0");
        TryAlter(db, "ALTER TABLE Bookings ADD COLUMN SettlementNote TEXT NULL");

        // Backfill planned return + original amount for older rows.
        try
        {
            db.Database.ExecuteSqlRaw(
                """
                UPDATE Bookings
                SET ExpectedReturnDate = ReturnDate
                WHERE ExpectedReturnDate IS NULL AND ReturnDate IS NOT NULL;
                """);
            db.Database.ExecuteSqlRaw(
                """
                UPDATE Bookings
                SET OriginalAmount = Amount
                WHERE CAST(OriginalAmount AS REAL) = 0 AND CAST(Amount AS REAL) > 0;
                """);
        }
        catch
        {
            // Ignore backfill failures on fresh DBs.
        }
    }

    private static void EnsureCompanySettings(AppDbContext db)
    {
        if (db.CompanySettings.Any(x => x.Id == 1))
        {
            return;
        }

        db.CompanySettings.Add(new CompanySettings());
        db.SaveChanges();
    }

    private static void TryAlter(AppDbContext db, string sql)
    {
        try
        {
            db.Database.ExecuteSqlRaw(sql);
        }
        catch (SqliteException)
        {
        }
        catch (Exception)
        {
        }
    }

    private static void SeedCoreData(AppDbContext db)
    {
        var customers = new List<Customer>
        {
            new() { Name = "ABC Events", Phone = "9876543210", Email = "abc@events.com", Address = "Andheri, Mumbai" },
            new() { Name = "Royal Events", Phone = "9876501234", Email = "royal@events.com", Address = "Bandra, Mumbai" },
            new() { Name = "Mehta Wedding", Phone = "9988776655", Email = "mehta@mail.com", Address = "Pune" },
            new() { Name = "City Lights", Phone = "9123456780", Email = "city@lights.com", Address = "Thane" }
        };

        db.Customers.AddRange(customers);
        db.SaveChanges();

        var inventory = new List<InventoryItem>
        {
            new() { Name = "Plastic Chair", Category = "Seating", TotalQuantity = 100, RentedQuantity = 20, DailyRate = 15 },
            new() { Name = "Tables", Category = "Furniture", TotalQuantity = 50, RentedQuantity = 35, DailyRate = 80 },
            new() { Name = "Generator", Category = "Power", TotalQuantity = 10, RentedQuantity = 7, DailyRate = 1500 },
            new() { Name = "Mixture Machine", Category = "Equipment", TotalQuantity = 8, RentedQuantity = 5, DailyRate = 1200 },
            new() { Name = "Camera Kit", Category = "Media", TotalQuantity = 5, RentedQuantity = 3, DailyRate = 900 }
        };
        db.InventoryItems.AddRange(inventory);
        db.SaveChanges();

        var bookings = new List<Booking>
        {
            new()
            {
                BookingNumber = "BK-1025",
                CustomerId = customers[0].Id,
                InventoryItemId = inventory[2].Id,
                ItemsSummary = "1 x Generator",
                Quantity = 1,
                RentalDays = 1,
                DailyRate = 1500,
                BookingDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(1),
                ExpectedReturnDate = DateTime.Today.AddDays(1),
                Amount = 1500,
                OriginalAmount = 1500,
                PaidAmount = 500,
                Status = BookingStatus.Confirmed
            },
            new()
            {
                BookingNumber = "BK-1024",
                CustomerId = customers[1].Id,
                InventoryItemId = inventory[0].Id,
                ItemsSummary = "200 x Plastic Chair",
                Quantity = 200,
                RentalDays = 2,
                DailyRate = 15,
                BookingDate = DateTime.Today.AddDays(-1),
                ReturnDate = DateTime.Today.AddDays(1),
                ExpectedReturnDate = DateTime.Today.AddDays(1),
                Amount = 6000,
                OriginalAmount = 6000,
                PaidAmount = 2000,
                Status = BookingStatus.Dispatched
            },
            new()
            {
                BookingNumber = "BK-1023",
                CustomerId = customers[2].Id,
                InventoryItemId = inventory[1].Id,
                ItemsSummary = "12 x Tables",
                Quantity = 12,
                RentalDays = 2,
                DailyRate = 80,
                BookingDate = DateTime.Today.AddDays(-2),
                ReturnDate = DateTime.Today.AddDays(3),
                ExpectedReturnDate = DateTime.Today.AddDays(3),
                Amount = 1920,
                OriginalAmount = 1920,
                PaidAmount = 0,
                Status = BookingStatus.Confirmed
            },
            new()
            {
                BookingNumber = "BK-1022",
                CustomerId = customers[3].Id,
                InventoryItemId = inventory[4].Id,
                ItemsSummary = "1 x Camera Kit",
                Quantity = 1,
                RentalDays = 3,
                DailyRate = 900,
                BookingDate = DateTime.Today.AddDays(-4),
                ReturnDate = DateTime.Today.AddDays(-1),
                ExpectedReturnDate = DateTime.Today.AddDays(-1),
                Amount = 2700,
                OriginalAmount = 2700,
                PaidAmount = 1000,
                Status = BookingStatus.Overdue
            }
        };

        db.Bookings.AddRange(bookings);
        db.SaveChanges();

        db.Payments.AddRange(
            new Payment { BookingId = bookings[0].Id, Description = "Advance for BK-1025", Amount = 500, Status = PaymentStatus.Collected, PaymentDate = DateTime.Today, IsDeposit = true },
            new Payment { BookingId = bookings[1].Id, Description = "Advance for BK-1024", Amount = 2000, Status = PaymentStatus.Collected, PaymentDate = DateTime.Today, IsDeposit = true },
            new Payment { BookingId = bookings[3].Id, Description = "Advance for BK-1022", Amount = 1000, Status = PaymentStatus.Collected, PaymentDate = DateTime.Today, IsDeposit = true }
        );

        SeedStaff(db);
        SeedExpenses(db);
        EnsureCompanySettings(db);
        db.SaveChanges();
    }

    private static void SeedExpenses(AppDbContext db)
    {
        db.Expenses.AddRange(
            new Expense { Title = "Fuel for delivery", Category = "Transport", Amount = 2500, ExpenseDate = DateTime.Today.AddDays(-2), Notes = "Van diesel" },
            new Expense { Title = "Generator servicing", Category = "Maintenance", Amount = 4200, ExpenseDate = DateTime.Today.AddDays(-5), Notes = "Oil + filters" },
            new Expense { Title = "Warehouse rent", Category = "Rent", Amount = 18000, ExpenseDate = DateTime.Today.AddDays(-10), Notes = "Monthly rent" }
        );
        db.SaveChanges();
    }

    private static void SeedStaff(AppDbContext db)
    {
        db.Users.AddRange(
            new AppUser
            {
                FullName = "Admin User",
                Role = "Administrator",
                Email = "admin@rentalx.local",
                Phone = "9000000001",
                Department = "Management",
                IsActive = true
            },
            new AppUser
            {
                FullName = "Riya Sharma",
                Role = "Billing Staff",
                Email = "riya@rentalx.local",
                Phone = "9000000002",
                Department = "Accounts",
                IsActive = true
            },
            new AppUser
            {
                FullName = "Amit Patel",
                Role = "Dispatch Staff",
                Email = "amit@rentalx.local",
                Phone = "9000000003",
                Department = "Logistics",
                IsActive = true
            },
            new AppUser
            {
                FullName = "Neha Verma",
                Role = "Warehouse Staff",
                Email = "neha@rentalx.local",
                Phone = "9000000004",
                Department = "Inventory",
                IsActive = true
            }
        );
        db.SaveChanges();
    }
}

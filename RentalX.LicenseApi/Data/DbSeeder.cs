using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Helpers;
using RentalX.LicenseApi.Models;

namespace RentalX.LicenseApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(LicenseDbContext db)
    {
        // PostgreSQL (Neon): create schema from EF model.
        // Sqlite: EnsureCreated + legacy ALTER patches for older local DBs.
        await db.Database.EnsureCreatedAsync();
        await EnsureAdminSerialsTableAsync(db);

        if (db.Database.IsSqlite())
        {
            await EnsureSqliteLegacyPatchesAsync(db);
        }

        // Only Super Admin is seeded. No demo clients — clean testing:
        // Apply → Approve → Login.
        if (!await db.AdminUsers.AnyAsync())
        {
            var (hash, salt) = PasswordHasher.HashPassword("SuperAdmin@123");
            db.AdminUsers.Add(new AdminUser
            {
                Username = "superadmin",
                PasswordHash = hash,
                PasswordSalt = salt,
                DisplayName = "Super Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Adds AdminSerials on existing Neon DBs where EnsureCreated won't alter schema.</summary>
    private static async Task EnsureAdminSerialsTableAsync(LicenseDbContext db)
    {
        if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "AdminSerials" (
                    "Id" SERIAL PRIMARY KEY,
                    "SerialNumber" character varying(128) NOT NULL,
                    "MachineName" character varying(120) NULL,
                    "Notes" character varying(250) NULL,
                    "IsActive" boolean NOT NULL DEFAULT TRUE,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "LastLoginAt" timestamp with time zone NULL
                );
                """);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE UNIQUE INDEX IF NOT EXISTS "IX_AdminSerials_SerialNumber" ON "AdminSerials" ("SerialNumber");""");
            return;
        }

        if (db.Database.IsSqlite())
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "AdminSerials" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_AdminSerials" PRIMARY KEY AUTOINCREMENT,
                    "SerialNumber" TEXT NOT NULL,
                    "MachineName" TEXT NULL,
                    "Notes" TEXT NULL,
                    "IsActive" INTEGER NOT NULL DEFAULT 1,
                    "CreatedAt" TEXT NOT NULL,
                    "LastLoginAt" TEXT NULL
                );
                """);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE UNIQUE INDEX IF NOT EXISTS "IX_AdminSerials_SerialNumber" ON "AdminSerials" ("SerialNumber");""");
        }
    }

    private static async Task EnsureSqliteLegacyPatchesAsync(LicenseDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ClientUsers" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ClientUsers" PRIMARY KEY AUTOINCREMENT,
                "ClientId" INTEGER NOT NULL,
                "Username" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "PasswordSalt" TEXT NOT NULL,
                "PortalPassword" TEXT NOT NULL DEFAULT 'RxShop@2026',
                "DisplayName" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_ClientUsers_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ClientUsers_Username" ON "ClientUsers" ("Username");
            """);

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "ClientUsers" ADD COLUMN "PortalPassword" TEXT NOT NULL DEFAULT 'RxShop@2026';""");
        }
        catch
        {
            // column already exists
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ClientSessions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ClientSessions" PRIMARY KEY AUTOINCREMENT,
                "ClientUserId" INTEGER NOT NULL,
                "Token" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "ExpiresAt" TEXT NOT NULL,
                "RevokedAt" TEXT NULL,
                CONSTRAINT "FK_ClientSessions_ClientUsers_ClientUserId" FOREIGN KEY ("ClientUserId") REFERENCES "ClientUsers" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ClientSessions_Token" ON "ClientSessions" ("Token");
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "SerialRegistrations" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SerialRegistrations" PRIMARY KEY AUTOINCREMENT,
                "SerialNumber" TEXT NOT NULL,
                "MachineName" TEXT NULL,
                "CompanyName" TEXT NOT NULL,
                "ContactName" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "Phone" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "Status" TEXT NOT NULL,
                "ClientId" INTEGER NULL,
                "LicenseId" INTEGER NULL,
                "AppliedAt" TEXT NOT NULL,
                "ReviewedAt" TEXT NULL,
                "ReviewNotes" TEXT NULL,
                CONSTRAINT "FK_SerialRegistrations_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE SET NULL,
                CONSTRAINT "FK_SerialRegistrations_Licenses_LicenseId" FOREIGN KEY ("LicenseId") REFERENCES "Licenses" ("Id") ON DELETE SET NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SerialRegistrations_SerialNumber" ON "SerialRegistrations" ("SerialNumber");
            """);

        try { await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "SerialRegistrations" ADD COLUMN "Address" TEXT NULL;"""); }
        catch { /* exists */ }

        try { await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "SerialRegistrations" ADD COLUMN "Pincode" TEXT NULL;"""); }
        catch { /* exists */ }
    }
}

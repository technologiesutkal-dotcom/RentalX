using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RentalX.LicenseApi.Data;
using RentalX.LicenseApi.Dtos;
using RentalX.LicenseApi.Helpers;
using RentalX.LicenseApi.Services;

// Neon/Npgsql: prefer UTC timestamps (we also normalize dates in services).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

var builder = WebApplication.CreateBuilder(args);

// Neon PostgreSQL (pooled) by default. Set Database:Provider=Sqlite to use local file.
var dbProvider = builder.Configuration["Database:Provider"] ?? "PostgreSQL";
var pgConnectionRaw =
    builder.Configuration["Database:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("RENTALX_LICENSE_DB")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

builder.Services.AddDbContext<LicenseDbContext>(options =>
{
    if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        var dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RentalX",
            "LicenseServer");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "rentalx-license.db");
        options.UseSqlite($"Data Source={dbPath}");
        return;
    }

    if (string.IsNullOrWhiteSpace(pgConnectionRaw))
    {
        throw new InvalidOperationException(
            "PostgreSQL connection string missing. Set Database__ConnectionString on Railway " +
            "(keyword Host=... form or postgresql:// URI), or RENTALX_LICENSE_DB / DATABASE_URL.");
    }

    // Neon/Railway URI → Npgsql keyword string.
    var pgConnection = PostgresConnectionString.Normalize(pgConnectionRaw);

    // Neon pooler endpoint — use for the running API.
    options.UseNpgsql(pgConnection, npgsql =>
    {
        npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(3), null);
        npgsql.CommandTimeout(60);
    });
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminCatalogService, AdminCatalogService>();
builder.Services.AddScoped<ILicenseOpsService, LicenseOpsService>();
builder.Services.AddScoped<ISerialService, SerialService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogInformation("License DB provider: {Provider}", dbProvider);
    await DbSeeder.SeedAsync(db);
    logger.LogInformation("License database ready (Neon PostgreSQL / configured provider).");
}

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// ---------- Public license endpoints (client app) ----------
app.MapPost("/api/license/activate", async (ActivateRequest request, ILicenseOpsService ops) =>
{
    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var result = await ops.ActivateAsync(request);
    return result.IsValid ? Results.Ok(result) : Results.BadRequest(result);
}).WithTags("License");

app.MapPost("/api/license/validate", async (LicenseCheckRequest request, ILicenseOpsService ops) =>
{
    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var result = await ops.ValidateAsync(request);
    return result.IsValid ? Results.Ok(result) : Results.BadRequest(result);
}).WithTags("License");

// ---------- Public serial apply (website / client app) ----------
app.MapPost("/api/serial/apply", async (SerialApplyRequest request, ISerialService serials) =>
{
    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, message) = await serials.ApplyAsync(request);
    return dto is null
        ? Results.BadRequest(new { message })
        : Results.Ok(new { data = dto, message });
}).WithTags("Serial");

app.MapGet("/api/serial/status", async (string serialNumber, ISerialService serials) =>
{
    var (dto, message) = await serials.GetStatusAsync(serialNumber);
    return Results.Ok(new { data = dto, message });
}).WithTags("Serial");

// ---------- Auth ----------
app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService auth) =>
{
    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (result, loginError) = await auth.LoginAsync(request);
    return result is null
        ? Results.BadRequest(new { message = loginError ?? "Login failed." })
        : Results.Ok(result);
}).WithTags("Auth");

app.MapPost("/api/auth/logout", async (HttpRequest http, IAuthService auth) =>
{
    await auth.LogoutAsync(GetBearer(http));
    return Results.Ok(new { message = "Signed out." });
}).WithTags("Auth");

app.MapGet("/api/auth/me", async (HttpRequest http, IAuthService auth) =>
{
    var user = await auth.ValidateTokenAsync(GetBearer(http));
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(new { user.Username, user.DisplayName });
}).WithTags("Auth");

// Client desktop heartbeat — used to force sign-out after Super Admin deactivates.
app.MapGet("/api/auth/access", async (HttpRequest http, IAuthService auth) =>
{
    var (allowed, message) = await auth.CheckClientAccessAsync(GetBearer(http));
    return allowed
        ? Results.Ok(new { allowed = true, message })
        : Results.Json(new { allowed = false, message }, statusCode: 403);
}).WithTags("Auth");

app.MapPut("/api/admin/credentials", async (ChangeAdminCredentialsRequest request, HttpRequest http, IAuthService auth) =>
{
    var (username, err) = await auth.ChangeAdminCredentialsAsync(
        GetBearer(http),
        request.CurrentPassword,
        request.NewUsername,
        request.NewPassword);
    return err is not null
        ? Results.BadRequest(new { message = err })
        : Results.Ok(new { username, message = "Super Admin username and password updated. Use them next time you sign in." });
}).WithTags("Admin");

app.MapGet("/api/admin/serials", async (HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await catalog.GetAdminSerialsAsync());
}).WithTags("Admin");

app.MapPost("/api/admin/serials", async (AdminSerialUpsertRequest request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await catalog.AddAdminSerialAsync(request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Admin");

app.MapDelete("/api/admin/serials/{id:int}", async (int id, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (message, err) = await catalog.RemoveAdminSerialAsync(id);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(new { message });
}).WithTags("Admin");

// ---------- Dashboard ----------
app.MapGet("/api/dashboard/stats", async (HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await catalog.GetDashboardAsync());
}).WithTags("Dashboard");

// ---------- Clients ----------
app.MapGet("/api/clients", async (string? search, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await catalog.GetClientsAsync(search));
}).WithTags("Clients");

app.MapGet("/api/clients/{id:int}", async (int id, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var client = await catalog.GetClientAsync(id);
    return client is null ? Results.NotFound() : Results.Ok(client);
}).WithTags("Clients");

app.MapPost("/api/clients", async (ClientUpsertRequest request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await catalog.CreateClientAsync(request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Created($"/api/clients/{dto!.Id}", dto);
}).WithTags("Clients");

app.MapPut("/api/clients/{id:int}", async (int id, ClientUpsertRequest request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await catalog.UpdateClientAsync(id, request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Clients");

app.MapPost("/api/clients/{id:int}/deactivate", async (int id, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (dto, err) = await catalog.DeactivateClientAsync(id);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Clients");

app.MapPost("/api/clients/{id:int}/reactivate", async (int id, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (dto, err) = await catalog.ReactivateClientAsync(id);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Clients");

app.MapPost("/api/admin/factory-reset", async (HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (message, err) = await catalog.FactoryResetAsync();
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(new { message });
}).WithTags("Admin");

// ---------- Licenses ----------
app.MapGet("/api/licenses", async (string? search, int? clientId, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await catalog.GetLicensesAsync(search, clientId));
}).WithTags("Licenses");

app.MapPost("/api/licenses", async (LicenseCreateRequest request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await catalog.CreateLicenseAsync(request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Created($"/api/licenses/{dto!.Id}", dto);
}).WithTags("Licenses");

app.MapPut("/api/licenses/{id:int}", async (int id, LicenseUpdateRequest request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await catalog.UpdateLicenseAsync(id, request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Licenses");

app.MapPost("/api/licenses/{id:int}/extend", async (int id, ExtendLicenseRequest request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await catalog.ExtendLicenseAsync(id, request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Licenses");

app.MapPost("/api/licenses/{id:int}/revoke", async (int id, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (dto, err) = await catalog.RevokeLicenseAsync(id);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Licenses");

// ---------- Activations ----------
app.MapGet("/api/activations", async (string? search, bool? activeOnly, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await catalog.GetActivationsAsync(search, activeOnly));
}).WithTags("Activations");

app.MapPost("/api/activations/{id:int}/deactivate", async (int id, DeactivateRequest? request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (dto, err) = await catalog.DeactivateAsync(id, request ?? new DeactivateRequest(null));
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Activations");

// ---------- Serial admin ----------
app.MapGet("/api/serials", async (string? search, string? status, HttpRequest http, IAuthService auth, ISerialService serials) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await serials.GetAllAsync(search, status));
}).WithTags("Serial");

app.MapPost("/api/serials/{id:int}/approve", async (int id, SerialApproveRequest request, HttpRequest http, IAuthService auth, ISerialService serials) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await serials.ApproveAsync(id, request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Serial");

app.MapPost("/api/serials/{id:int}/quick-approve", async (int id, QuickApproveRequest? request, HttpRequest http, IAuthService auth, ISerialService serials) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (response, err) = await serials.QuickApproveAsync(id, request?.ReviewNotes, request?.PlanType);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(response);
}).WithTags("Serial");

app.MapPost("/api/serials/{id:int}/reject", async (int id, SerialRejectRequest? request, HttpRequest http, IAuthService auth, ISerialService serials) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (dto, err) = await serials.RejectAsync(id, request ?? new SerialRejectRequest(null));
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Serial");

app.MapPost("/api/serials/{id:int}/revoke", async (int id, SerialRejectRequest? request, HttpRequest http, IAuthService auth, ISerialService serials) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var (dto, err) = await serials.RevokeAsync(id, request ?? new SerialRejectRequest(null));
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Serial");

app.MapPost("/api/serials/configure", async (SerialConfigureRequest request, HttpRequest http, IAuthService auth, ISerialService serials) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await serials.ConfigureAsync(request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Serial");

// ---------- Payments ----------
app.MapGet("/api/payments", async (string? search, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await catalog.GetPaymentsAsync(search));
}).WithTags("Payments");

app.MapPost("/api/payments", async (PaymentUpsertRequest request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await catalog.CreatePaymentAsync(request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Created($"/api/payments/{dto!.Id}", dto);
}).WithTags("Payments");

app.MapPut("/api/payments/{id:int}", async (int id, PaymentUpsertRequest request, HttpRequest http, IAuthService auth, IAdminCatalogService catalog) =>
{
    if (await auth.ValidateTokenAsync(GetBearer(http)) is null)
    {
        return Results.Unauthorized();
    }

    var error = Validate(request);
    if (error is not null)
    {
        return Results.BadRequest(new { message = error });
    }

    var (dto, err) = await catalog.UpdatePaymentAsync(id, request);
    return err is not null ? Results.BadRequest(new { message = err }) : Results.Ok(dto);
}).WithTags("Payments");

app.MapGet("/", async (LicenseDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    var provider = db.Database.ProviderName ?? "unknown";
    var adminCount = canConnect ? await db.AdminUsers.CountAsync() : -1;
    return Results.Ok(new
    {
        name = "RentalX License API",
        status = canConnect ? "running" : "db-unreachable",
        databaseProvider = provider,
        neonConnected = canConnect,
        adminUsers = adminCount
    });
});

app.MapGet("/api/health/db", async (LicenseDbContext db, IConfiguration config) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        var clients = await db.Clients.CountAsync();
        var serials = await db.SerialRegistrations.CountAsync();
        var licenses = await db.Licenses.CountAsync();
        var provider = config["Database:Provider"] ?? "PostgreSQL";
        var label = string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase)
            ? "Local SQLite"
            : "Neon PostgreSQL";
        return Results.Ok(new
        {
            ok = true,
            provider = db.Database.ProviderName,
            databaseLabel = label,
            clients,
            serials,
            licenses,
            message = $"{label} connection OK"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { ok = false, error = ex.Message, databaseLabel = "Database offline" }, statusCode: 503);
    }
}).WithTags("Health");

app.Run();

static string? GetBearer(HttpRequest http)
{
    if (!http.Headers.TryGetValue("Authorization", out var header))
    {
        return null;
    }

    var value = header.ToString();
    if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return value["Bearer ".Length..].Trim();
    }

    return value.Trim();
}

static string? Validate(object model)
{
    var ctx = new ValidationContext(model);
    var results = new List<ValidationResult>();
    if (Validator.TryValidateObject(model, ctx, results, true))
    {
        return null;
    }

    return string.Join(" ", results.Select(r => r.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
}

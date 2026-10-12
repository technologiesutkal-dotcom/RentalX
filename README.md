# RentalX

Rental management desktop app (WPF) with Super Admin licensing via `RentalX.LicenseApi` and Neon PostgreSQL.

## Projects

- `RentalX` — client desktop app
- `RentalX.Admin` — Super Admin UI (embedded)
- `RentalX.LicenseApi` — License API (deploy this to Railway / hosting)

## Local API

```bash
dotnet run --project RentalX.LicenseApi --launch-profile http
```

Default (Railway): `https://rentalx-production.up.railway.app`  
Local: `http://localhost:5159`

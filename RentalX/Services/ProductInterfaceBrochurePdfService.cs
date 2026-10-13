using System.Diagnostics;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentalX.Helpers;
using RentalX.Models;

namespace RentalX.Services;

/// <summary>
/// Full product design brochure: every screen, step-by-step, with UI layout snapshots (schematic).
/// </summary>
public sealed class ProductInterfaceBrochurePdfService
{
    private static readonly Color Brand = Color.FromHex("#0F766E");
    private static readonly Color BrandDark = Color.FromHex("#115E59");
    private static readonly Color Accent = Color.FromHex("#F59E0B");
    private static readonly Color Soft = Color.FromHex("#F0FDFA");
    private static readonly Color Ink = Color.FromHex("#0F172A");
    private static readonly Color Muted = Color.FromHex("#64748B");
    private static readonly Color ScreenBg = Color.FromHex("#F8FAFC");
    private static readonly Color Sidebar = Color.FromHex("#0F766E");

    public ProductInterfaceBrochurePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string Generate(CompanySettings? company = null)
    {
        company ??= new CompanySettings
        {
            CompanyName = "Utkal Technologies",
            BusinessType = "Rental Business",
            Phone = "",
            Email = "technologiesutkal@gmail.com",
            City = "India",
            ReportFooter = "Powered by Utkal Technologies"
        };

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RentalX",
            "Brochure");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, $"RentalX_Product_Interface_Brochure_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        Document.Create(container =>
        {
            Cover(container, company);
            Toc(container);
            DesignSystem(container);
            LoginApply(container);
            Shell(container);
            PageGuide(container, "06", "Dashboard",
                "First screen after login. Live KPIs and rental health.",
                ["Total Items", "Available", "Active Rentals", "Overdue", "Revenue highlight"],
                ["KPI cards across the top (5 metrics).",
                    "Recent bookings / activity lists below.",
                    "Alerts when overdue returns need attention.",
                    "Sidebar: Dashboard under MAIN."],
                DrawDashboardSnap);
            PageGuide(container, "07", "1. Bookings",
                "Step 1 of the rental flow — create multi-item orders.",
                ["Customer picker", "Add inventory lines", "Rental days / dates", "Advance / Full pay", "Create Booking"],
                ["Select or quick-add a customer.",
                    "Add one or more inventory lines to the cart.",
                    "Amount = Daily Rate × Qty × Days (rate from Inventory).",
                    "Optional advance; remaining balance tracked.",
                    "Stock stays in warehouse until Dispatch."],
                DrawBookingsSnap);
            PageGuide(container, "08", "Inventory",
                "Price list + stock master. Daily rate lives here.",
                ["Item name", "Category", "Qty available", "Daily rate", "Status Active/Inactive"],
                ["Add / edit / delete items.",
                    "Add stock quantity.",
                    "Search and filter by category/status.",
                    "Only Active items with stock appear in new bookings."],
                DrawTableSnap);
            PageGuide(container, "09", "2. Dispatch",
                "Step 2 — hand items to the customer (stock out).",
                ["Confirmed bookings list", "Dispatch action"],
                ["Shows bookings ready to leave the warehouse.",
                    "One click Dispatch reduces Available quantity.",
                    "No money collection on this page — stock movement only."],
                DrawListActionSnap);
            PageGuide(container, "10", "3. Receive / Returns",
                "Step 3 — return items + late/early settlement.",
                ["Out bookings", "Actual return date", "Late/Early preview", "Manual adjust", "Settle + Invoice"],
                ["Stock returns to Available.",
                    "Auto late charge or early credit from calendar days.",
                    "Manual ± adjustment for discounts/fees.",
                    "Collect balance or refund excess; print invoice PDF."],
                DrawReturnsSnap);
            PageGuide(container, "11", "Customers",
                "Customer master data for bookings.",
                ["Name", "Phone", "Email", "Address", "Search"],
                ["Add, edit, delete customers.",
                    "Search by name/phone/email.",
                    "Quick-add also available from Bookings."],
                DrawTableSnap);
            PageGuide(container, "12", "Payments",
                "Collect leftover balances anytime.",
                ["Unpaid list", "Collect amount", "Full balance", "History"],
                ["See bookings with Balance Due.",
                    "Record partial or full payment.",
                    "Advance payments tagged in history."],
                DrawListActionSnap);
            PageGuide(container, "13", "Billing",
                "Generate invoice PDFs with company header.",
                ["Booking list", "Generate Invoice"],
                ["Uses Settings company name, address, GST, logo.",
                    "Shows original amount, adjustments, paid, balance."],
                DrawListActionSnap);
            PageGuide(container, "14", "Expenses",
                "Business costs for profit clarity.",
                ["Category", "Amount", "Date", "Notes", "Search/filter"],
                ["Log daily/monthly expenses.",
                    "Used with Reports for profit snapshot."],
                DrawTableSnap);
            PageGuide(container, "15", "Maintenance",
                "Take items offline for repair.",
                ["Item list", "Under Maintenance / Active"],
                ["Mark items Under Maintenance to block new bookings.",
                    "Set Active when ready to rent again."],
                DrawListActionSnap);
            PageGuide(container, "16", "Reports",
                "Date-range business PDF for accounts.",
                ["From/To dates", "Generate Report"],
                ["Bookings, expenses, and profit summary.",
                    "Company header from Settings."],
                DrawListActionSnap);
            PageGuide(container, "17", "Users / Staff",
                "Team logins for your shop (local).",
                ["Username", "Role", "Department", "Active"],
                ["Add staff accounts for dispatch/billing.",
                    "Separate from Super Admin license portal."],
                DrawTableSnap);
            PageGuide(container, "18", "Settings",
                "Company profile + PDF tools.",
                ["Company", "Business type", "Phone/Email", "GST", "Marketing PDF", "Brochure"],
                ["Controls invoice/report headers.",
                    "Generate marketing & interface brochure PDFs.",
                    "Syncs company profile when licensed client logs in."],
                DrawSettingsSnap);
            SuperAdmin(container);
            Closing(container, company);
        }).GeneratePdf(filePath);

        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch
        {
            // ignore
        }

        return filePath;
    }

    private static void Cover(IDocumentContainer container, CompanySettings company)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.Content().Column(col =>
            {
                col.Item().Height(340).Background(BrandDark).Padding(48).Column(h =>
                {
                    h.Item().Text("UTKAL TECHNOLOGIES").FontSize(11).FontColor(Color.FromHex("#99F6E4"));
                    if (BrandAssets.LogoLightPng is { Length: > 0 } logo)
                    {
                        h.Item().PaddingTop(12).Height(56).Image(logo).FitHeight();
                    }
                    else
                    {
                        h.Item().PaddingTop(12).Text("RentalX").FontSize(40).Bold().FontColor(Colors.White);
                    }

                    h.Item().PaddingTop(14).Text("Product Design & Interface Brochure")
                        .FontSize(18).FontColor(Colors.White);
                    h.Item().PaddingTop(10).Text(
                            "Step-by-step walkthrough of every screen — login, shell, bookings flow,\n" +
                            "inventory, money settlement, reports, and Super Admin licensing.")
                        .FontSize(11).FontColor(Color.FromHex("#CCFBF1")).LineHeight(1.4f);
                    h.Item().PaddingTop(24).Height(34).Width(200).Background(Accent).AlignCenter().AlignMiddle()
                        .Text("UI · Workflow · Training").Bold().FontColor(Colors.White).FontSize(11);
                });

                col.Item().Padding(40).Column(b =>
                {
                    b.Item().Text("Prepared for").FontSize(10).FontColor(Muted);
                    b.Item().Text(company.CompanyName).FontSize(16).Bold().FontColor(BrandDark);
                    b.Item().Text($"{company.BusinessType}  ·  {company.City}").FontSize(10).FontColor(Muted);
                    b.Item().PaddingTop(16).Background(Soft).Padding(16).Text(
                            "Each page below includes: purpose, on-screen controls, what the user does, " +
                            "and a schematic snapshot of the layout (matching the live WPF interface).")
                        .FontSize(10).FontColor(Muted).LineHeight(1.4f);
                    b.Item().PaddingTop(20).Text($"Generated {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(9).FontColor(Muted);
                    b.Item().Text("API: https://rentalx-production.up.railway.app").FontSize(9).FontColor(Brand);
                });
            });
        });
    }

    private static void Toc(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("01  ·  Contents").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(14).Column(col =>
            {
                string[] items =
                [
                    "02 Design language (brand, layout, shell)",
                    "03–04 Login & Apply for Access",
                    "05 Main application shell",
                    "06 Dashboard",
                    "07 Bookings (Step 1)",
                    "08 Inventory",
                    "09 Dispatch (Step 2)",
                    "10 Receive / Returns (Step 3)",
                    "11 Customers",
                    "12 Payments",
                    "13 Billing / Invoices",
                    "14 Expenses",
                    "15 Maintenance",
                    "16 Reports",
                    "17 Users / Staff",
                    "18 Settings",
                    "19 Super Admin (license portal)",
                    "20 Closing & training tips"
                ];
                foreach (var item in items)
                {
                    col.Item().PaddingBottom(6).Text(item).FontSize(11).FontColor(Ink);
                }
            });
        });
    }

    private static void DesignSystem(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("02  ·  Design language").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(14).Column(col =>
            {
                col.Item().Text("Brand").Bold().FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "Forest teal primary (#0F766E), soft mint backgrounds, amber accents for CTAs. " +
                        "Logo appears on login (light mark on dark), sidebar, invoices, and this brochure.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);

                col.Item().PaddingTop(14).Row(row =>
                {
                    Swatch(row, Brand, "Brand");
                    Swatch(row, BrandDark, "Brand dark");
                    Swatch(row, Accent, "Accent");
                    Swatch(row, Soft, "Soft");
                });

                col.Item().PaddingTop(18).Text("Shell layout").Bold().FontColor(BrandDark);
                col.Item().PaddingTop(8).Element(DrawShellSnap);
                col.Item().PaddingTop(10).Text(
                        "Left: branded sidebar navigation (MAIN / BUSINESS / SYSTEM).\n" +
                        "Top: search, notifications, New Booking, user menu.\n" +
                        "Center: page title + content for the selected module.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);
            });
        });
    }

    private static void Swatch(RowDescriptor row, Color color, string label)
    {
        row.RelativeItem().PaddingRight(8).Column(c =>
        {
            c.Item().Height(36).Background(color);
            c.Item().PaddingTop(4).AlignCenter().Text(label).FontSize(8).FontColor(Muted);
        });
    }

    private static void LoginApply(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("03–04  ·  Login & Apply for Access").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(12).Column(col =>
            {
                col.Item().Element(DrawLoginSnap);
                col.Item().PaddingTop(12).Text("Left panel").Bold().FontColor(BrandDark);
                col.Item().Text("Brand logo, tagline, serial/status messages, Utkal Technologies credit.")
                    .FontSize(10).FontColor(Muted);
                col.Item().PaddingTop(8).Text("Right panel — Sign in").Bold().FontColor(BrandDark);
                col.Item().Text(
                        "API URL (defaults to Railway), Login ID, Password, Sign In. " +
                        "Super Admin opens the license portal; clients enter the main app.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);
                col.Item().PaddingTop(8).Text("Right panel — Apply for Access").Bold().FontColor(BrandDark);
                col.Item().Text(
                        "Company & contact fields → Submit. Machine serial is applied automatically. " +
                        "Status: Pending → Approved (then login) / Deactivated.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);
            });
        });
    }

    private static void Shell(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("05  ·  Main shell after login").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(12).Column(col =>
            {
                col.Item().Element(DrawShellSnap);
                col.Item().PaddingTop(12).Background(Soft).Padding(12).Column(b =>
                {
                    b.Item().Text("Navigation groups").Bold().FontColor(BrandDark);
                    b.Item().PaddingTop(6).Text("MAIN — Dashboard, Bookings, Inventory, Dispatch, Receive, Customers")
                        .FontSize(9).FontColor(Muted);
                    b.Item().Text("BUSINESS — Payments, Billing, Expenses, Maintenance, Reports")
                        .FontSize(9).FontColor(Muted);
                    b.Item().Text("SYSTEM — Users, Settings")
                        .FontSize(9).FontColor(Muted);
                });
            });
        });
    }

    private static void PageGuide(
        IDocumentContainer container,
        string num,
        string title,
        string purpose,
        string[] controls,
        string[] steps,
        Action<IContainer> snap)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.Header().Text($"{num}  ·  {title}").FontSize(15).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(10).Column(col =>
            {
                col.Item().Text(purpose).FontSize(10).FontColor(Muted).LineHeight(1.35f);
                col.Item().PaddingTop(10).Element(snap);
                col.Item().PaddingTop(12).Text("On-screen controls").Bold().FontSize(11).FontColor(BrandDark);
                col.Item().PaddingTop(4).Text(string.Join("  ·  ", controls)).FontSize(9).FontColor(Ink);
                col.Item().PaddingTop(10).Text("What the user does").Bold().FontSize(11).FontColor(BrandDark);
                foreach (var s in steps)
                {
                    col.Item().PaddingTop(3).Row(r =>
                    {
                        r.ConstantItem(14).Text("→").FontColor(Brand).Bold();
                        r.RelativeItem().Text(s).FontSize(9).FontColor(Muted);
                    });
                }
            });
        });
    }

    private static void SuperAdmin(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("19  ·  Super Admin license portal").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(12).Column(col =>
            {
                col.Item().Text(
                        "Opened when you sign in with Super Admin credentials against the License API. " +
                        "Same RentalX window shell, Admin modules:")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);
                col.Item().PaddingTop(10).Column(c =>
                {
                    Module(c, "Dashboard", "License stats, pending serials, DB health (Neon).");
                    Module(c, "Serials", "Approve / reject / deactivate machine access requests.");
                    Module(c, "Clients", "Client companies, activate / deactivate (forces client logout).");
                    Module(c, "Licenses", "Keys, extend, revoke.");
                    Module(c, "Payments", "License payment records.");
                    Module(c, "Account", "Change Super Admin username/password.");
                });
                col.Item().PaddingTop(12).Background(Color.FromHex("#FFFBEB")).Border(1).BorderColor(Accent).Padding(12)
                    .Text("Deactivate client or serial → client heartbeat (~90s) signs them out automatically.")
                    .FontSize(9).FontColor(Color.FromHex("#92400E"));
            });
        });
    }

    private static void Module(ColumnDescriptor col, string name, string desc)
    {
        col.Item().PaddingBottom(6).Row(r =>
        {
            r.ConstantItem(80).Text(name).SemiBold().FontColor(BrandDark).FontSize(10);
            r.RelativeItem().Text(desc).FontSize(9).FontColor(Muted);
        });
    }

    private static void Closing(IDocumentContainer container, CompanySettings company)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("20  ·  Training tips").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(12).Column(col =>
            {
                Tip(col, "Always Book → Dispatch → Receive in that order.");
                Tip(col, "Set Daily Rate in Inventory before creating bookings.");
                Tip(col, "Fill Settings so invoices show your company correctly.");
                Tip(col, "Keep API URL as https://rentalx-production.up.railway.app for live licensing.");
                Tip(col, "Distribute RentalX-Setup.exe from Google Drive — not the raw DLL folder.");

                col.Item().PaddingTop(28).Background(BrandDark).Padding(24).Column(c =>
                {
                    c.Item().Text("RentalX").FontSize(22).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(6).Text("Clear stock. Clear money. Clear returns.")
                        .FontSize(12).FontColor(Color.FromHex("#99F6E4"));
                    c.Item().PaddingTop(14).Text(company.CompanyName).FontColor(Colors.White).FontSize(11);
                    c.Item().Text(company.ReportFooter).FontSize(9).FontColor(Color.FromHex("#5EEAD4"));
                });
            });
        });
    }

    private static void Tip(ColumnDescriptor col, string text)
    {
        col.Item().PaddingBottom(6).Row(r =>
        {
            r.ConstantItem(16).Text("✓").Bold().FontColor(Brand);
            r.RelativeItem().Text(text).FontSize(10).FontColor(Muted);
        });
    }

    // ——— Schematic snapshots (UI layout mirrors) ———

    private static void DrawLoginSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Height(200).Row(row =>
        {
            row.RelativeItem(1.05f).Background(BrandDark).Padding(14).Column(c =>
            {
                c.Item().Text("RENTALX").Bold().FontColor(Colors.White).FontSize(14);
                c.Item().PaddingTop(6).Text("Rental management for your business")
                    .FontSize(8).FontColor(Color.FromHex("#99F6E4"));
                c.Item().PaddingTop(10).Text("Sign in · Apply for Access →")
                    .FontSize(8).FontColor(Color.FromHex("#CCFBF1"));
            });
            row.RelativeItem(1.15f).Background(ScreenBg).Padding(14).Column(c =>
            {
                c.Item().Text("Sign in").Bold().FontSize(11).FontColor(BrandDark);
                Field(c, "API URL");
                Field(c, "Login ID");
                Field(c, "Password");
                c.Item().PaddingTop(8).Height(22).Width(90).Background(Brand).AlignCenter().AlignMiddle()
                    .Text("Sign In").FontSize(8).FontColor(Colors.White);
            });
        });
    }

    private static void DrawShellSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Height(160).Row(row =>
        {
            row.ConstantItem(90).Background(Sidebar).Padding(8).Column(c =>
            {
                c.Item().Text("RentalX").Bold().FontColor(Colors.White).FontSize(8);
                c.Item().PaddingTop(8).Text("Dashboard").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().Text("Bookings").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().Text("Inventory").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().Text("Dispatch").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().Text("Receive").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().PaddingTop(6).Text("…").FontSize(7).FontColor(Color.FromHex("#99F6E4"));
            });
            row.RelativeItem().Background(ScreenBg).Column(c =>
            {
                c.Item().Height(28).Background(Colors.White).BorderBottom(1).BorderColor(Color.FromHex("#E2E8F0"))
                    .PaddingHorizontal(10).AlignMiddle().Text("Search  ·  Notifications  ·  New Booking  ·  User")
                    .FontSize(7).FontColor(Muted);
                c.Item().Padding(10).Text("Page content area").FontSize(9).FontColor(Muted);
            });
        });
    }

    private static void DrawDashboardSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Background(ScreenBg).Padding(10).Column(col =>
        {
            col.Item().Row(r =>
            {
                Kpi(r, "Items");
                Kpi(r, "Available");
                Kpi(r, "Active");
                Kpi(r, "Overdue");
                Kpi(r, "Revenue");
            });
            col.Item().PaddingTop(8).Height(50).Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                .AlignCenter().AlignMiddle().Text("Recent activity / charts").FontSize(8).FontColor(Muted);
        });
    }

    private static void Kpi(RowDescriptor row, string label)
    {
        row.RelativeItem().Padding(3).Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
            .Padding(6).AlignCenter().Text(label).FontSize(7).FontColor(BrandDark);
    }

    private static void DrawBookingsSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Background(ScreenBg).Padding(10).Row(row =>
        {
            row.RelativeItem(1.2f).Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                .Padding(8).Column(c =>
                {
                    c.Item().Text("New booking form").Bold().FontSize(8).FontColor(BrandDark);
                    Field(c, "Customer");
                    Field(c, "Item + Qty");
                    Field(c, "Days / Dates");
                    Field(c, "Advance");
                    c.Item().PaddingTop(6).Height(18).Background(Brand).AlignCenter().AlignMiddle()
                        .Text("Create Booking").FontSize(7).FontColor(Colors.White);
                });
            row.ConstantItem(8);
            row.RelativeItem().Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                .Padding(8).Column(c =>
                {
                    c.Item().Text("Bookings list + search").Bold().FontSize(8).FontColor(BrandDark);
                    c.Item().PaddingTop(6).Text("BK-001  ·  Customer  ·  Paid / Balance")
                        .FontSize(7).FontColor(Muted);
                    c.Item().Text("BK-002  ·  …").FontSize(7).FontColor(Muted);
                });
        });
    }

    private static void DrawTableSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Background(ScreenBg).Padding(10).Column(c =>
        {
            c.Item().Row(r =>
            {
                r.RelativeItem().Height(20).Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                    .Padding(4).AlignMiddle().Text("Search…").FontSize(7).FontColor(Muted);
                r.ConstantItem(8);
                r.ConstantItem(70).Height(20).Background(Brand).AlignCenter().AlignMiddle()
                    .Text("+ Add").FontSize(7).FontColor(Colors.White);
            });
            c.Item().PaddingTop(8).Height(70).Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                .Padding(8).Text("Data grid — rows with Edit / Delete actions").FontSize(8).FontColor(Muted);
        });
    }

    private static void DrawListActionSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Background(ScreenBg).Padding(10).Column(c =>
        {
            c.Item().Height(24).Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                .Padding(6).Text("Filter / search").FontSize(7).FontColor(Muted);
            c.Item().PaddingTop(6).Height(80).Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                .Padding(8).Column(x =>
                {
                    x.Item().Text("List of records").FontSize(8).FontColor(BrandDark);
                    x.Item().PaddingTop(8).Height(18).Width(100).Background(Brand).AlignCenter().AlignMiddle()
                        .Text("Primary action").FontSize(7).FontColor(Colors.White);
                });
        });
    }

    private static void DrawReturnsSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Background(ScreenBg).Padding(10).Row(row =>
        {
            row.RelativeItem().Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                .Padding(8).Text("Out-for-rent bookings").FontSize(8).FontColor(Muted);
            row.ConstantItem(8);
            row.RelativeItem(1.2f).Background(Colors.White).Border(1).BorderColor(Color.FromHex("#E2E8F0"))
                .Padding(8).Column(c =>
                {
                    c.Item().Text("Settlement preview").Bold().FontSize(8).FontColor(BrandDark);
                    c.Item().PaddingTop(4).Text("Late / Early / Manual adjust").FontSize(7).FontColor(Muted);
                    c.Item().Text("Collect / Refund / Invoice").FontSize(7).FontColor(Muted);
                });
        });
    }

    private static void DrawSettingsSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Background(ScreenBg).Padding(10).Column(c =>
        {
            Field(c, "Company name");
            Field(c, "Business type");
            Field(c, "Phone / Email / Address");
            c.Item().PaddingTop(8).Row(r =>
            {
                r.RelativeItem().PaddingRight(4).Height(20).Background(Brand).AlignCenter().AlignMiddle()
                    .Text("Save").FontSize(7).FontColor(Colors.White);
                r.RelativeItem().PaddingRight(4).Height(20).Background(Accent).AlignCenter().AlignMiddle()
                    .Text("Marketing PDF").FontSize(7).FontColor(Colors.White);
                r.RelativeItem().Height(20).Background(BrandDark).AlignCenter().AlignMiddle()
                    .Text("Interface Brochure").FontSize(7).FontColor(Colors.White);
            });
        });
    }

    private static void Field(ColumnDescriptor col, string label)
    {
        col.Item().PaddingTop(4).Text(label).FontSize(7).FontColor(Muted);
        col.Item().Height(16).Background(Color.FromHex("#F1F5F9")).Border(0.5f).BorderColor(Color.FromHex("#CBD5E1"));
    }
}

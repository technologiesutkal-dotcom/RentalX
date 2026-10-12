using System.Diagnostics;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentalX.Helpers;

namespace RentalX.Services;

/// <summary>
/// End-to-end guide: website download, installer, payment, machine license, DB.
/// </summary>
public class ProductDeliveryGuidePdfService
{
    private static readonly Color Brand = Color.FromHex("#0F766E");
    private static readonly Color BrandDark = Color.FromHex("#115E59");
    private static readonly Color Ink = Color.FromHex("#0F172A");
    private static readonly Color Muted = Color.FromHex("#64748B");
    private static readonly Color Soft = Color.FromHex("#F0FDFA");
    private static readonly Color WarnBg = Color.FromHex("#FFFBEB");
    private static readonly Color WarnInk = Color.FromHex("#92400E");

    public ProductDeliveryGuidePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string Generate()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RentalX",
            "Guides");
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, $"RentalX_Website_Delivery_License_Guide_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        Document.Create(container =>
        {
            Cover(container);
            Overview(container);
            WebsiteFiles(container);
            PaymentLicenseFlow(container);
            TechnicalBuild(container);
            PrerequisitesDb(container);
            Checklist(container);
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

    private static void Cover(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.Content().Column(col =>
            {
                col.Item().Height(280).Background(BrandDark).Padding(44).Column(h =>
                {
                    h.Item().Text("UTKAL TECHNOLOGIES").FontSize(12).FontColor(Color.FromHex("#99F6E4"));
                    if (BrandAssets.LogoLightPng is { Length: > 0 } logo)
                    {
                        h.Item().PaddingTop(10).Height(44).Image(logo).FitHeight();
                    }
                    else
                    {
                        h.Item().PaddingTop(10).Text("RentalX").FontSize(36).Bold().FontColor(Colors.White);
                    }

                    h.Item().PaddingTop(10).Text("Website Download → Pay → Install → License").FontSize(14).FontColor(Color.FromHex("#CCFBF1"));
                    h.Item().PaddingTop(18).Text(
                            "End-to-end guide: which files to host, prerequisites, database,\n" +
                            "automated payment + machine-locked activation so another PC cannot use a copied install.")
                        .FontSize(11).FontColor(Colors.White).LineHeight(1.4f);
                });

                col.Item().Padding(40).Column(b =>
                {
                    b.Item().Text("For").FontSize(10).FontColor(Muted);
                    b.Item().Text("https://www.utkaltechnologies.com/products").FontSize(12).SemiBold().FontColor(BrandDark);
                    b.Item().PaddingTop(16).Background(Soft).Padding(14).Text(
                            "Goal: Customer downloads from your site → pays online → installs → app activates only on THAT computer. " +
                            "No manual license emails. Copying the folder to another PC will not work.")
                        .FontSize(10).FontColor(Muted).LineHeight(1.4f);
                    b.Item().PaddingTop(20).Text($"Generated {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(9).FontColor(Muted);
                });
            });
        });
    }

    private static void Overview(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));
            page.Header().Text("01  ·  Big picture (recommended architecture)").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(14).Column(col =>
            {
                col.Item().Text(
                        "RentalX is a Windows desktop app for any rental business (events, equipment, vehicles, costumes, AV, tools, and more) " +
                        "with a local SQLite database. For paid download on your website, do NOT only zip the EXE. " +
                        "Use: Installer + License Server + Payment Gateway + Machine binding.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);

                col.Item().PaddingTop(14).Text("Customer journey (fully automated)").Bold().FontSize(12).FontColor(BrandDark);
                Step(col, "1", "Browse Products on utkaltechnologies.com → open RentalX product page");
                Step(col, "2", "Click Buy / Download → redirect to payment (Razorpay / Stripe / PhonePe)");
                Step(col, "3", "After successful payment, website shows Download Installer + creates License Order in your backend");
                Step(col, "4", "Customer installs Setup.exe on their Windows PC");
                Step(col, "5", "First launch → Activation screen asks for License Key / Order ID (auto-filled from download portal if possible)");
                Step(col, "6", "App sends Machine ID (hardware fingerprint) to your License API → API binds license to that PC only");
                Step(col, "7", "App unlocks. Local SQLite DB is created automatically on that PC");

                col.Item().PaddingTop(14).Background(WarnBg).Padding(12).Column(w =>
                {
                    w.Item().Text("Important reality check").Bold().FontColor(WarnInk);
                    w.Item().PaddingTop(4).Text(
                            "100% unbreakable piracy protection is impossible for offline desktop apps. " +
                            "What you CAN do well: payment-gated download + online activation + one machine binding + " +
                            "periodic online license check. That stops casual copy/paste to another person.")
                        .FontSize(9).FontColor(WarnInk).LineHeight(1.35f);
                });
            });
        });
    }

    private static void Step(ColumnDescriptor col, string n, string text)
    {
        col.Item().PaddingTop(6).Row(r =>
        {
            r.ConstantItem(22).Height(22).Background(Brand).AlignCenter().AlignMiddle()
                .Text(n).FontColor(Colors.White).FontSize(9).Bold();
            r.RelativeItem().PaddingLeft(8).AlignMiddle().Text(text).FontSize(10).FontColor(Muted);
        });
    }

    private static void WebsiteFiles(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));
            page.Header().Text("02  ·  What to put on the website (files)").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(14).Column(col =>
            {
                col.Item().Text("Answer: host a SETUP installer — not only the raw publish folder (unless behind payment).").Bold().FontColor(BrandDark);

                col.Item().PaddingTop(12).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.3f);
                        c.RelativeColumn(2.7f);
                    });
                    Th(t, "What");
                    Th(t, "Recommendation");
                    Td(t, "Published folder (dotnet publish)");
                    Td(t, "Build output for YOU. Zip it only for testing. Prefer converting to Setup.exe for clients.");
                    Td(t, "Setup.exe / MSI installer");
                    Td(t, "BEST for website download. Installs to Program Files, Start Menu shortcut, uninstaller.");
                    Td(t, "rentalx.db");
                    Td(t, "DO NOT upload your DB to the website. Each client PC creates its own DB on first run.");
                    Td(t, "Source code (.sln / .cs)");
                    Td(t, "Never publish on the website.");
                });

                col.Item().PaddingTop(16).Text("How to create the installer package").Bold().FontSize(12).FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "1) On your PC: publish Release (self-contained win-x64)\n" +
                        "2) Wrap that folder with Inno Setup (free) or Advanced Installer / WiX → RentalX-Setup.exe\n" +
                        "3) Upload RentalX-Setup.exe to secure storage (S3 / Azure Blob / your server private folder)\n" +
                        "4) Website Download button should work ONLY after payment success (signed, time-limited download URL)")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);

                col.Item().PaddingTop(14).Background(Soft).Padding(12).Column(b =>
                {
                    b.Item().Text("Website product page fields (utkaltechnologies.com/products)").Bold().FontColor(BrandDark);
                    b.Item().PaddingTop(6).Text(
                            "• Product name: RentalX – Rental Management\n" +
                            "• Short description: Book → Dispatch → Receive → Settle\n" +
                            "• Price (one-time)\n" +
                            "• Buy Now button → payment\n" +
                            "• After pay: Download Setup + License Key\n" +
                            "• System requirements: Windows 10/11, 4GB RAM, internet for activation")
                        .FontSize(10).FontColor(Muted).LineHeight(1.35f);
                });

                col.Item().PaddingTop(12).Text(
                        "Your products page today lists Acadiflow, Business ERP, HRM, CRM, etc. " +
                        "Add RentalX the same way as another product card, then connect Buy → payment → gated download. " +
                        "Reference: https://www.utkaltechnologies.com/products")
                    .FontSize(9).FontColor(Muted).LineHeight(1.35f);
            });
        });
    }

    private static void PaymentLicenseFlow(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));
            page.Header().Text("03  ·  Automated pay + license (no manual work)").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(14).Column(col =>
            {
                col.Item().Text("You need 3 cloud pieces (small backend)").Bold().FontSize(12).FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "A) Payment gateway webhook (Razorpay/Stripe) → marks Order = Paid\n" +
                        "B) License API (your server) → creates License Key, stores MachineId when activated\n" +
                        "C) Secure file storage → Setup.exe download link only for paid orders")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);

                col.Item().PaddingTop(12).Text("License binding (stops other persons using a copy)").Bold().FontSize(12).FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "On first activation, RentalX should compute a Machine ID from:\n" +
                        "• Windows MachineGuid / motherboard + disk serial (hashed)\n" +
                        "• Send License Key + Machine ID to License API\n" +
                        "• API saves: LicenseKey → MachineId (ONE machine)\n" +
                        "• If same key used on another PC → API rejects\n" +
                        "• App stores signed license token locally; every N days re-validates online")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);

                col.Item().PaddingTop(12).Text("What happens if they copy the folder to a friend?").Bold().FontSize(12).FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "• EXE may copy, but activation token is tied to Machine ID\n" +
                        "• Friend’s PC has different Machine ID → activation fails\n" +
                        "• Offline grace period can be short (e.g. 7 days) then must check internet\n" +
                        "• Optional: allow 1 PC transfer per year via support ticket (still semi-auto)")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);

                col.Item().PaddingTop(12).Background(Soft).Padding(12).Column(b =>
                {
                    b.Item().Text("Suggested data tables on your license server").Bold().FontColor(BrandDark);
                    b.Item().PaddingTop(6).Text(
                            "Orders(Id, Email, Amount, PaymentId, Status, CreatedAt)\n" +
                            "Licenses(Id, OrderId, LicenseKey, MaxMachines=1, Status)\n" +
                            "Activations(Id, LicenseId, MachineId, ActivatedAt, LastCheckAt)\n" +
                            "Downloads(Id, OrderId, Token, ExpiresAt)")
                        .FontSize(9).FontColor(Muted).LineHeight(1.35f);
                });

                col.Item().PaddingTop(12).Text(
                        "Default app login (admin / RentalX@123) is SEPARATE from purchase license. " +
                        "License = permission to run app. Login = enter the software. Client changes login in Settings.")
                    .FontSize(9).FontColor(Muted).LineHeight(1.35f);
            });
        });
    }

    private static void TechnicalBuild(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));
            page.Header().Text("04  ·  Build & publish steps (your PC)").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(14).Column(col =>
            {
                col.Item().Text("A) Publish RentalX (self-contained – client needs no .NET install)").Bold().FontColor(BrandDark);
                col.Item().PaddingTop(6).Background(Color.FromHex("#F1F5F9")).Padding(10).Text(
                        "cd \"f:\\Dot net\\RentalX\"\n" +
                        "dotnet publish RentalX\\RentalX.csproj -c Release -r win-x64 --self-contained true -o .\\publish\\RentalX-client")
                    .FontSize(9).FontColor(Ink);

                col.Item().PaddingTop(12).Text("B) Create Setup.exe (Inno Setup example)").Bold().FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "1. Install Inno Setup (free)\n" +
                        "2. Script: copy all files from publish\\RentalX-client into {app}\n" +
                        "3. Output: RentalX-Setup-1.0.0.exe\n" +
                        "4. Upload that EXE to private cloud storage")
                    .FontSize(10).FontColor(Muted).LineHeight(1.35f);

                col.Item().PaddingTop(12).Text("C) Website integration (high level)").Bold().FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "1. Add product card on /products (like your other SaaS products)\n" +
                        "2. Buy button → create pending Order → open Razorpay Checkout\n" +
                        "3. Payment webhook → mark Paid → generate LicenseKey → email + show download page\n" +
                        "4. Download link = signed URL to RentalX-Setup-x.y.z.exe (expires in 24–48 hours)\n" +
                        "5. App activation calls https://api.yourdomain.com/licenses/activate")
                    .FontSize(10).FontColor(Muted).LineHeight(1.35f);

                col.Item().PaddingTop(12).Text("D) Code work still needed inside RentalX (next development phase)").Bold().FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "• Activation window before/after login\n" +
                        "• Machine ID generator\n" +
                        "• Call license API + store encrypted license file\n" +
                        "• Block UI if not activated / machine mismatch\n" +
                        "• Optional: force online check every 7 days")
                    .FontSize(10).FontColor(Muted).LineHeight(1.35f);

                col.Item().PaddingTop(10).Background(WarnBg).Padding(10).Text(
                        "Today’s RentalX already has login + local SQLite. " +
                        "Payment + machine license API are the next build. This PDF is your blueprint.")
                    .FontSize(9).FontColor(WarnInk);
            });
        });
    }

    private static void PrerequisitesDb(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));
            page.Header().Text("05  ·  Prerequisites & database").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(14).Column(col =>
            {
                col.Item().Text("Client PC prerequisites").Bold().FontSize(12).FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "• Windows 10 or Windows 11 (64-bit)\n" +
                        "• ~500 MB free disk (self-contained install)\n" +
                        "• Internet required for first activation (and periodic re-check)\n" +
                        "• If you publish framework-dependent: also need .NET 8 Desktop Runtime\n" +
                        "• If self-contained Setup: NO separate .NET install needed (recommended)")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);

                col.Item().PaddingTop(14).Text("Database (automatic – no schema copy to website)").Bold().FontSize(12).FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "Engine: SQLite (single file)\n" +
                        "File: rentalx.db\n" +
                        "Path: %LOCALAPPDATA%\\RentalX\\rentalx.db\n" +
                        "Example: C:\\Users\\<Name>\\AppData\\Local\\RentalX\\rentalx.db\n\n" +
                        "On first run after install, app creates DB + tables automatically.\n" +
                        "Do NOT host rentalx.db on the website.\n" +
                        "Each licensed PC gets its own private database.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);

                col.Item().PaddingTop(12).Text("Default app login (after license unlock)").Bold().FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "Login ID: admin\n" +
                        "Password: RentalX@123\n" +
                        "Client should change this in Settings → Login ID & Password")
                    .FontSize(10).FontColor(Muted).LineHeight(1.35f);

                col.Item().PaddingTop(12).Text("Backup for client").Bold().FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "Backup = copy rentalx.db from AppData\\Local\\RentalX\\\n" +
                        "License token file (once built) should also be backed up if you store it separately.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.35f);
            });
        });
    }

    private static void Checklist(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));
            page.Header().Text("06  ·  End-to-end checklist").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(14).Column(col =>
            {
                col.Item().Text("Phase 1 — Package (you can do now)").Bold().FontColor(BrandDark);
                Check(col, "Publish self-contained win-x64 build");
                Check(col, "Create RentalX-Setup.exe (Inno Setup)");
                Check(col, "Test install on a clean Windows PC");
                Check(col, "Prepare product text + price for website");

                col.Item().PaddingTop(12).Text("Phase 2 — Website + payment (Utkal site)").Bold().FontColor(BrandDark);
                Check(col, "Add RentalX card on /products");
                Check(col, "Connect Razorpay/Stripe checkout");
                Check(col, "Webhook marks order paid");
                Check(col, "Paid users get time-limited Setup download URL");
                Check(col, "Auto-email License Key + download link");

                col.Item().PaddingTop(12).Text("Phase 3 — License lock (app + API)").Bold().FontColor(BrandDark);
                Check(col, "Build License API (Orders / Licenses / Activations)");
                Check(col, "Add Activation screen in RentalX");
                Check(col, "Bind License Key → Machine ID (1 PC)");
                Check(col, "Reject activation on second machine");
                Check(col, "Periodic online re-validation");

                col.Item().PaddingTop(16).Background(BrandDark).Padding(16).Column(c =>
                {
                    c.Item().Text("Simple answer to your question").Bold().FontColor(Colors.White).FontSize(12);
                    c.Item().PaddingTop(8).Text(
                            "Upload SETUP (Setup.exe), not your source and not your DB.\n" +
                            "Payment unlocks download + license key.\n" +
                            "Activation locks that license to one PC.\n" +
                            "DB is created automatically on the client machine.")
                        .FontSize(10).FontColor(Color.FromHex("#CCFBF1")).LineHeight(1.4f);
                });

                col.Item().PaddingTop(16).Text(
                        "Next development step in RentalX: implement Activation + License API integration. " +
                        "Tell your developer/team to build Phase 3 using this blueprint.")
                    .FontSize(9).FontColor(Muted).LineHeight(1.35f);

                col.Item().PaddingTop(20).AlignCenter()
                    .Text("Utkal Technologies  ·  RentalX delivery guide  ·  Internal use")
                    .FontSize(8).FontColor(Muted);
            });
        });
    }

    private static void Th(TableDescriptor t, string text) =>
        t.Cell().Background(Brand).Padding(7).Text(text).FontColor(Colors.White).SemiBold().FontSize(9);

    private static void Td(TableDescriptor t, string text) =>
        t.Cell().BorderBottom(0.5f).BorderColor(Color.FromHex("#E2E8F0")).Padding(7).Text(text).FontSize(9).FontColor(Muted);

    private static void Check(ColumnDescriptor col, string text)
    {
        col.Item().PaddingTop(4).Row(r =>
        {
            r.ConstantItem(14).Text("☐").FontColor(Brand);
            r.RelativeItem().Text(text).FontSize(10).FontColor(Muted);
        });
    }
}

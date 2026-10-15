using System.Diagnostics;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentalX.Helpers;
using RentalX.Models;

namespace RentalX.Services;

/// <summary>
/// Client app how-to-use guide: every screen with schematic UI snapshots and step-by-step instructions.
/// </summary>
public sealed class ClientUserGuidePdfService
{
    private static readonly Color Brand = Color.FromHex("#0F766E");
    private static readonly Color BrandDark = Color.FromHex("#115E59");
    private static readonly Color Accent = Color.FromHex("#F59E0B");
    private static readonly Color Soft = Color.FromHex("#F0FDFA");
    private static readonly Color Ink = Color.FromHex("#0F172A");
    private static readonly Color Muted = Color.FromHex("#64748B");
    private static readonly Color ScreenBg = Color.FromHex("#F8FAFC");
    private static readonly Color Sidebar = Color.FromHex("#0F766E");

    public ClientUserGuidePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string Generate(CompanySettings? company = null)
    {
        company ??= new CompanySettings
        {
            CompanyName = "Utkal Technologies",
            BusinessType = "Rental Business",
            Email = "technologiesutkal@gmail.com",
            City = "India",
            ReportFooter = "Powered by Utkal Technologies"
        };

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RentalX",
            "Guides");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, $"RentalX_Client_User_Guide_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        Document.Create(container =>
        {
            Cover(container, company);
            Toc(container);
            QuickStart(container);
            LoginPage(container);
            ShellPage(container);
            Feature(container, "05", "Dashboard",
                "Your home screen after login — live numbers for stock and rentals.",
                ["KPI cards", "Recent activity", "Overdue alerts"],
                [
                    "Open RentalX and sign in — Dashboard loads first.",
                    "Check Total Items, Available, Active Rentals, Overdue, and Revenue.",
                    "If Overdue is high, open 3. Receive and settle late returns.",
                    "Use the top New Booking button for a quick start to Bookings."
                ],
                DrawDashboardSnap);
            Feature(container, "06", "Inventory (set prices first)",
                "Master list of rentable items. Daily rate used by every new booking lives here.",
                ["Add / Edit / Delete", "Add stock", "Category", "Daily rate", "Active / Inactive"],
                [
                    "Go to Inventory in the left sidebar (MAIN).",
                    "Click Add. Enter name, category, quantity, and Daily Rate (₹ per day).",
                    "Save. Only Active items with stock appear when creating bookings.",
                    "Use Add stock later when you buy more units.",
                    "Search or filter by category/status when the list grows."
                ],
                DrawTableSnap);
            Feature(container, "07", "Customers",
                "People or companies who rent from you.",
                ["Name", "Phone", "Email", "Address", "Search", "Add / Edit / Delete"],
                [
                    "Open Customers under BUSINESS (or add a customer from Bookings).",
                    "Enter at least name and phone for easy search later.",
                    "Edit anytime — bookings keep the customer link.",
                    "Search by name, phone, or email from the search box."
                ],
                DrawTableSnap);
            Feature(container, "08", "Step 1 — Bookings",
                "Create the rental order. Stock stays in the warehouse until Dispatch.",
                ["Customer picker", "Add inventory lines", "Rental days / dates", "Advance / Full pay", "Create Booking"],
                [
                    "Open 1. Bookings (or click New Booking in the top bar).",
                    "Select an existing customer or quick-add a new one.",
                    "Add one or more inventory lines (item + quantity).",
                    "Set rental days / dates. Amount = Daily Rate × Qty × Days.",
                    "Optionally collect an advance (or full payment).",
                    "Click Create Booking. Status becomes Confirmed.",
                    "Remember: nothing leaves the shop until you Dispatch."
                ],
                DrawBookingsSnap);
            Feature(container, "09", "Step 2 — Dispatch",
                "Hand items to the customer (stock out of the warehouse).",
                ["Confirmed bookings list", "Dispatch action"],
                [
                    "Open 2. Dispatch.",
                    "Find the Confirmed booking that is ready to leave.",
                    "Click Dispatch. Available quantity decreases.",
                    "Status becomes Dispatched. No money is collected on this page."
                ],
                DrawListActionSnap);
            Feature(container, "10", "Step 3 — Receive / Returns",
                "Customer returns items. Settle late fees, early credits, and balance; print invoice.",
                ["Out bookings", "Actual return date", "Late / Early preview", "Manual adjust", "Settle + Invoice"],
                [
                    "Open 3. Receive.",
                    "Select the Dispatched (out) booking.",
                    "Set the actual return date. The app previews late charge or early credit from calendar days.",
                    "Use Manual adjust for discounts or extra fees if needed.",
                    "Collect remaining balance or refund excess.",
                    "Settle — stock returns to Available; print/save the invoice PDF.",
                    "Invoices save under Documents\\RentalX\\Invoices\\."
                ],
                DrawReturnsSnap);
            Feature(container, "11", "Payments",
                "Collect leftover balances anytime (not only at return).",
                ["Unpaid list", "Collect amount", "Full balance", "Payment history"],
                [
                    "Open Payments under BUSINESS.",
                    "See bookings with Balance Due.",
                    "Enter a partial amount or collect the full balance.",
                    "Advances and later collections appear in payment history."
                ],
                DrawListActionSnap);
            Feature(container, "12", "Billing / Invoices",
                "Regenerate invoice PDFs with your company header from Settings.",
                ["Booking list", "Generate Invoice"],
                [
                    "Open Billing.",
                    "Pick a booking that has been settled (or needs a reprint).",
                    "Click Generate Invoice.",
                    "Header uses company name, address, GST, and logo from Settings."
                ],
                DrawListActionSnap);
            Feature(container, "13", "Expenses",
                "Log business costs so Reports can show a clearer profit picture.",
                ["Category", "Amount", "Date", "Notes", "Search"],
                [
                    "Open Expenses.",
                    "Add category, amount, date, and optional notes.",
                    "Search/filter when reviewing the month.",
                    "These feed into the Reports PDF summary."
                ],
                DrawTableSnap);
            Feature(container, "14", "Maintenance",
                "Take damaged or repairing items offline so they cannot be booked.",
                ["Item list", "Under Maintenance", "Set Active"],
                [
                    "Open Maintenance.",
                    "Select the item and mark Under Maintenance.",
                    "It will not appear for new bookings until you set Active again."
                ],
                DrawListActionSnap);
            Feature(container, "15", "Reports",
                "Date-range business PDF for accounts and owners.",
                ["From date", "To date", "Generate Report"],
                [
                    "Open Reports.",
                    "Choose From and To dates.",
                    "Click Generate Report.",
                    "PDF includes bookings, expenses, and profit summary with your company header.",
                    "Saved under Documents\\RentalX\\Reports\\."
                ],
                DrawListActionSnap);
            Feature(container, "16", "Staff (Users)",
                "Local team directory for your shop (separate from license login).",
                ["Username", "Role", "Department", "Active"],
                [
                    "Open Staff under SYSTEM.",
                    "Add staff names/roles for your internal records.",
                    "This is not the Super Admin license portal — day-to-day access still uses your Client Login ID."
                ],
                DrawTableSnap);
            Feature(container, "17", "Settings",
                "Company profile used on invoices and reports. Change local login password here.",
                ["Company / shop name", "Business type", "Phone / Email / Address", "GST", "Currency", "Footer", "Save", "Local login"],
                [
                    "Open Settings under SYSTEM.",
                    "Fill company name, owner, address, phone, email, GST as needed.",
                    "Set currency and report/invoice footer text.",
                    "Click Save Settings — future PDFs use these details.",
                    "Below, update local Login ID / password if required (enter current password first).",
                    "When you signed in as a licensed client, company fields may sync from your portal profile."
                ],
                DrawSettingsSnap);
            GoldenRules(container, company);
        }).GeneratePdf(filePath);

        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch
        {
            // Viewer optional
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

                    h.Item().PaddingTop(14).Text("Client App — How to Use Guide")
                        .FontSize(20).FontColor(Colors.White);
                    h.Item().PaddingTop(10).Text(
                            "Step-by-step instructions for every feature in the customer desktop app,\n" +
                            "with on-screen layout snapshots (schematics) matching the live interface.")
                        .FontSize(11).FontColor(Color.FromHex("#CCFBF1")).LineHeight(1.4f);
                    h.Item().PaddingTop(24).Height(34).Width(220).Background(Accent).AlignCenter().AlignMiddle()
                        .Text("Training · Daily operations").Bold().FontColor(Colors.White).FontSize(11);
                });

                col.Item().Padding(40).Column(b =>
                {
                    b.Item().Text("Prepared for").FontSize(10).FontColor(Muted);
                    b.Item().Text(company.CompanyName).FontSize(16).Bold().FontColor(BrandDark);
                    b.Item().Text($"{company.BusinessType}  ·  {company.City}").FontSize(10).FontColor(Muted);
                    b.Item().PaddingTop(16).Background(Soft).Padding(16).Text(
                            "Golden path: Inventory → Customers → Book → Dispatch → Receive → Invoice. " +
                            "Each chapter shows a UI snapshot, what you see, and exactly what to click.")
                        .FontSize(10).FontColor(Muted).LineHeight(1.4f);
                    b.Item().PaddingTop(20).Text($"Generated {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(9).FontColor(Muted);
                    b.Item().Text("Support: Utkal Technologies").FontSize(9).FontColor(Brand);
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
                    "02 Quick start (first day checklist)",
                    "03 Sign in & Apply for Access",
                    "04 Main app shell (sidebar & top bar)",
                    "05 Dashboard",
                    "06 Inventory — set daily rates",
                    "07 Customers",
                    "08 Bookings (Step 1)",
                    "09 Dispatch (Step 2)",
                    "10 Receive / Returns (Step 3)",
                    "11 Payments",
                    "12 Billing / Invoices",
                    "13 Expenses",
                    "14 Maintenance",
                    "15 Reports",
                    "16 Staff",
                    "17 Settings",
                    "18 Golden rules & tips"
                ];
                foreach (var item in items)
                {
                    col.Item().PaddingBottom(6).Text(item).FontSize(11).FontColor(Ink);
                }
            });
        });
    }

    private static void QuickStart(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("02  ·  Quick start").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(12).Column(col =>
            {
                col.Item().Text("Do this on day one (in order)").Bold().FontColor(BrandDark);
                Step(col, "1", "Install RentalX-Setup.exe and open the app.");
                Step(col, "2", "New PC? Click Apply for Access, submit company details, wait for approval.");
                Step(col, "3", "Sign in with the Login ID and password from Utkal Technologies.");
                Step(col, "4", "Open Settings — save your shop name, phone, address, GST.");
                Step(col, "5", "Open Inventory — add items with Daily Rate and stock qty.");
                Step(col, "6", "Add a Customer, then create a Booking → Dispatch → Receive.");
                Step(col, "7", "Print the invoice and check Documents\\RentalX\\Invoices.");

                col.Item().PaddingTop(20).Background(Soft).Padding(14).Column(b =>
                {
                    b.Item().Text("Money formula").Bold().FontColor(BrandDark);
                    b.Item().PaddingTop(6).Text("Line amount = Daily Rate × Quantity × Rental Days")
                        .FontSize(11).FontColor(Ink);
                    b.Item().PaddingTop(4).Text("Advance reduces Balance Due. Settle remaining money on Receive or Payments.")
                        .FontSize(10).FontColor(Muted).LineHeight(1.35f);
                });

                col.Item().PaddingTop(16).Background(Color.FromHex("#FFFBEB")).Border(1).BorderColor(Accent).Padding(12)
                    .Text("Never skip Dispatch. Booking alone does not reduce stock — Dispatch does.")
                    .FontSize(10).FontColor(Color.FromHex("#92400E"));
            });
        });
    }

    private static void LoginPage(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("03  ·  Sign in & Apply for Access").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(12).Column(col =>
            {
                col.Item().Element(DrawLoginSnap);
                col.Item().PaddingTop(12).Text("Left panel").Bold().FontColor(BrandDark);
                col.Item().Text("Greeting, date/time, and rotating tips. Powered by Utkal Technologies.")
                    .FontSize(10).FontColor(Muted);
                col.Item().PaddingTop(8).Text("Sign in").Bold().FontColor(BrandDark);
                col.Item().Text(
                        "Enter Login ID and Password provided after your access is approved. " +
                        "Optional: Show password. Click Sign In to open the client app.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);
                col.Item().PaddingTop(8).Text("Apply for Access (new installation)").Bold().FontColor(BrandDark);
                col.Item().Text(
                        "Click Apply for Access → fill Company, Email, Phone, Address, Pincode → Submit. " +
                        "Your computer is registered automatically in the background (no serial shown on screen). " +
                        "One application per PC. When approved, sign in with the credentials you receive.")
                    .FontSize(10).FontColor(Muted).LineHeight(1.4f);
            });
        });
    }

    private static void ShellPage(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("04  ·  Main app shell").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(12).Column(col =>
            {
                col.Item().Element(DrawShellSnap);
                col.Item().PaddingTop(12).Background(Soft).Padding(12).Column(b =>
                {
                    b.Item().Text("Where to click").Bold().FontColor(BrandDark);
                    b.Item().PaddingTop(6).Text("MAIN — Dashboard, 1. Bookings, Inventory, 2. Dispatch, 3. Receive")
                        .FontSize(9).FontColor(Muted);
                    b.Item().Text("BUSINESS — Billing, Payments, Expenses, Maintenance, Customers, Reports")
                        .FontSize(9).FontColor(Muted);
                    b.Item().Text("SYSTEM — Settings, Staff")
                        .FontSize(9).FontColor(Muted);
                    b.Item().PaddingTop(8).Text("Top bar — Search, Notifications, New Booking, user menu / sign out")
                        .FontSize(9).FontColor(Muted);
                });
            });
        });
    }

    private static void Feature(
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
                col.Item().PaddingTop(12).Text("What you see on screen").Bold().FontSize(11).FontColor(BrandDark);
                col.Item().PaddingTop(4).Text(string.Join("  ·  ", controls)).FontSize(9).FontColor(Ink);
                col.Item().PaddingTop(10).Text("How to use (step by step)").Bold().FontSize(11).FontColor(BrandDark);
                var i = 1;
                foreach (var s in steps)
                {
                    var n = i++;
                    col.Item().PaddingTop(3).Row(r =>
                    {
                        r.ConstantItem(18).Text($"{n}.").FontColor(Brand).Bold().FontSize(9);
                        r.RelativeItem().Text(s).FontSize(9).FontColor(Muted);
                    });
                }
            });
        });
    }

    private static void GoldenRules(IDocumentContainer container, CompanySettings company)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.Header().Text("18  ·  Golden rules & tips").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(12).Column(col =>
            {
                Tip(col, "Always Book → Dispatch → Receive in that order.");
                Tip(col, "Set Daily Rate in Inventory before creating bookings.");
                Tip(col, "Save Settings so invoices show your real company details.");
                Tip(col, "Use Maintenance for broken items — do not delete if you need history.");
                Tip(col, "PDFs open from Documents\\RentalX (Invoices, Reports).");
                Tip(col, "If you are signed out suddenly, contact Utkal Technologies — access may have been updated.");
                Tip(col, "Need a new Login ID? Contact Utkal Technologies after Apply for Access is approved.");

                col.Item().PaddingTop(28).Background(BrandDark).Padding(24).Column(c =>
                {
                    c.Item().Text("RentalX Client Guide").FontSize(20).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(6).Text("Clear stock. Clear money. Clear returns.")
                        .FontSize(12).FontColor(Color.FromHex("#99F6E4"));
                    c.Item().PaddingTop(14).Text(company.CompanyName).FontColor(Colors.White).FontSize(11);
                    c.Item().Text(company.ReportFooter).FontSize(9).FontColor(Color.FromHex("#5EEAD4"));
                });
            });
        });
    }

    private static void Step(ColumnDescriptor col, string n, string text)
    {
        col.Item().PaddingTop(6).Row(r =>
        {
            r.ConstantItem(22).Height(22).Background(Brand).AlignCenter().AlignMiddle()
                .Text(n).FontSize(9).Bold().FontColor(Colors.White);
            r.RelativeItem().PaddingLeft(8).AlignMiddle().Text(text).FontSize(10).FontColor(Muted);
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

    // ——— Schematic snapshots ———

    private static void DrawLoginSnap(IContainer container)
    {
        container.Border(1).BorderColor(Color.FromHex("#CBD5E1")).Height(200).Row(row =>
        {
            row.RelativeItem(1.05f).Background(BrandDark).Padding(14).Column(c =>
            {
                c.Item().Text("RENTALX").Bold().FontColor(Colors.White).FontSize(14);
                c.Item().PaddingTop(6).Text("Good afternoon").FontSize(10).FontColor(Color.FromHex("#99F6E4"));
                c.Item().Text("Wednesday · live tips").FontSize(7).FontColor(Color.FromHex("#5EEAD4"));
                c.Item().PaddingTop(10).Background(Color.FromHex("#042F2E")).Padding(6)
                    .Text("TODAY'S TIP — Book → Dispatch → Return")
                    .FontSize(7).FontColor(Color.FromHex("#FDE68A"));
            });
            row.RelativeItem(1.15f).Background(ScreenBg).Padding(14).Column(c =>
            {
                c.Item().Text("Sign in").Bold().FontSize(11).FontColor(BrandDark);
                Field(c, "Login ID");
                Field(c, "Password");
                c.Item().PaddingTop(8).Height(22).Width(90).Background(Brand).AlignCenter().AlignMiddle()
                    .Text("Sign In").FontSize(8).FontColor(Colors.White);
                c.Item().PaddingTop(6).Height(18).Width(110).Background(Color.FromHex("#CCFBF1")).AlignCenter().AlignMiddle()
                    .Text("Apply for Access").FontSize(7).FontColor(BrandDark);
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
                c.Item().Text("1. Bookings").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().Text("Inventory").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().Text("2. Dispatch").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().Text("3. Receive").FontSize(7).FontColor(Color.FromHex("#CCFBF1"));
                c.Item().PaddingTop(6).Text("…").FontSize(7).FontColor(Color.FromHex("#99F6E4"));
            });
            row.RelativeItem().Background(ScreenBg).Column(c =>
            {
                c.Item().Height(28).Background(Colors.White).BorderBottom(1).BorderColor(Color.FromHex("#E2E8F0"))
                    .PaddingHorizontal(10).AlignMiddle().Text("Search  ·  Notifications  ·  New Booking  ·  User")
                    .FontSize(7).FontColor(Muted);
                c.Item().Padding(10).Text("Selected page content").FontSize(9).FontColor(Muted);
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
                .AlignCenter().AlignMiddle().Text("Recent activity").FontSize(8).FontColor(Muted);
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
                    c.Item().PaddingTop(6).Text("BK-001  ·  Customer  ·  Balance")
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
                .Padding(8).Text("Data grid — rows with Edit / Delete").FontSize(8).FontColor(Muted);
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
            Field(c, "Phone / Email / Address / GST");
            c.Item().PaddingTop(8).Height(20).Width(100).Background(Brand).AlignCenter().AlignMiddle()
                .Text("Save Settings").FontSize(7).FontColor(Colors.White);
            c.Item().PaddingTop(8).Background(Color.FromHex("#F1F5F9")).Padding(6)
                .Text("Local login ID / password change").FontSize(7).FontColor(Muted);
        });
    }

    private static void Field(ColumnDescriptor col, string label)
    {
        col.Item().PaddingTop(4).Text(label).FontSize(7).FontColor(Muted);
        col.Item().Height(16).Background(Color.FromHex("#F1F5F9")).Border(0.5f).BorderColor(Color.FromHex("#CBD5E1"));
    }
}

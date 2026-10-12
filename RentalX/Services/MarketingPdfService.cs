using System.Diagnostics;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentalX.Helpers;
using RentalX.Models;

namespace RentalX.Services;

public class MarketingPdfService : IMarketingPdfService
{
    private static readonly Color Brand = Color.FromHex("#0F766E");
    private static readonly Color BrandDark = Color.FromHex("#115E59");
    private static readonly Color Accent = Color.FromHex("#F59E0B");
    private static readonly Color Soft = Color.FromHex("#F0FDFA");
    private static readonly Color Ink = Color.FromHex("#134E4A");
    private static readonly Color Muted = Color.FromHex("#64748B");

    public MarketingPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string Generate(CompanySettings company)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RentalX",
            "Marketing");
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, $"RentalX_Marketing_Guide_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        Document.Create(container =>
        {
            CoverPage(container, company);
            WorkflowPage(container);
            ModulesPage(container);
            MoneySettlementPage(container);
            QuickStartPage(container, company);
        }).GeneratePdf(filePath);

        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch
        {
            // Viewer may be unavailable; file is still saved.
        }

        return filePath;
    }

    private static void CoverPage(IDocumentContainer container, CompanySettings company)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.DefaultTextStyle(x => x.FontColor(Ink));

            page.Content().Column(col =>
            {
                col.Item().Height(320).Background(BrandDark).Padding(48).Column(hero =>
                {
                    if (BrandAssets.LogoLightPng is { Length: > 0 } logo)
                    {
                        hero.Item().Height(48).Image(logo).FitHeight();
                    }
                    else
                    {
                        hero.Item().Text("RENTALX")
                            .FontSize(42).Bold().FontColor(Colors.White);
                    }

                    hero.Item().PaddingTop(12).Text("Software for any rental business")
                        .FontSize(16).FontColor(Color.FromHex("#99F6E4"));
                    hero.Item().PaddingTop(18).Text(
                            "Book → Dispatch → Receive → Settle\nInventory, payments, invoices & reports — in one clear flow.")
                        .FontSize(12).FontColor(Colors.White).LineHeight(1.4f);
                    hero.Item().PaddingTop(28).Row(row =>
                    {
                        row.ConstantItem(150).Height(36).Background(Accent).AlignCenter().AlignMiddle()
                            .Text("Product Guide").Bold().FontColor(Colors.White).FontSize(11);
                        row.ConstantItem(12);
                        row.RelativeItem().AlignMiddle()
                            .Text($"{company.CompanyName}  ·  {company.BusinessType}")
                            .FontSize(10).FontColor(Color.FromHex("#CCFBF1"));
                    });
                });

                col.Item().Padding(40).Column(body =>
                {
                    body.Item().Text("Why RentalX?").FontSize(18).Bold().FontColor(BrandDark);
                    body.Item().PaddingTop(10).Text(
                            $"Built for {company.BusinessType.ToLowerInvariant()} and every other rental shop — stock control, " +
                            "customer bookings, advance money, dispatch tracking, and fair late/early settlement — without spreadsheet chaos. " +
                            "Use the same flow for tents, equipment, vehicles, costumes, cameras, tools, furniture, and more.")
                        .FontSize(11).FontColor(Muted).LineHeight(1.45f);

                    body.Item().PaddingTop(22).Row(row =>
                    {
                        FeatureChip(row, "3-step rental flow");
                        FeatureChip(row, "Advance + balance");
                        FeatureChip(row, "Late / early settle");
                        FeatureChip(row, "PDF invoices");
                    });

                    body.Item().PaddingTop(28).Background(Soft).Padding(18).Column(box =>
                    {
                        box.Item().Text("What this PDF covers").Bold().FontColor(BrandDark).FontSize(12);
                        box.Item().PaddingTop(8).Text("1. End-to-end workflow everyone can follow").FontSize(10);
                        box.Item().Text("2. All working modules (Inventory to Reports)").FontSize(10);
                        box.Item().Text("3. Money: advance, full pay, late charges, early credit, refunds").FontSize(10);
                        box.Item().Text("4. Quick start checklist for your team").FontSize(10);
                    });

                    body.Item().PaddingTop(30).Text($"{company.Phone}  ·  {company.Email}  ·  {company.City}")
                        .FontSize(9).FontColor(Muted);
                });
            });
        });
    }

    private static void FeatureChip(RowDescriptor row, string text)
    {
        row.RelativeItem().PaddingRight(8).Border(1).BorderColor(Color.FromHex("#99F6E4"))
            .Background(Colors.White).Padding(10)
            .AlignCenter().Text(text).FontSize(9).SemiBold().FontColor(BrandDark);
    }

    private static void WorkflowPage(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));

            page.Header().Text("01  ·  How the business flow works").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(16).Column(col =>
            {
                col.Spacing(14);

                StepCard(col, "1", "BOOKINGS — Create the order",
                    "Customer books an item. Stock stays in your godown. Amount = rate × qty × days (from Inventory price). " +
                    "Take Advance now, or Full payment, or leave unpaid for later.");

                StepCard(col, "2", "DISPATCH — Give items to customer",
                    "When items leave the warehouse, click Dispatch. Available stock goes down. " +
                    "This step is about stock movement — not collecting money.");

                StepCard(col, "3", "RECEIVE — Return + settlement",
                    "Customer brings items back. Stock returns to Available. " +
                    "System auto-calculates Late charges or Early credit. You can also Manual adjust. " +
                    "Settle remaining balance or refund excess advance, then print invoice PDF.");

                col.Item().PaddingTop(8).Background(Soft).Padding(16).Column(box =>
                {
                    box.Item().Text("Simple money rule").Bold().FontColor(BrandDark);
                    box.Item().PaddingTop(6).Text(
                        "Final amount − Already paid = Collect now\n" +
                        "If paid more than final (early credit) = Refund due\n" +
                        "Inventory holds the daily price — bookings use that rate automatically.")
                        .FontSize(10).FontColor(Muted).LineHeight(1.4f);
                });

                col.Item().PaddingTop(6).Text("Sidebar order in the app: 1. Bookings → 2. Dispatch → 3. Receive")
                    .FontSize(10).SemiBold().FontColor(Brand);
            });
        });
    }

    private static void StepCard(ColumnDescriptor col, string num, string title, string body)
    {
        col.Item().Border(1).BorderColor(Color.FromHex("#CCFBF1")).Padding(14).Row(row =>
        {
            row.ConstantItem(36).Height(36).Background(Brand).AlignCenter().AlignMiddle()
                .Text(num).Bold().FontColor(Colors.White).FontSize(14);
            row.ConstantItem(12);
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(title).Bold().FontSize(12).FontColor(BrandDark);
                c.Item().PaddingTop(4).Text(body).FontSize(10).FontColor(Muted).LineHeight(1.4f);
            });
        });
    }

    private static void ModulesPage(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));

            page.Header().Text("02  ·  Working modules (all included)").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(16).Column(col =>
            {
                col.Spacing(8);
                ModuleRow(col, "Dashboard", "Live overview of bookings, stock pressure, payments & alerts.");
                ModuleRow(col, "Inventory", "Add / edit all fields / delete / add stock. Daily rate lives here. Search + category/status filters.");
                ModuleRow(col, "Customers", "Add, edit, delete customers. Search by name, phone, email, address.");
                ModuleRow(col, "Bookings", "New booking form, quick add customer, advance or full pay, search all orders.");
                ModuleRow(col, "Dispatch", "List of confirmed bookings ready to give out. One click = stock out.");
                ModuleRow(col, "Receive", "Return items, late/early settlement preview, manual adjust, collect/refund, invoice.");
                ModuleRow(col, "Payments", "Unpaid balances, collect amount / full balance, payment history (advance tagged).");
                ModuleRow(col, "Billing", "Generate invoice PDFs anytime with company header from Settings.");
                ModuleRow(col, "Expenses", "Log business costs with categories; search & filter for profit clarity.");
                ModuleRow(col, "Maintenance", "Mark items Under Maintenance or Active (blocked from new bookings).");
                ModuleRow(col, "Reports", "Date-range business PDF: bookings, expenses, profit snapshot.");
                ModuleRow(col, "Staff / Users", "Add team members, roles, departments, active/inactive.");
                ModuleRow(col, "Settings", "Business type, company header for invoices/reports + marketing PDF for any rental niche.");
                ModuleRow(col, "Global search", "Top bar search jumps across bookings, customers, inventory & more.");
            });
        });
    }

    private static void ModuleRow(ColumnDescriptor col, string name, string desc)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(110).Text(name).SemiBold().FontColor(BrandDark).FontSize(10);
            row.RelativeItem().Text(desc).FontSize(9).FontColor(Muted);
        });
        col.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(Color.FromHex("#E2E8F0"));
    }

    private static void MoneySettlementPage(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));

            page.Header().Text("03  ·  Money & settlement (understandable)").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(16).Column(col =>
            {
                col.Item().Text("At booking time").Bold().FontSize(12).FontColor(BrandDark);
                col.Item().PaddingTop(6).Text(
                        "• Enter Advance (₹) — or press Full to take entire rental now\n" +
                        "• Paid and Balance show on every booking row\n" +
                        "• Inventory daily rate drives the total (edit price in Inventory, not on each booking)")
                    .FontSize(10).FontColor(Muted).LineHeight(1.45f);

                col.Item().PaddingTop(16).Text("At receive time (automatic)").Bold().FontSize(12).FontColor(BrandDark);
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(2.8f);
                    });

                    HeaderCell(table, "Case");
                    HeaderCell(table, "What RentalX does");

                    BodyCell(table, "Late return");
                    BodyCell(table, "Extra = late days × daily rate × quantity. Added to final amount.");

                    BodyCell(table, "Early return");
                    BodyCell(table, "Credit = early days × rate × qty (optional checkbox). Can create refund if advance was high.");

                    BodyCell(table, "On time");
                    BodyCell(table, "No auto change. Collect only remaining balance.");

                    BodyCell(table, "Manual adjust");
                    BodyCell(table, "Type +500 for extra fee or -200 for discount. Easy override.");
                });

                col.Item().PaddingTop(18).Background(Color.FromHex("#FFFBEB")).Border(1).BorderColor(Accent).Padding(14).Column(box =>
                {
                    box.Item().Text("Example").Bold().FontColor(Color.FromHex("#92400E"));
                    box.Item().PaddingTop(6).Text(
                            "Chair ₹15/day × 100 qty × 2 days = ₹3,000 booked.\n" +
                            "Customer paid ₹1,000 advance.\n" +
                            "Returned 1 day late → late charge ₹15 × 100 × 1 = ₹1,500.\n" +
                            "Final = ₹4,500. Collect now = ₹3,500.")
                        .FontSize(10).FontColor(Color.FromHex("#78350F")).LineHeight(1.4f);
                });

                col.Item().PaddingTop(16).Text(
                        "Invoices show original amount, adjustment, paid, and balance so customers understand the settlement.")
                    .FontSize(10).FontColor(Muted);
            });
        });
    }

    private static void HeaderCell(TableDescriptor table, string text)
    {
        table.Cell().Background(Brand).Padding(8).Text(text).FontColor(Colors.White).SemiBold().FontSize(9);
    }

    private static void BodyCell(TableDescriptor table, string text)
    {
        table.Cell().BorderBottom(0.5f).BorderColor(Color.FromHex("#E2E8F0")).Padding(8)
            .Text(text).FontSize(9).FontColor(Muted);
    }

    private static void QuickStartPage(IDocumentContainer container, CompanySettings company)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontColor(Ink).FontSize(10));

            page.Header().Text("04  ·  Quick start for your team").FontSize(16).Bold().FontColor(BrandDark);
            page.Content().PaddingTop(16).Column(col =>
            {
                col.Spacing(10);
                col.Item().Text("Day-1 setup").Bold().FontSize(12).FontColor(BrandDark);
                Checklist(col, "Open Settings → fill company name, phone, GST (appears on invoices)");
                Checklist(col, "Inventory → add items with Daily Rate (this is your price list)");
                Checklist(col, "Customers → add your regular clients (or quick-add from Bookings)");
                Checklist(col, "Staff → add dispatch / billing users if needed");

                col.Item().PaddingTop(12).Text("Daily operations").Bold().FontSize(12).FontColor(BrandDark);
                Checklist(col, "Create Booking (+ advance if cash received)");
                Checklist(col, "When giving items → Dispatch");
                Checklist(col, "When items come back → Receive (check settlement preview, then settle)");
                Checklist(col, "Use Payments page anytime for leftover balances");
                Checklist(col, "Billing / Reports → PDF for customers & accounts");

                col.Item().PaddingTop(18).Background(BrandDark).Padding(22).Column(cta =>
                {
                    cta.Item().Text("RentalX").FontSize(20).Bold().FontColor(Colors.White);
                    cta.Item().PaddingTop(6).Text("Clear stock. Clear money. Clear returns.")
                        .FontSize(12).FontColor(Color.FromHex("#99F6E4"));
                    cta.Item().PaddingTop(14).Text($"{company.CompanyName}")
                        .FontSize(11).FontColor(Colors.White);
                    cta.Item().Text($"{company.Address}, {company.City}")
                        .FontSize(9).FontColor(Color.FromHex("#CCFBF1"));
                    cta.Item().Text($"{company.Phone}  ·  {company.Email}")
                        .FontSize(9).FontColor(Color.FromHex("#CCFBF1"));
                    cta.Item().PaddingTop(12).Text(company.ReportFooter)
                        .FontSize(9).Italic().FontColor(Color.FromHex("#5EEAD4"));
                });

                col.Item().PaddingTop(20).AlignCenter()
                    .Text("Generated by RentalX  ·  Keep this PDF for staff training & client demos")
                    .FontSize(8).FontColor(Muted);
            });
        });
    }

    private static void Checklist(ColumnDescriptor col, string text)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(16).Text("✓").FontColor(Brand).Bold();
            row.RelativeItem().Text(text).FontSize(10).FontColor(Muted);
        });
    }
}

using System.Diagnostics;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentalX.Helpers;
using RentalX.Models;

namespace RentalX.Services;

public class PdfReportService : IPdfReportService
{
    private static readonly Color Brand = Color.FromHex("#0F766E");
    private static readonly Color BrandDark = Color.FromHex("#004D40");

    public PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string GenerateBusinessReport(
        CompanySettings company,
        DateTime fromDate,
        DateTime toDate,
        ReportSummary summary,
        IReadOnlyList<ReportBookingRow> bookings,
        IReadOnlyList<Expense> expenses)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RentalX",
            "Reports");
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(
            folder,
            $"RentalX_Report_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.BlueGrey.Darken3));

                page.Background().AlignCenter().AlignMiddle().Text("RentalX")
                    .FontSize(72)
                    .FontColor(Color.FromHex("#CCFBF1"))
                    .Bold();

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            if (BrandAssets.LogoPng is { Length: > 0 } logo)
                            {
                                left.Item().Height(36).Image(logo).FitHeight();
                            }
                            else
                            {
                                left.Item().Text("RentalX").FontSize(16).Bold().FontColor(BrandDark);
                            }

                            left.Item().PaddingTop(6).Text(company.CompanyName).FontSize(18).Bold().FontColor(BrandDark);
                            left.Item().Text($"Owner: {company.OwnerName}").FontSize(10);
                            left.Item().Text($"{company.Address}, {company.City}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            left.Item().Text($"Phone: {company.Phone}  |  Email: {company.Email}").FontSize(9);
                            if (company.ShowGstOnReports && !string.IsNullOrWhiteSpace(company.GstNumber))
                            {
                                left.Item().Text($"GST: {company.GstNumber}").FontSize(9);
                            }
                        });

                        row.ConstantItem(150).AlignRight().Column(right =>
                        {
                            right.Item().Text("BUSINESS REPORT").Bold().FontSize(12).FontColor(Brand);
                            right.Item().Text($"{fromDate:dd MMM yyyy} to {toDate:dd MMM yyyy}").FontSize(9);
                            right.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Brand);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text("Financial Summary").Bold().FontSize(13).FontColor(BrandDark);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        void Metric(string title, string value, string color)
                        {
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(m =>
                            {
                                m.Item().Text(title).FontSize(8).FontColor(Colors.Grey.Darken1);
                                m.Item().Text(value).Bold().FontSize(12).FontColor(color);
                            });
                        }

                        Metric("Collected", $"₹{summary.CollectedAmount:N0}", Colors.Green.Darken2);
                        Metric("Expenses", $"₹{summary.TotalExpenses:N0}", Colors.Red.Medium);
                        Metric("Profit", $"₹{summary.Profit:N0}", BrandDark);
                        Metric("Balance", $"₹{summary.Balance:N0}", Brand);
                    });

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Bookings: {summary.TotalBookings}").FontSize(9);
                        r.RelativeItem().Text($"Booking Value: ₹{summary.TotalRevenue:N0}").FontSize(9);
                        r.RelativeItem().Text($"Pending: ₹{summary.PendingAmount:N0}").FontSize(9);
                        r.RelativeItem().Text($"Overdue: {summary.OverdueCount}").FontSize(9);
                    });

                    col.Item().PaddingTop(8).Text("Bookings Detail").Bold().FontSize(12).FontColor(BrandDark);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(70);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.ConstantColumn(70);
                            c.ConstantColumn(65);
                            c.ConstantColumn(65);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in new[] { "Booking", "Customer", "Items", "Date", "Amount", "Status" })
                            {
                                h.Cell().Background(BrandDark).Padding(5)
                                    .Text(title).FontColor(Colors.White).FontSize(8).Bold();
                            }
                        });

                        foreach (var b in bookings)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.BookingNumber).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.CustomerName).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.ItemsSummary).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.BookingDateText).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.AmountText).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.StatusText).FontSize(8);
                        }

                        if (bookings.Count == 0)
                        {
                            table.Cell().ColumnSpan(6).Padding(8).Text("No bookings in selected date range.").FontSize(9).Italic();
                        }
                    });

                    col.Item().PaddingTop(10).Text("Expenses Detail").Bold().FontSize(12).FontColor(BrandDark);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.ConstantColumn(70);
                            c.ConstantColumn(70);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in new[] { "Title", "Category", "Date", "Amount", "Notes" })
                            {
                                h.Cell().Background(Colors.BlueGrey.Darken2).Padding(5)
                                    .Text(title).FontColor(Colors.White).FontSize(8).Bold();
                            }
                        });

                        foreach (var e in expenses)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Title).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Category).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.ExpenseDateText).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.AmountText).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Notes).FontSize(8);
                        }

                        if (expenses.Count == 0)
                        {
                            table.Cell().ColumnSpan(5).Padding(8).Text("No expenses in selected date range.").FontSize(9).Italic();
                        }
                    });
                });

                page.Footer().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    f.Item().PaddingTop(6).Row(r =>
                    {
                        r.RelativeItem().Text(company.ReportFooter).FontSize(8).FontColor(Colors.Grey.Darken1);
                        r.ConstantItem(120).AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(8);
                            text.CurrentPageNumber().FontSize(8);
                            text.Span(" / ").FontSize(8);
                            text.TotalPages().FontSize(8);
                        });
                    });
                    f.Item().AlignCenter().Text("Powered by RentalX").FontSize(8).FontColor(Brand);
                });
            });
        }).GeneratePdf(filePath);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch
        {
            // File still saved even if open fails.
        }

        return filePath;
    }
}

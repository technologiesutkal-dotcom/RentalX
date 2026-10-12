using System.Diagnostics;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentalX.Helpers;
using RentalX.Models;

namespace RentalX.Services;

public class InvoicePdfService : IInvoicePdfService
{
    private static readonly Color Brand = Color.FromHex("#0F766E");
    private static readonly Color BrandDark = Color.FromHex("#004D40");

    public InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string GenerateInvoice(CompanySettings company, Booking booking)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RentalX",
            "Invoices");
        Directory.CreateDirectory(folder);

        var invoiceNo = booking.InvoiceNumber ?? booking.BookingNumber;
        var filePath = Path.Combine(folder, $"{invoiceNo}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.BlueGrey.Darken3));

                page.Background().AlignCenter().AlignMiddle()
                    .Text("RentalX")
                    .FontSize(70)
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
                            left.Item().Text($"Owner: {company.OwnerName}");
                            left.Item().Text($"{company.Address}, {company.City}").FontSize(9);
                            left.Item().Text($"Phone: {company.Phone}").FontSize(9);
                            if (company.ShowGstOnReports && !string.IsNullOrWhiteSpace(company.GstNumber))
                            {
                                left.Item().Text($"GST: {company.GstNumber}").FontSize(9);
                            }
                        });
                        row.ConstantItem(180).AlignRight().Column(right =>
                        {
                            right.Item().Text("INVOICE").Bold().FontSize(16).FontColor(Brand);
                            right.Item().Text($"Invoice #: {invoiceNo}").FontSize(10);
                            right.Item().Text($"Booking #: {booking.BookingNumber}").FontSize(10);
                            right.Item().Text($"Date: {DateTime.Today:dd MMM yyyy}").FontSize(9);
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Brand);
                });

                page.Content().PaddingVertical(18).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("Bill To").Bold().FontSize(11);
                    col.Item().Text(booking.CustomerName).FontSize(12).Bold();
                    col.Item().Text($"Status: {booking.StatusText}").FontSize(9);

                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.ConstantColumn(60);
                            c.ConstantColumn(70);
                            c.ConstantColumn(50);
                            c.ConstantColumn(80);
                        });

                        table.Header(h =>
                        {
                            foreach (var t in new[] { "Item", "Qty", "Rate/Day", "Days", "Amount" })
                            {
                                h.Cell().Background(BrandDark).Padding(6)
                                    .Text(t).FontColor(Colors.White).SemiBold().FontSize(9);
                            }
                        });

                        if (booking.Lines is { Count: > 0 })
                        {
                            foreach (var line in booking.Lines)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(line.ItemName).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(line.Quantity.ToString()).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"₹{line.DailyRate:N0}").FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(booking.RentalDays.ToString()).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"₹{line.LineAmount:N0}").FontSize(9);
                            }
                        }
                        else
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(booking.ItemsSummary).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(booking.Quantity.ToString()).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text($"₹{booking.DailyRate:N0}").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(booking.RentalDays.ToString()).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(booking.AmountText).FontSize(9).Bold();
                        }
                    });

                    if (booking.AdjustmentAmount != 0 || booking.LateDays > 0 || booking.EarlyDays > 0)
                    {
                        col.Item().PaddingTop(8).Text("Settlement").Bold().FontSize(11);
                        col.Item().Text($"Expected return: {booking.ExpectedReturnText}").FontSize(9);
                        col.Item().Text($"Actual return: {booking.ActualReturnText}").FontSize(9);
                        if (booking.LateDays > 0)
                        {
                            col.Item().Text($"Late days: {booking.LateDays}").FontSize(9);
                        }
                        if (booking.EarlyDays > 0)
                        {
                            col.Item().Text($"Early days: {booking.EarlyDays}").FontSize(9);
                        }
                        col.Item().Text($"Original: ₹{booking.OriginalAmount:N0}").FontSize(9);
                        col.Item().Text($"Adjustment: ₹{booking.AdjustmentAmount:N0}").FontSize(9);
                        if (!string.IsNullOrWhiteSpace(booking.SettlementNote))
                        {
                            col.Item().Text(booking.SettlementNote).FontSize(9).Italic();
                        }
                    }

                    col.Item().AlignRight().PaddingTop(12).Column(sum =>
                    {
                        sum.Item().Text($"Final total: {booking.AmountText}").FontSize(12);
                        sum.Item().Text($"Paid: {booking.PaidText}").FontSize(11);
                        sum.Item().Text($"Balance: {booking.BalanceText}")
                            .FontSize(14).Bold().FontColor(BrandDark);
                    });

                    col.Item().PaddingTop(20).Text(company.ReportFooter).FontSize(9).Italic();
                });

                page.Footer().AlignCenter().Text("Generated by RentalX").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf(filePath);

        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch
        {
        }

        return filePath;
    }
}

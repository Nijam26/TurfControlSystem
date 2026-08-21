using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TurfControlSystem.Application.DTOs;

namespace TurfControlSystem.Application.Services;

/// <summary>Renders a <see cref="DailyReportDto"/> to a downloadable PDF, for manual record-keeping.</summary>
public class ReportPdfService
{
    static ReportPdfService()
    {
        // Community license is free — see QuestPDF's licensing page if this project ever
        // grows past a small team / low revenue, since the terms are size-based.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(DailyReportDto report)
    {
        var title = report.IsSingleDay
            ? $"Daily Report — {report.StartDate:dd MMM yyyy}"
            : $"Report — {report.StartDate:dd MMM yyyy} to {report.EndDate:dd MMM yyyy}";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Turf Control System").FontSize(18).Bold();
                    col.Item().Text(title).FontSize(12).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(2)
                        .Text($"Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Row(row =>
                    {
                        SummaryCard(row.RelativeItem(), "Total Bookings", report.TotalBookings.ToString());
                        SummaryCard(row.RelativeItem(), "Cancelled", report.CancelledBookings.ToString());
                        SummaryCard(row.RelativeItem(), "Total Revenue", $"BDT {report.TotalRevenue:N0}");
                        SummaryCard(row.RelativeItem(), "Due Payments", $"BDT {report.DuePayments:N0}");
                        SummaryCard(row.RelativeItem(), "Avg Duration", $"{report.AverageDurationMinutes:N0} min");
                    });

                    var columnCount = report.IsSingleDay ? 8 : 9;

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            if (!report.IsSingleDay) columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.3f);  // Turf
                            columns.RelativeColumn(1.4f);  // Team
                            columns.RelativeColumn(1f);    // Start
                            columns.RelativeColumn(1f);    // End
                            columns.RelativeColumn(1.1f);  // Status
                            columns.RelativeColumn(1f);    // Total
                            columns.RelativeColumn(1f);    // Paid
                            columns.RelativeColumn(1f);    // Due
                        });

                        table.Header(header =>
                        {
                            if (!report.IsSingleDay) header.Cell().Element(HeaderCell).Text("Date");
                            header.Cell().Element(HeaderCell).Text("Turf");
                            header.Cell().Element(HeaderCell).Text("Team");
                            header.Cell().Element(HeaderCell).Text("Start");
                            header.Cell().Element(HeaderCell).Text("End");
                            header.Cell().Element(HeaderCell).Text("Status");
                            header.Cell().Element(HeaderCell).Text("Total");
                            header.Cell().Element(HeaderCell).Text("Paid");
                            header.Cell().Element(HeaderCell).Text("Due");

                            static IContainer HeaderCell(IContainer c) => c
                                .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8))
                                .Background(Colors.Blue.Darken2)
                                .Padding(4);
                        });

                        foreach (var b in report.Bookings)
                        {
                            if (!report.IsSingleDay)
                                table.Cell().Element(BodyCell).Text(b.StartTime.ToString("dd MMM"));
                            table.Cell().Element(BodyCell).Text(b.Turf?.Name ?? "-");
                            table.Cell().Element(BodyCell).Text(b.Team?.Name ?? "-");
                            table.Cell().Element(BodyCell).Text(b.StartTime.ToString("hh:mm tt"));
                            table.Cell().Element(BodyCell).Text(b.EndTime.ToString("hh:mm tt"));
                            table.Cell().Element(BodyCell).Text(b.Status.ToString());
                            table.Cell().Element(BodyCell).Text($"{b.TotalAmount:N0}");
                            table.Cell().Element(BodyCell).Text($"{b.TotalPaid:N0}");
                            table.Cell().Element(BodyCell).Text($"{b.DueAmount:N0}");

                            static IContainer BodyCell(IContainer c) => c
                                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .Padding(4);
                        }

                        if (report.Bookings.Count == 0)
                        {
                            table.Cell().ColumnSpan((uint)columnCount)
                                .Padding(12).AlignCenter()
                                .Text("No bookings for this period.").FontColor(Colors.Grey.Medium);
                        }
                    });
                });

                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium)).Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void SummaryCard(IContainer container, string label, string value)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(2).Text(value).FontSize(13).Bold();
        });
    }
}

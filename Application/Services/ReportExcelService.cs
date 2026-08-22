using ClosedXML.Excel;
using TurfControlSystem.Application.DTOs;

namespace TurfControlSystem.Application.Services;

/// <summary>Renders a <see cref="DailyReportDto"/> to a downloadable .xlsx workbook — same data as the PDF export, in a format that's easy to filter/pivot in Excel.</summary>
public class ReportExcelService
{
    public byte[] GenerateExcel(DailyReportDto report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");

        var title = report.IsSingleDay
            ? $"Daily Report — {report.StartDate:dd MMM yyyy}"
            : $"Report — {report.StartDate:dd MMM yyyy} to {report.EndDate:dd MMM yyyy}";

        // ---- Title + generated-on stamp ----
        sheet.Cell(1, 1).Value = "Turf Control System";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;

        sheet.Cell(2, 1).Value = title;
        sheet.Cell(2, 1).Style.Font.FontSize = 11;
        sheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#4B5563");

        sheet.Cell(3, 1).Value = $"Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}";
        sheet.Cell(3, 1).Style.Font.FontSize = 9;
        sheet.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#9CA3AF");

        // ---- Summary cards, as a row of label/value pairs ----
        var summaryRow = 5;
        var summary = new (string Label, string Value)[]
        {
            ("Total Bookings", report.TotalBookings.ToString()),
            ("Cancelled", report.CancelledBookings.ToString()),
            ("Total Revenue", $"BDT {report.TotalRevenue:N0}"),
            ("Due Payments", $"BDT {report.DuePayments:N0}"),
            ("Avg Duration", $"{report.AverageDurationMinutes:N0} min"),
        };

        for (var i = 0; i < summary.Length; i++)
        {
            var col = i + 1;
            var labelCell = sheet.Cell(summaryRow, col);
            var valueCell = sheet.Cell(summaryRow + 1, col);

            labelCell.Value = summary[i].Label;
            labelCell.Style.Font.FontSize = 8;
            labelCell.Style.Font.FontColor = XLColor.FromHtml("#6B7280");

            valueCell.Value = summary[i].Value;
            valueCell.Style.Font.Bold = true;
            valueCell.Style.Font.FontSize = 12;
        }

        // ---- Bookings table ----
        var headerRow = summaryRow + 3;
        var col2 = 1;

        void Header(string text)
        {
            var cell = sheet.Cell(headerRow, col2);
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1D4ED8");
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            col2++;
        }

        if (!report.IsSingleDay) Header("Date");
        Header("Turf");
        Header("Team");
        Header("Start");
        Header("End");
        Header("Status");
        Header("Total");
        Header("Paid");
        Header("Due");

        var row = headerRow + 1;
        foreach (var b in report.Bookings)
        {
            var c = 1;
            if (!report.IsSingleDay) sheet.Cell(row, c++).Value = b.StartTime.ToString("dd MMM yyyy");
            sheet.Cell(row, c++).Value = b.Turf?.Name ?? "-";
            sheet.Cell(row, c++).Value = b.Team?.Name ?? "-";
            sheet.Cell(row, c++).Value = b.StartTime.ToString("hh:mm tt");
            sheet.Cell(row, c++).Value = b.EndTime.ToString("hh:mm tt");
            sheet.Cell(row, c++).Value = b.Status.ToString();
            sheet.Cell(row, c++).Value = b.TotalAmount;
            sheet.Cell(row, c++).Value = b.TotalPaid;
            sheet.Cell(row, c++).Value = b.DueAmount;
            row++;
        }

        if (report.Bookings.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No bookings for this period.";
            sheet.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#9CA3AF");
            row++;
        }

        var lastCol = report.IsSingleDay ? 8 : 9;
        var tableRange = sheet.Range(headerRow, 1, row - 1, lastCol);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D1D5DB");
        tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E5E7EB");

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Infrastructure.Service;

public class PdfCertificateService
{
    private const double Margin = 48;
    private const double LineHeight = 18;

    public byte[] Render(EnrollmentCertificateDto data)
    {
        using var document = new PdfDocument();
        document.Info.Title = "SkillHub - Constancia de inscripción";
        var regular = new XFont("DejaVu Sans", 11, XFontStyleEx.Regular);
        var heading = new XFont("DejaVu Sans", 22, XFontStyleEx.Bold);
        var subheading = new XFont("DejaVu Sans", 16, XFontStyleEx.Bold);
        PdfPage page = null!;
        XGraphics graphics = null!;
        double y = 0;
        void AddPage()
        {
            graphics?.Dispose();
            page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            graphics = XGraphics.FromPdfPage(page);
            graphics.DrawString("SkillHub", heading, XBrushes.DarkSlateBlue, new XPoint(Margin, 65));
            graphics.DrawString("Constancia de inscripción", subheading, XBrushes.Black, new XPoint(Margin, 105));
            y = 140;
        }
        AddPage();
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            string Local(DateTimeOffset value)
            {
                return TimeZoneInfo.ConvertTime(value, zone).ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture);
            }

            var fields = new[] { "This certifies that the following participant has an active workshop enrollment.",
                $"Participant: {data.ParticipantName}", $"Email: {data.ParticipantEmail}", $"Workshop code: {data.WorkshopCode}",
                $"Workshop title: {data.WorkshopTitle}", $"Instructor: {data.InstructorName}", $"Start: {Local(data.StartTime)}",
                $"End: {Local(data.EndTime)}", "Workshop time zone: America/Bogota (UTC-05:00)",
                $"Enrollment date: {Local(data.EnrolledAt)}", $"Document generated: {Local(data.GeneratedAt)}" };
            foreach (var field in fields)
            {
                // Measure real glyph widths, split long tokens, and paginate instead of clipping required values.
                var lines = Wrap(field, graphics, regular, page.Width.Point - 2 * Margin).ToList();
                foreach (var line in lines)
                {
                    if (y + LineHeight > page.Height.Point - Margin)
                    {
                        AddPage();
                    }

                    graphics.DrawString(line, regular, XBrushes.Black,
                        new XRect(Margin, y, page.Width.Point - 2 * Margin, LineHeight), XStringFormats.TopLeft);
                    y += LineHeight;
                }
                y += 12;
            }
        }
        finally { graphics.Dispose(); }
        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static IEnumerable<string> Wrap(string text, XGraphics graphics, XFont font, double width)
    {
        foreach (var paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = string.Empty;
            var elements = StringInfo.GetTextElementEnumerator(paragraph);
            while (elements.MoveNext())
            {
                var element = elements.GetTextElement();
                if (line.Length > 0 && graphics.MeasureString(line + element, font).Width > width)
                {
                    var space = line.LastIndexOf(' ');
                    if (space > 0)
                    {
                        yield return line[..space];
                        line = line[(space + 1)..];
                    }
                    else
                    {
                        yield return line;
                        line = string.Empty;
                    }
                }
                line += element;
            }
            yield return line;
        }
    }
}

using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Infrastructure.Service;

namespace SkillHub.Infrastructure.Persistence.Repository.External;

/// <summary>Adapts the workbook service to the application import contract.</summary>
public class ExcelRepository : IExcelService
{
    private readonly ExcelWorkbookService _workbookService;

    public ExcelRepository(
        ExcelWorkbookService workbookService)
    {
        _workbookService = workbookService;
    }

    public IReadOnlyList<EnrollmentImportRowDto> ReadEnrollments(Stream file)
    {
        using var workbook = _workbookService.Open(file);
        var sheet = workbook.Worksheets.FirstOrDefault(s => s.Name.Equals("Enrollments", StringComparison.OrdinalIgnoreCase))
            ?? (workbook.Worksheets.Count == 1 ? workbook.Worksheet(1) : null)
            ?? throw new ValidationException("The workbook must contain an Enrollments worksheet.");
        var lastColumn = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        var emailColumn = 0;
        var codeColumn = 0;
        for (var c = 1; c <= lastColumn; c++)
        {
            if (sheet.Cell(1, c).HasFormula)
            {
                throw new ValidationException("Excel headers must be plain text, not formulas.");
            }

            var name = sheet.Cell(1, c).GetString().Trim();
            if (name.Equals("ParticipantEmail", StringComparison.OrdinalIgnoreCase))
            {
                emailColumn = c;
            }

            if (name.Equals("WorkshopCode", StringComparison.OrdinalIgnoreCase))
            {
                codeColumn = c;
            }
        }
        if (emailColumn == 0 || codeColumn == 0)
        {
            throw new ValidationException("The header row must contain ParticipantEmail and WorkshopCode.");
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow > 5001)
        {
            throw new ValidationException("Import at most 5000 data rows at a time.");
        }

        var rows = new List<EnrollmentImportRowDto>();
        for (var row = 2; row <= lastRow; row++)
        {
            // Never evaluate uploaded formulas: circular or unsupported formulas could abort unrelated rows.
            if (sheet.Cell(row, emailColumn).HasFormula || sheet.Cell(row, codeColumn).HasFormula)
            {
                rows.Add(new EnrollmentImportRowDto
                {
                    RowNumber = row,
                    RejectionReason = "Formula cells are not supported; provide plain text values."
                });
                continue;
            }
            var email = sheet.Cell(row, emailColumn).GetString().Trim();
            var code = sheet.Cell(row, codeColumn).GetString().Trim();
            if (email.Length == 0 && code.Length == 0)
            {
                continue;
            }

            EnrollmentImportRowDto rowDto = new EnrollmentImportRowDto
            {
                RowNumber = row,
                ParticipantEmail = email,
                WorkshopCode = code
            };

            rows.Add(rowDto);
        }
        if (rows.Count == 0)
        {
            throw new ValidationException("The worksheet contains no enrollment rows.");
        }

        return rows;
    }
}

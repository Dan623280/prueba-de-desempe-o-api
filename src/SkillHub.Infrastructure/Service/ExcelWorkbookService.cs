using System.IO.Compression;
using ClosedXML.Excel;
using SkillHub.Application.Exceptions;

namespace SkillHub.Infrastructure.Service;

public class ExcelWorkbookService
{
    public XLWorkbook Open(Stream file)
    {
        try
        {
            if (!file.CanSeek)
            {
                throw new ValidationException("The Excel upload must be seekable.");
            }

            if (file.Length == 0 || file.Length > 5 * 1024 * 1024)
            {
                throw new ValidationException("Upload a non-empty .xlsx file no larger than 5 MB.");
            }

            using (var zip = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: true))
            {
                if (zip.Entries.Sum(e => e.Length) > 30 * 1024 * 1024)
                {
                    throw new ValidationException("The expanded workbook exceeds 30 MB.");
                }
            }
            file.Position = 0;
            return new XLWorkbook(file);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            throw new ValidationException("The file is not a valid supported .xlsx workbook.");
        }
    }
}

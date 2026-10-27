using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Services;

public interface IExcelService
{
    IReadOnlyList<EnrollmentImportRowDto> ReadEnrollments(Stream file);
}

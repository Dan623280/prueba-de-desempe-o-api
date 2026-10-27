using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Services;

public interface IEnrollmentService
{
    Task<EnrollmentDto> EnrollAsync(Guid workshopId, CancellationToken ct);
    Task<IReadOnlyList<EnrollmentDto>> GetMineAsync(CancellationToken ct);
    Task<EnrollmentDto> CancelAsync(Guid id, CancellationToken ct);
    Task<EnrollmentImportResultDto> ImportAsync(Stream file, CancellationToken ct);
    Task<CertificateFileDto> GetCertificateAsync(Guid id, CancellationToken ct);
}

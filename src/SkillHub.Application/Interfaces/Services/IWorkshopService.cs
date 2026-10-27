using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Services;

public interface IWorkshopService
{
    Task<IReadOnlyList<WorkshopDto>> GetAvailableAsync(CancellationToken ct);
    Task<WorkshopDto> GetAsync(Guid id, CancellationToken ct);
    Task<WorkshopDto> CreateAsync(CreateWorkshopDto request, CancellationToken ct);
    Task<IReadOnlyList<EnrollmentDto>> GetEnrollmentsAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<InstructorDto>> GetInstructorsAsync(CancellationToken ct);
}

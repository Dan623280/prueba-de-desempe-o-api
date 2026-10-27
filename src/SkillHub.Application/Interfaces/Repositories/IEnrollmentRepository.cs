using SkillHub.Domain.Entities;
using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Repositories;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Workshop>> GetActiveWorkshopsAsync(Guid participantId, CancellationToken ct);
    Task<int> CountActiveAsync(Guid workshopId, CancellationToken ct);
    Task AddAsync(Enrollment enrollment, CancellationToken ct);
    Task UpdateAsync(Enrollment enrollment, CancellationToken ct);
    Task<EnrollmentDto?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<EnrollmentDto>> GetMineAsync(Guid participantId, CancellationToken ct);
    Task<IReadOnlyList<EnrollmentDto>> GetByWorkshopAsync(Guid workshopId, CancellationToken ct);
}

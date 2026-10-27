using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.DTOs.Reports;
using SkillHub.Domain.Entities;

namespace SkillHub.Application.Interfaces.Repositories;

public interface IWorkshopRepository
{
    Task<IReadOnlyList<WorkshopSnapshotDto>> GetAvailableAsync(DateTimeOffset now, CancellationToken ct);
    Task<WorkshopSnapshotDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Workshop?> GetByCodeAsync(string code, CancellationToken ct);
    Task<Instructor?> GetInstructorByEmailAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<Instructor>> GetInstructorsAsync(CancellationToken ct);
    Task AddAsync(Workshop workshop, CancellationToken ct);
    Task<IReadOnlyList<TopWorkshopDto>> GetTopAsync(CancellationToken ct);
}

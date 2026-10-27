using SkillHub.Application.DTOs.Reports;

namespace SkillHub.Application.Interfaces.Services;

public interface IReportService
{
    Task<IReadOnlyList<TopWorkshopDto>> GetTopWorkshopsAsync(CancellationToken ct);
}

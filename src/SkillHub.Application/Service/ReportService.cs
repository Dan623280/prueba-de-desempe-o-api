using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Application.Service;

public class ReportService : IReportService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly ICurrentUser _currentUser;

    public ReportService(IWorkshopRepository workshopRepository, ICurrentUser currentUser)
    {
        _workshopRepository = workshopRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TopWorkshopDto>> GetTopWorkshopsAsync(CancellationToken ct)
    {
        if (!_currentUser.IsEmployee)
        {
            throw new ForbiddenException();
        }

        var workshops = await _workshopRepository.GetTopAsync(ct);
        return workshops;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Api.Controllers;

[ApiController]
[Authorize(Roles = "Employee")]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("top-workshops")]
    public async Task<ActionResult<IReadOnlyList<TopWorkshopDto>>> GetTopWorkshops(CancellationToken ct)
    {
        var workshops = await _reportService.GetTopWorkshopsAsync(ct);
        return Ok(workshops);
    }
}

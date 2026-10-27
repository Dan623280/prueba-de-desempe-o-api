using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/workshops")]
public class WorkshopsController : ControllerBase
{
    private readonly IWorkshopService _workshopService;

    public WorkshopsController(IWorkshopService workshopService)
    {
        _workshopService = workshopService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<WorkshopDto>>> GetAvailable(CancellationToken ct)
    {
        var workshops = await _workshopService.GetAvailableAsync(ct);
        return Ok(workshops);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<WorkshopDto>> Get(Guid id, CancellationToken ct)
    {
        var workshop = await _workshopService.GetAsync(id, ct);
        return Ok(workshop);
    }

    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<WorkshopDto>> Create(CreateWorkshopDto dto, CancellationToken ct)
    {
        var workshop = await _workshopService.CreateAsync(dto, ct);

        return CreatedAtAction(
            nameof(Get),
            new
            {
                id = workshop.Id
            },
            workshop);
    }

    [HttpGet("{id:guid}/enrollments")]
    [Authorize(Roles = "Employee")]
    public async Task<ActionResult<IReadOnlyList<EnrollmentDto>>> GetEnrollments(Guid id, CancellationToken ct)
    {
        var enrollments = await _workshopService.GetEnrollmentsAsync(id, ct);
        return Ok(enrollments);
    }
}

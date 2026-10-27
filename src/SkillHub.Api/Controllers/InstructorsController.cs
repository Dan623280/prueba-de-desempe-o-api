using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Api.Controllers;

[ApiController]
[Authorize(Roles = "Employee")]
[Route("api/v1/instructors")]
public class InstructorsController : ControllerBase
{
    private readonly IWorkshopService _workshopService;

    public InstructorsController(IWorkshopService workshopService)
    {
        _workshopService = workshopService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InstructorDto>>> Get(CancellationToken ct)
    {
        var instructors = await _workshopService.GetInstructorsAsync(ct);
        return Ok(instructors);
    }
}

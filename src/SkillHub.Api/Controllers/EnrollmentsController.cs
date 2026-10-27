using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpPost]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<EnrollmentDto>> Enroll(CreateEnrollmentDto dto, CancellationToken ct)
    {
        var enrollment = await _enrollmentService.EnrollAsync(dto.WorkshopId, ct);
        return StatusCode(StatusCodes.Status201Created, enrollment);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<IReadOnlyList<EnrollmentDto>>> GetMine(CancellationToken ct)
    {
        var enrollments = await _enrollmentService.GetMineAsync(ct);
        return Ok(enrollments);
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<ActionResult<EnrollmentDto>> Cancel(Guid id, CancellationToken ct)
    {
        var enrollment = await _enrollmentService.CancelAsync(id, ct);
        return Ok(enrollment);
    }

    // Every Excel row follows the same enrollment rules as an individual request.
    [HttpPost("import")]
    [Authorize(Roles = "Employee")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<EnrollmentImportResultDto>> Import([FromForm] EnrollmentImportRequest request, CancellationToken ct)
    {
        await using var file = request.File.OpenReadStream();
        var result = await _enrollmentService.ImportAsync(file, ct);

        return Ok(result);
    }

    [HttpGet("{id:guid}/certificate")]
    public async Task<IActionResult> GetCertificate(Guid id, CancellationToken ct)
    {
        var certificate = await _enrollmentService.GetCertificateAsync(id, ct);
        return File(certificate.Content, "application/pdf", certificate.FileName);
    }
}

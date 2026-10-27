using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SkillHub.Api.Controllers;

public class EnrollmentImportRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}

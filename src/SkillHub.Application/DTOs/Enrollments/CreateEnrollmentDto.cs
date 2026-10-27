using System.ComponentModel.DataAnnotations;

namespace SkillHub.Application.DTOs.Enrollments;

public class CreateEnrollmentDto
{
    [Required]
    public Guid WorkshopId { get; set; }
}

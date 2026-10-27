using System.ComponentModel.DataAnnotations;

namespace SkillHub.Application.DTOs.Workshops;

public class CreateWorkshopDto
{
    [Required(ErrorMessage = "Code is required."), StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required."), StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required."), StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Instructor email is required."), EmailAddress, StringLength(254)]
    public string InstructorEmail { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}

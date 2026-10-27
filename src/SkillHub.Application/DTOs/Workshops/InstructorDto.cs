namespace SkillHub.Application.DTOs.Workshops;

public class InstructorDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Specialty { get; set; } = string.Empty;
}

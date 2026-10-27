namespace SkillHub.Application.DTOs.Workshops;

public class WorkshopDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public InstructorDto Instructor { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public int Capacity { get; set; }

    public int ActiveEnrollments { get; set; }

    public int AvailableSeats { get; set; }
}

namespace SkillHub.Application.DTOs.Enrollments;

public class EnrollmentCertificateDto
{
    public string ParticipantName { get; set; } = string.Empty;

    public string ParticipantEmail { get; set; } = string.Empty;

    public string WorkshopCode { get; set; } = string.Empty;

    public string WorkshopTitle { get; set; } = string.Empty;

    public string InstructorName { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public DateTimeOffset EnrolledAt { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }
}

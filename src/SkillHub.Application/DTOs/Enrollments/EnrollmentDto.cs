using SkillHub.Application.DTOs.Workshops;

namespace SkillHub.Application.DTOs.Enrollments;

public class EnrollmentDto
{
    public Guid Id { get; set; }

    public Guid ParticipantId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public string ParticipantEmail { get; set; } = string.Empty;

    public WorkshopDto Workshop { get; set; } = null!;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset EnrolledAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public Guid? CancelledById { get; set; }
}

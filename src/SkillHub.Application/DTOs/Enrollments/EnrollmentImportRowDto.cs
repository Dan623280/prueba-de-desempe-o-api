namespace SkillHub.Application.DTOs.Enrollments;

public class EnrollmentImportRowDto
{
    public int RowNumber { get; set; }

    public string ParticipantEmail { get; set; } = string.Empty;

    public string WorkshopCode { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }
}

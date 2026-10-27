namespace SkillHub.Application.DTOs.Enrollments;

public class RejectedEnrollmentRowDto
{
    public int RowNumber { get; set; }

    public string Reason { get; set; } = string.Empty;
}

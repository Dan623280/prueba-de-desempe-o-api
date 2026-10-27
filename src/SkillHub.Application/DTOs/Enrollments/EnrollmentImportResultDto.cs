namespace SkillHub.Application.DTOs.Enrollments;

public class EnrollmentImportResultDto
{
    public int TotalRows { get; set; }

    public int EnrolledRows { get; set; }

    public int RejectedRows { get; set; }

    public IReadOnlyList<RejectedEnrollmentRowDto> Rejections { get; set; } = new List<RejectedEnrollmentRowDto>();
}

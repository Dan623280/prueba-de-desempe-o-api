namespace SkillHub.Application.DTOs.Reports;

public class TopWorkshopDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int ActiveEnrollments { get; set; }

    public int Capacity { get; set; }

    public int AvailableSeats { get; set; }
}

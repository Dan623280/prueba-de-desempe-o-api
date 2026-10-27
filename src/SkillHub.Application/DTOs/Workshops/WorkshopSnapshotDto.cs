using SkillHub.Domain.Entities;

namespace SkillHub.Application.DTOs.Workshops;

public class WorkshopSnapshotDto
{
    public Workshop Workshop { get; set; } = null!;

    public int ActiveCount { get; set; }
}

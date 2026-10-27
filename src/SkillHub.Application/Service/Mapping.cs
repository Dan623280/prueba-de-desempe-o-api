using SkillHub.Application.DTOs.Workshops;

namespace SkillHub.Application.Service;

public static class Mapping
{
    // Reuse this mapping when the application needs a workshop with its calculated seats.
    public static WorkshopDto ToDto(this WorkshopSnapshotDto snapshot)
    {
        var workshop = snapshot.Workshop;
        var instructor = workshop.Instructor;

        InstructorDto instructorDto = new InstructorDto
        {
            Id = instructor.Id,
            FullName = instructor.FullName,
            Email = instructor.Email,
            Specialty = instructor.Specialty
        };

        WorkshopDto responseDto = new WorkshopDto
        {
            Id = workshop.Id,
            Code = workshop.Code,
            Title = workshop.Title,
            Description = workshop.Description,
            Instructor = instructorDto,
            StartTime = workshop.StartTime,
            EndTime = workshop.EndTime,
            Capacity = workshop.Capacity,
            ActiveEnrollments = snapshot.ActiveCount,
            AvailableSeats = workshop.Capacity - snapshot.ActiveCount
        };

        return responseDto;
    }
}

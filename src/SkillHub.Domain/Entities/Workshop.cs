using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Workshop
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid InstructorId { get; private set; }
    public Instructor Instructor { get; private set; } = null!;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public int Capacity { get; private set; }

    private Workshop()
    {
    }

    public static Workshop Schedule(
        string code, string title, string description, Guid instructorId,
        DateTimeOffset startTime, DateTimeOffset endTime, int capacity, DateTimeOffset now)
    {
        if (startTime <= now)
        {
            throw new BusinessRuleException("The workshop must start in the future.");
        }

        var workshop = ImportPublished(code, title, description, instructorId, startTime, endTime, capacity);
        return workshop;
    }

    // Seed data may contain historical workshops. New API workshops always use Schedule.
    public static Workshop ImportPublished(
        string code, string title, string description, Guid instructorId,
        DateTimeOffset startTime, DateTimeOffset endTime, int capacity)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 30)
        {
            throw new BusinessRuleException("Workshop code is required and must not exceed 30 characters.");
        }

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
        {
            throw new BusinessRuleException("Workshop title is required and must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 4000)
        {
            throw new BusinessRuleException("Workshop description is required and must not exceed 4000 characters.");
        }

        if (instructorId == Guid.Empty)
        {
            throw new BusinessRuleException("A registered instructor is required.");
        }

        if (endTime <= startTime)
        {
            throw new BusinessRuleException("The end time must be after the start time.");
        }

        if (capacity <= 0)
        {
            throw new BusinessRuleException("Workshop capacity must be greater than zero.");
        }

        Workshop workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Title = title.Trim(),
            Description = description.Trim(),
            InstructorId = instructorId,
            StartTime = startTime.ToUniversalTime(),
            EndTime = endTime.ToUniversalTime(),
            Capacity = capacity
        };

        return workshop;
    }
}

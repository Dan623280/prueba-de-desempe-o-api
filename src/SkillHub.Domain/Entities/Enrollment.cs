using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; private set; }
    public Guid ParticipantId { get; private set; }
    public Guid WorkshopId { get; private set; }
    public Workshop Workshop { get; private set; } = null!;
    public EnrollmentStatus Status { get; private set; }
    public DateTimeOffset EnrolledAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledById { get; private set; }

    private Enrollment()
    {
    }

    public static Enrollment Create(Guid participantId, Guid workshopId, DateTimeOffset now)
    {
        if (participantId == Guid.Empty || workshopId == Guid.Empty)
        {
            throw new BusinessRuleException("A participant and workshop are required.");
        }

        Enrollment enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            ParticipantId = participantId,
            WorkshopId = workshopId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = now.ToUniversalTime()
        };

        return enrollment;
    }

    public void Cancel(Guid actorId, bool isEmployee, DateTimeOffset workshopStart, DateTimeOffset now)
    {
        if (actorId == Guid.Empty)
        {
            throw new BusinessRuleException("The cancellation actor is required.");
        }

        if (Status != EnrollmentStatus.Active)
        {
            throw new BusinessRuleException("The enrollment is already cancelled.");
        }

        if (workshopStart <= now)
        {
            throw new BusinessRuleException("A started workshop enrollment cannot be cancelled.");
        }

        if (!isEmployee && workshopStart - now < TimeSpan.FromHours(24))
        {
            throw new BusinessRuleException("Participants must cancel at least 24 hours before the workshop starts.");
        }

        // Keep the actor and timestamp so cancellations can be audited.
        Status = EnrollmentStatus.Cancelled;
        CancelledById = actorId;
        CancelledAt = now.ToUniversalTime();
    }
}

using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Rules;

public static class EnrollmentPolicy
{
    public static void EnsureCanEnroll(
        Workshop workshop, int activeCount,
        IReadOnlyCollection<Workshop> participantActiveWorkshops, DateTimeOffset now)
    {
        if (workshop.StartTime <= now)
        {
            throw new BusinessRuleException("Enrollment is only available for future workshops.");
        }

        var alreadyEnrolled = participantActiveWorkshops.Any(activeWorkshop => activeWorkshop.Id == workshop.Id);

        if (alreadyEnrolled)
        {
            throw new BusinessRuleException("The participant already has an active enrollment in this workshop.");
        }

        if (activeCount >= workshop.Capacity)
        {
            throw new BusinessRuleException("The workshop has no available seats.");
        }

        // A workshop can start exactly when another one ends.
        var scheduleOverlaps = participantActiveWorkshops.Any(activeWorkshop =>
            activeWorkshop.StartTime < workshop.EndTime && workshop.StartTime < activeWorkshop.EndTime);

        if (scheduleOverlaps)
        {
            throw new BusinessRuleException("The workshop overlaps another active enrollment for this participant.");
        }
    }
}

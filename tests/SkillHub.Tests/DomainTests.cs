using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.Rules;
using SkillHub.Domain.Enums;
using Xunit;

namespace SkillHub.Tests;

public class DomainTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static Workshop WorkshopAt(int hours = 48, int capacity = 2)
    {
        return Workshop.Schedule(
        "FOT-001", "Photography", "Learn composition.", Guid.NewGuid(), Now.AddHours(hours), Now.AddHours(hours + 2), capacity, Now);
    }

    [Fact]
    public void WorkshopMustStartInTheFuture()
    {
        Assert.Throws<BusinessRuleException>(() => WorkshopAt(0));
    }

    [Fact]
    public void WorkshopCapacityMustBePositive()
    {
        Assert.Throws<BusinessRuleException>(() => WorkshopAt(capacity: 0));
    }

    [Fact]
    public void WorkshopEndMustFollowStart()
    {
        Assert.Throws<BusinessRuleException>(() => Workshop.Schedule(
        "CODE", "Title", "Description", Guid.NewGuid(), Now.AddHours(2), Now.AddHours(1), 3, Now));
    }

    [Fact]
    public void WorkshopRequiresAnInstructor()
    {
        Assert.Throws<BusinessRuleException>(() => Workshop.Schedule(
        "CODE", "Title", "Description", Guid.Empty, Now.AddHours(2), Now.AddHours(3), 3, Now));
    }

    [Fact]
    public void FullWorkshopRejectsEnrollment()
    {
        Assert.Throws<BusinessRuleException>(() => EnrollmentPolicy.EnsureCanEnroll(WorkshopAt(), 2, [], Now));
    }

    [Fact]
    public void StartedWorkshopRejectsEnrollment()
    {
        Assert.Throws<BusinessRuleException>(() => EnrollmentPolicy.EnsureCanEnroll(WorkshopAt(), 0, [], Now.AddHours(48)));
    }

    [Fact]
    public void ActiveDuplicateIsRejected()
    {
        var workshop = WorkshopAt();
        Assert.Throws<BusinessRuleException>(() => EnrollmentPolicy.EnsureCanEnroll(workshop, 0, [workshop], Now));
    }
    [Fact]
    public void OverlappingWorkshopIsRejected()
    {
        Assert.Throws<BusinessRuleException>(() => EnrollmentPolicy.EnsureCanEnroll(WorkshopAt(49), 0, [WorkshopAt(48)], Now));
    }

    [Fact]
    public void AdjacentWorkshopsAreAllowed()
    {
        EnrollmentPolicy.EnsureCanEnroll(WorkshopAt(50), 0, [WorkshopAt(48)], Now);
    }

    [Fact]
    public void WorkshopContainingAnotherWorkshopOverlaps()
    {
        Assert.Throws<BusinessRuleException>(() => EnrollmentPolicy.EnsureCanEnroll(WorkshopAt(49), 0, [WorkshopAt(49)], Now));
    }

    [Fact]
    public void NewEnrollmentIsActive()
    {
        Assert.Equal(EnrollmentStatus.Active, Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now).Status);
    }

    [Fact]
    public void CancellationAtExactly24HoursIsAllowed()
    {
        var e = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        var actor = Guid.NewGuid();
        e.Cancel(actor, false, WorkshopAt().StartTime, Now.AddHours(24));
        Assert.Equal(EnrollmentStatus.Cancelled, e.Status);
        Assert.Equal(actor, e.CancelledById);
        Assert.Equal(Now.AddHours(24), e.CancelledAt);
    }
    [Fact]
    public void ParticipantCannotCancelWithin24Hours()
    {
        Assert.Throws<BusinessRuleException>(() =>
        Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now).Cancel(Guid.NewGuid(), false, WorkshopAt().StartTime, Now.AddHours(24).AddTicks(1)));
    }

    [Fact]
    public void EmployeeCanCancelWithin24Hours()
    {
        Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now).Cancel(Guid.NewGuid(), true, WorkshopAt().StartTime, Now.AddHours(47));
    }

    [Fact]
    public void EmployeeCannotCancelStartedWorkshop()
    {
        Assert.Throws<BusinessRuleException>(() =>
        Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now).Cancel(Guid.NewGuid(), true, WorkshopAt().StartTime, Now.AddHours(48)));
    }

    [Fact]
    public void CancellationCannotBeRepeated()
    {
        var e = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        e.Cancel(Guid.NewGuid(), false, WorkshopAt().StartTime, Now);
        Assert.Throws<BusinessRuleException>(() => e.Cancel(Guid.NewGuid(), false, WorkshopAt().StartTime, Now));
    }
    [Fact]
    public void ReenrollmentCreatesANewActiveRecordAfterCancellation()
    {
        var e = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        e.Cancel(Guid.NewGuid(), false, WorkshopAt().StartTime, Now);
        var next = Enrollment.Create(e.ParticipantId, e.WorkshopId, Now.AddMinutes(1));
        Assert.NotEqual(e.Id, next.Id);
        Assert.Equal(EnrollmentStatus.Active, next.Status);
        EnrollmentPolicy.EnsureCanEnroll(WorkshopAt(), 0, [], Now);
    }
}


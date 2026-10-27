namespace SkillHub.Application.Interfaces.Repositories;

public interface IEnrollmentTransaction
{
    // Serializes enrollment changes for both the participant and the workshop across API instances.
    Task<T> ExecuteAsync<T>(Guid participantId, Guid workshopId, Func<CancellationToken, Task<T>> action, CancellationToken ct);
}

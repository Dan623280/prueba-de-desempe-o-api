namespace SkillHub.Application.Interfaces.Services;

public interface ICurrentUser
{
    Guid UserId
    {
        get;
    }
    bool IsEmployee
    {
        get;
    }
    bool IsParticipant
    {
        get;
    }
}

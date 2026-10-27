namespace SkillHub.Domain.Exceptions;

/// <summary>Signals a violation of a workshop or enrollment invariant.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

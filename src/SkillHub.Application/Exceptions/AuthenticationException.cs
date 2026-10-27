namespace SkillHub.Application.Exceptions;

public class AuthenticationException : Exception
{
    public AuthenticationException(string message = "Invalid email or password.") : base(message)
    {
    }
}

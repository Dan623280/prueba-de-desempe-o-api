using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Instructor
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;

    // EF Core uses this constructor when reading an existing instructor.
    private Instructor()
    {
    }

    public Instructor(Guid id, string fullName, string email, string specialty)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(specialty))
        {
            throw new BusinessRuleException("An instructor requires an identity, full name, email and specialty.");
        }

        Id = id;
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Specialty = specialty.Trim();
    }
}

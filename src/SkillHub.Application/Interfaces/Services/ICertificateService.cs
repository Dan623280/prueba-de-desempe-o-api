using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.Services;

public interface ICertificateService
{
    byte[] Generate(EnrollmentCertificateDto data);
}

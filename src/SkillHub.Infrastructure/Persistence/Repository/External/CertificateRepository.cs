using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Infrastructure.Service;

namespace SkillHub.Infrastructure.Persistence.Repository.External;

public class CertificateRepository : ICertificateService
{
    private readonly PdfCertificateService _pdfService;

    public CertificateRepository(
        PdfCertificateService pdfService)
    {
        _pdfService = pdfService;
    }

    public byte[] Generate(EnrollmentCertificateDto data)
    {
        return _pdfService.Render(data);
    }
}

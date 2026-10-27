namespace SkillHub.Application.DTOs.Enrollments;

public class CertificateFileDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string FileName { get; set; } = string.Empty;
}

using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.Rules;

namespace SkillHub.Application.Service;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEnrollmentTransaction _enrollmentTransaction;
    private readonly ICurrentUser _currentUser;
    private readonly IExcelService _excelService;
    private readonly ICertificateService _certificateService;
    private readonly TimeProvider _timeProvider;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IWorkshopRepository workshopRepository,
        IUserRepository userRepository,
        IEnrollmentTransaction enrollmentTransaction,
        ICurrentUser currentUser,
        IExcelService excelService,
        ICertificateService certificateService,
        TimeProvider timeProvider)
    {
        _enrollmentRepository = enrollmentRepository;
        _workshopRepository = workshopRepository;
        _userRepository = userRepository;
        _enrollmentTransaction = enrollmentTransaction;
        _currentUser = currentUser;
        _excelService = excelService;
        _certificateService = certificateService;
        _timeProvider = timeProvider;
    }

    public async Task<EnrollmentDto> EnrollAsync(Guid workshopId, CancellationToken ct)
    {
        if (!_currentUser.IsParticipant)
        {
            throw new ForbiddenException("Only participants can enroll themselves.");
        }

        var enrollment = await EnrollParticipantAsync(_currentUser.UserId, workshopId, ct);
        return enrollment;
    }

    private async Task<EnrollmentDto> EnrollParticipantAsync(Guid participantId, Guid workshopId, CancellationToken ct)
    {
        // Keep validation and saving in one transaction to prevent simultaneous requests from taking the same seat.
        var responseDto = await _enrollmentTransaction.ExecuteAsync(participantId, workshopId, async token =>
        {
            var workshopSnapshot = await _workshopRepository.GetAsync(workshopId, token);

            if (workshopSnapshot == null)
            {
                throw new NotFoundException("Workshop not found.");
            }

            var workshop = workshopSnapshot.Workshop;
            var activeCount = await _enrollmentRepository.CountActiveAsync(workshopId, token);
            var activeWorkshops = await _enrollmentRepository.GetActiveWorkshopsAsync(participantId, token);
            var now = _timeProvider.GetUtcNow();

            EnrollmentPolicy.EnsureCanEnroll(workshop, activeCount, activeWorkshops, now);

            Enrollment enrollment = Enrollment.Create(participantId, workshopId, now);
            await _enrollmentRepository.AddAsync(enrollment, token);

            var createdEnrollment = await _enrollmentRepository.GetDetailsAsync(enrollment.Id, token);
            return createdEnrollment!;
        }, ct);

        return responseDto;
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetMineAsync(CancellationToken ct)
    {
        if (!_currentUser.IsParticipant)
        {
            throw new ForbiddenException();
        }

        var enrollments = await _enrollmentRepository.GetMineAsync(_currentUser.UserId, ct);
        return enrollments;
    }

    public async Task<EnrollmentDto> CancelAsync(Guid id, CancellationToken ct)
    {
        var originalEnrollment = await _enrollmentRepository.GetAsync(id, ct);

        if (originalEnrollment == null)
        {
            throw new NotFoundException("Enrollment not found.");
        }

        EnsureOwnerOrEmployee(originalEnrollment.ParticipantId);

        var responseDto = await _enrollmentTransaction.ExecuteAsync(
            originalEnrollment.ParticipantId, originalEnrollment.WorkshopId, async token =>
        {
            var enrollment = await _enrollmentRepository.GetAsync(id, token);

            if (enrollment == null)
            {
                throw new NotFoundException("Enrollment not found.");
            }

            enrollment.Cancel(
                _currentUser.UserId, _currentUser.IsEmployee,
                enrollment.Workshop.StartTime, _timeProvider.GetUtcNow());

            await _enrollmentRepository.UpdateAsync(enrollment, token);

            var updatedEnrollment = await _enrollmentRepository.GetDetailsAsync(id, token);
            return updatedEnrollment!;
        }, ct);

        return responseDto;
    }

    public async Task<EnrollmentImportResultDto> ImportAsync(Stream file, CancellationToken ct)
    {
        if (!_currentUser.IsEmployee)
        {
            throw new ForbiddenException();
        }

        var rows = _excelService.ReadEnrollments(file);
        List<RejectedEnrollmentRowDto> rejectedRows = new List<RejectedEnrollmentRowDto>();
        int enrolledRows = 0;

        // Each row uses its own transaction. One rejected row must not undo the accepted rows.
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (row.RejectionReason != null)
                {
                    throw new ValidationException(row.RejectionReason);
                }

                var participant = await _userRepository.GetParticipantByEmailAsync(row.ParticipantEmail, ct);

                if (participant == null)
                {
                    throw new NotFoundException("Participant not found.");
                }

                var workshop = await _workshopRepository.GetByCodeAsync(row.WorkshopCode, ct);

                if (workshop == null)
                {
                    throw new NotFoundException("Workshop not found.");
                }

                await EnrollParticipantAsync(participant.Id, workshop.Id, ct);
                enrolledRows++;
            }
            catch (Exception ex) when (ex is BusinessRuleException or NotFoundException or ValidationException)
            {
                RejectedEnrollmentRowDto rejectedRow = new RejectedEnrollmentRowDto
                {
                    RowNumber = row.RowNumber,
                    Reason = ex.Message
                };

                rejectedRows.Add(rejectedRow);
            }
        }

        EnrollmentImportResultDto responseDto = new EnrollmentImportResultDto
        {
            TotalRows = rows.Count,
            EnrolledRows = enrolledRows,
            RejectedRows = rejectedRows.Count,
            Rejections = rejectedRows
        };

        return responseDto;
    }

    public async Task<CertificateFileDto> GetCertificateAsync(Guid id, CancellationToken ct)
    {
        var enrollment = await _enrollmentRepository.GetDetailsAsync(id, ct);

        if (enrollment == null)
        {
            throw new NotFoundException("Enrollment not found.");
        }

        EnsureOwnerOrEmployee(enrollment.ParticipantId);

        if (enrollment.Status != nameof(EnrollmentStatus.Active))
        {
            throw new BusinessRuleException("Certificates are available only for active enrollments.");
        }

        var workshop = enrollment.Workshop;

        EnrollmentCertificateDto certificateDto = new EnrollmentCertificateDto
        {
            ParticipantName = enrollment.ParticipantName,
            ParticipantEmail = enrollment.ParticipantEmail,
            WorkshopCode = workshop.Code,
            WorkshopTitle = workshop.Title,
            InstructorName = workshop.Instructor.FullName,
            StartTime = workshop.StartTime,
            EndTime = workshop.EndTime,
            EnrolledAt = enrollment.EnrolledAt,
            GeneratedAt = _timeProvider.GetUtcNow()
        };

        CertificateFileDto responseDto = new CertificateFileDto
        {
            Content = _certificateService.Generate(certificateDto),
            FileName = $"SkillHub-{workshop.Code}-{enrollment.Id:N}.pdf"
        };

        return responseDto;
    }

    private void EnsureOwnerOrEmployee(Guid participantId)
    {
        if (!_currentUser.IsEmployee && (!_currentUser.IsParticipant || _currentUser.UserId != participantId))
        {
            throw new ForbiddenException();
        }
    }
}

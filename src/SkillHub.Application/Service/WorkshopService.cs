using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Application.Interfaces.Services;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Application.Service;

public class WorkshopService : IWorkshopService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public WorkshopService(
        IWorkshopRepository workshopRepository,
        IEnrollmentRepository enrollmentRepository,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _workshopRepository = workshopRepository;
        _enrollmentRepository = enrollmentRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<WorkshopDto>> GetAvailableAsync(CancellationToken ct)
    {
        var workshops = await _workshopRepository.GetAvailableAsync(_timeProvider.GetUtcNow(), ct);
        List<WorkshopDto> dtos = new List<WorkshopDto>();

        foreach (var workshop in workshops)
        {
            WorkshopDto responseDto = workshop.ToDto();
            dtos.Add(responseDto);
        }

        return dtos;
    }

    public async Task<WorkshopDto> GetAsync(Guid id, CancellationToken ct)
    {
        var workshop = await _workshopRepository.GetAsync(id, ct);

        if (workshop == null)
        {
            throw new NotFoundException("Workshop not found.");
        }

        WorkshopDto responseDto = workshop.ToDto();
        return responseDto;
    }

    public async Task<WorkshopDto> CreateAsync(CreateWorkshopDto dto, CancellationToken ct)
    {
        if (!_currentUser.IsEmployee)
        {
            throw new ForbiddenException();
        }

        var instructor = await _workshopRepository.GetInstructorByEmailAsync(dto.InstructorEmail, ct);

        if (instructor == null)
        {
            throw new ValidationException("No registered instructor has this email.");
        }

        var existingWorkshop = await _workshopRepository.GetByCodeAsync(dto.Code, ct);

        if (existingWorkshop != null)
        {
            throw new BusinessRuleException("Workshop code already exists.");
        }

        // The domain validates the schedule and capacity before the workshop can be saved.
        Workshop workshop = Workshop.Schedule(
            dto.Code, dto.Title, dto.Description, instructor.Id,
            dto.StartTime, dto.EndTime, dto.Capacity, _timeProvider.GetUtcNow());

        await _workshopRepository.AddAsync(workshop, ct);

        var createdWorkshop = await GetAsync(workshop.Id, ct);
        return createdWorkshop;
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetEnrollmentsAsync(Guid id, CancellationToken ct)
    {
        if (!_currentUser.IsEmployee)
        {
            throw new ForbiddenException();
        }

        await GetAsync(id, ct);
        var enrollments = await _enrollmentRepository.GetByWorkshopAsync(id, ct);

        return enrollments;
    }

    public async Task<IReadOnlyList<InstructorDto>> GetInstructorsAsync(CancellationToken ct)
    {
        if (!_currentUser.IsEmployee)
        {
            throw new ForbiddenException();
        }

        var instructors = await _workshopRepository.GetInstructorsAsync(ct);
        List<InstructorDto> dtos = new List<InstructorDto>();

        foreach (var instructor in instructors)
        {
            InstructorDto responseDto = new InstructorDto
            {
                Id = instructor.Id,
                FullName = instructor.FullName,
                Email = instructor.Email,
                Specialty = instructor.Specialty
            };

            dtos.Add(responseDto);
        }

        return dtos;
    }
}

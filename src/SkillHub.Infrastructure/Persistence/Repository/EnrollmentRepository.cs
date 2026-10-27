using Microsoft.EntityFrameworkCore;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;

namespace SkillHub.Infrastructure.Persistence.Repository;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly SkillHubDbContext _context;

    public EnrollmentRepository(SkillHubDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetAsync(Guid id, CancellationToken ct)
    {
        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Workshop)
            .SingleOrDefaultAsync(enrollment => enrollment.Id == id, ct);

        return enrollment;
    }

    public async Task<IReadOnlyList<Workshop>> GetActiveWorkshopsAsync(Guid participantId, CancellationToken ct)
    {
        var workshops = await _context.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.ParticipantId == participantId && enrollment.Status == EnrollmentStatus.Active)
            .Select(enrollment => enrollment.Workshop)
            .ToListAsync(ct);

        return workshops;
    }

    public async Task<int> CountActiveAsync(Guid workshopId, CancellationToken ct)
    {
        var activeCount = await _context.Enrollments.CountAsync(enrollment =>
            enrollment.WorkshopId == workshopId && enrollment.Status == EnrollmentStatus.Active, ct);

        return activeCount;
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        await _context.Enrollments.AddAsync(enrollment, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Enrollment enrollment, CancellationToken ct)
    {
        // Update only the enrollment. The detached workshop must not be marked as modified.
        _context.Entry(enrollment).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<EnrollmentDto?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var query = _context.Enrollments.AsNoTracking().Where(enrollment => enrollment.Id == id);
        var enrollment = await GetDetailsQuery(query).SingleOrDefaultAsync(ct);

        return enrollment;
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetMineAsync(Guid participantId, CancellationToken ct)
    {
        var query = _context.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.ParticipantId == participantId)
            .OrderByDescending(enrollment => enrollment.EnrolledAt);

        var enrollments = await GetDetailsQuery(query).ToListAsync(ct);
        return enrollments;
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetByWorkshopAsync(Guid workshopId, CancellationToken ct)
    {
        var query = _context.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.WorkshopId == workshopId)
            .OrderBy(enrollment => enrollment.EnrolledAt);

        var enrollments = await GetDetailsQuery(query).ToListAsync(ct);
        return enrollments;
    }

    private IQueryable<EnrollmentDto> GetDetailsQuery(IQueryable<Enrollment> query)
    {
        // Join Identity users to include the participant name without returning private account fields.
        var enrollments =
            from enrollment in query
            join participant in _context.Users.AsNoTracking() on enrollment.ParticipantId equals participant.Id
            let workshop = enrollment.Workshop
            let instructor = workshop.Instructor
            let activeCount = _context.Enrollments.Count(activeEnrollment =>
                activeEnrollment.WorkshopId == workshop.Id && activeEnrollment.Status == EnrollmentStatus.Active)
            select new EnrollmentDto
            {
                Id = enrollment.Id,
                ParticipantId = enrollment.ParticipantId,
                ParticipantName = participant.FullName,
                ParticipantEmail = participant.Email!,
                Status = enrollment.Status.ToString(),
                EnrolledAt = enrollment.EnrolledAt,
                CancelledAt = enrollment.CancelledAt,
                CancelledById = enrollment.CancelledById,
                Workshop = new WorkshopDto
                {
                    Id = workshop.Id,
                    Code = workshop.Code,
                    Title = workshop.Title,
                    Description = workshop.Description,
                    StartTime = workshop.StartTime,
                    EndTime = workshop.EndTime,
                    Capacity = workshop.Capacity,
                    ActiveEnrollments = activeCount,
                    AvailableSeats = workshop.Capacity - activeCount,
                    Instructor = new InstructorDto
                    {
                        Id = instructor.Id,
                        FullName = instructor.FullName,
                        Email = instructor.Email,
                        Specialty = instructor.Specialty
                    }
                }
            };

        return enrollments;
    }
}

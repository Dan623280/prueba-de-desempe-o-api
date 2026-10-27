using Microsoft.EntityFrameworkCore;
using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.Interfaces.Repositories;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;

namespace SkillHub.Infrastructure.Persistence.Repository;

public class WorkshopRepository : IWorkshopRepository
{
    private readonly SkillHubDbContext _context;

    public WorkshopRepository(SkillHubDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkshopSnapshotDto>> GetAvailableAsync(DateTimeOffset now, CancellationToken ct)
    {
        var workshops = await _context.Workshops
            .AsNoTracking()
            .Include(workshop => workshop.Instructor)
            .Where(workshop => workshop.StartTime > now)
            .OrderBy(workshop => workshop.StartTime)
            .Select(workshop => new WorkshopSnapshotDto
            {
                Workshop = workshop,
                ActiveCount = _context.Enrollments.Count(enrollment =>
                    enrollment.WorkshopId == workshop.Id && enrollment.Status == EnrollmentStatus.Active)
            })
            .ToListAsync(ct);

        return workshops;
    }

    public async Task<WorkshopSnapshotDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var workshop = await _context.Workshops
            .AsNoTracking()
            .Include(workshop => workshop.Instructor)
            .Where(workshop => workshop.Id == id)
            .Select(workshop => new WorkshopSnapshotDto
            {
                Workshop = workshop,
                ActiveCount = _context.Enrollments.Count(enrollment =>
                    enrollment.WorkshopId == workshop.Id && enrollment.Status == EnrollmentStatus.Active)
            })
            .SingleOrDefaultAsync(ct);

        return workshop;
    }

    public async Task<Workshop?> GetByCodeAsync(string code, CancellationToken ct)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var workshop = await _context.Workshops
            .AsNoTracking()
            .SingleOrDefaultAsync(workshop => workshop.Code == normalizedCode, ct);

        return workshop;
    }

    public async Task<Instructor?> GetInstructorByEmailAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var instructor = await _context.Instructors
            .AsNoTracking()
            .SingleOrDefaultAsync(instructor => instructor.Email == normalizedEmail, ct);

        return instructor;
    }

    public async Task<IReadOnlyList<Instructor>> GetInstructorsAsync(CancellationToken ct)
    {
        var instructors = await _context.Instructors
            .AsNoTracking()
            .OrderBy(instructor => instructor.FullName)
            .ToListAsync(ct);

        return instructors;
    }

    public async Task AddAsync(Workshop workshop, CancellationToken ct)
    {
        await _context.Workshops.AddAsync(workshop, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TopWorkshopDto>> GetTopAsync(CancellationToken ct)
    {
        var workshops = await _context.Workshops
            .AsNoTracking()
            .Select(workshop => new TopWorkshopDto
            {
                Id = workshop.Id,
                Code = workshop.Code,
                Title = workshop.Title,
                Capacity = workshop.Capacity,
                ActiveEnrollments = _context.Enrollments.Count(enrollment =>
                    enrollment.WorkshopId == workshop.Id && enrollment.Status == EnrollmentStatus.Active),
                AvailableSeats = workshop.Capacity - _context.Enrollments.Count(enrollment =>
                    enrollment.WorkshopId == workshop.Id && enrollment.Status == EnrollmentStatus.Active)
            })
            .OrderByDescending(workshop => workshop.ActiveEnrollments)
            .ThenBy(workshop => workshop.Code)
            .ToListAsync(ct);

        return workshops;
    }
}

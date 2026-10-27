using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillHub.Application.Enums;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Identity;
using SkillHub.Infrastructure.Service;

namespace SkillHub.Infrastructure.Persistence;

public class DatabaseInitializer
{
    private readonly SkillHubDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ExcelWorkbookService _excelWorkbookService;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        SkillHubDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ExcelWorkbookService excelWorkbookService,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _excelWorkbookService = excelWorkbookService;
        _logger = logger;
    }

    public async Task InitializeAsync(string workbookPath, CancellationToken ct = default)
    {
        await _context.Database.MigrateAsync(ct);
        // Seeding is serialized as well, so simultaneous API startups remain idempotent.
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(813460192)", ct);
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                Ensure(await _roleManager.CreateAsync(new IdentityRole<Guid>(role)));
            }
        }
        if (!File.Exists(workbookPath))
        {
            throw new FileNotFoundException("The required SkillHub seed workbook was not found.", workbookPath);
        }

        using var file = File.OpenRead(workbookPath);
        using var workbook = _excelWorkbookService.Open(file);
        var instructorSheet = workbook.Worksheet("Instructors");
        foreach (var row in instructorSheet.RowsUsed().Skip(1))
        {
            var user = await SeedUserAsync(row, nameof(UserRole.Employee));
            if (!await _context.Instructors.AnyAsync(i => i.Id == user.Id, ct))
            {
                _context.Instructors.Add(new Instructor(user.Id, user.FullName, user.Email!, row.Cell(4).GetString()));
                await _context.SaveChangesAsync(ct);
            }
        }
        foreach (var row in workbook.Worksheet("Participants").RowsUsed().Skip(1))
        {
            await SeedUserAsync(row, nameof(UserRole.Participant));
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        foreach (var row in workbook.Worksheet("Workshops").RowsUsed().Skip(1))
        {
            var code = row.Cell(1).GetString().Trim().ToUpperInvariant();
            if (await _context.Workshops.AnyAsync(w => w.Code == code, ct))
            {
                continue;
            }

            var email = row.Cell(4).GetString().Trim().ToLowerInvariant();
            var instructor = await _context.Instructors.SingleAsync(i => i.Email == email, ct);
            DateTimeOffset ReadLocal(int column)
            {
                var local = DateTime.SpecifyKind(row.Cell(column).GetDateTime(), DateTimeKind.Unspecified);
                return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone));
            }
            _context.Workshops.Add(Workshop.ImportPublished(code, row.Cell(2).GetString(), row.Cell(3).GetString(),
                instructor.Id, ReadLocal(5), ReadLocal(6), row.Cell(7).GetValue<int>()));
            await _context.SaveChangesAsync(ct);
        }
        // The Enrollments worksheet is intentionally processed only by the import endpoint.
        await tx.CommitAsync(ct);
        _logger.LogInformation("SkillHub seed complete: {Instructors} instructors, {Users} users and {Workshops} workshops.",
            await _context.Instructors.CountAsync(ct), await _context.Users.CountAsync(ct), await _context.Workshops.CountAsync(ct));
    }

    private async Task<ApplicationUser> SeedUserAsync(IXLRow row, string role)
    {
        var email = row.Cell(2).GetString().Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { Id = Guid.NewGuid(), FullName = row.Cell(1).GetString().Trim(), Email = email, UserName = email, EmailConfirmed = true };
            Ensure(await _userManager.CreateAsync(user, row.Cell(3).GetString()));
        }
        if (!await _userManager.IsInRoleAsync(user, role))
        {
            Ensure(await _userManager.AddToRoleAsync(user, role));
        }

        return user;
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Interfaces.Repositories;

namespace SkillHub.Infrastructure.Persistence.Repository;

public class EnrollmentTransaction : IEnrollmentTransaction
{
    private readonly SkillHubDbContext _context;

    public EnrollmentTransaction(
        SkillHubDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteAsync<T>(Guid participantId, Guid workshopId, Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        try
        {
            // A fixed lock order prevents deadlocks; deterministic keys work across API processes.
            var participantKey = LockKey($"participant:{participantId:D}");
            var workshopKey = LockKey($"workshop:{workshopId:D}");
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({participantKey})", ct);
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({workshopKey})", ct);
            var result = await action(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }
    private static long LockKey(string value)
    {
        return BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}

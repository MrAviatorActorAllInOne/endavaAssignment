using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services
{
    public class PolicyExpirationJob
    {
        private readonly IClock _clock;
        private readonly ILogger<PolicyExpirationJob> _logger;

        public PolicyExpirationJob(IClock clock, ILogger<PolicyExpirationJob> logger)
        {
            _clock = clock;
            _logger = logger;
        }

        public async Task<int> RunOnceAsync(AppDbContext db, CancellationToken ct = default)
        {
            var nowLocal = _clock.Now.LocalDateTime;
            var today = DateOnly.FromDateTime(nowLocal.Date);
            var targetEndDate = today.AddDays(-1);

            var candidates = await db.Policies
                .Where(p => p.EndDate == targetEndDate)
                .Select(p => new { p.Id, p.EndDate })
                .ToListAsync(ct);

            var count = 0;

            foreach (var p in candidates)
            {
                // skip if already logged
                var exists = await db.PolicyExpirationLogs
                                     .AnyAsync(x => x.PolicyId == p.Id, ct);
                if (exists) continue;

                db.PolicyExpirationLogs.Add(new PolicyExpirationLog
                {
                    PolicyId = p.Id,
                    EndDate = p.EndDate,
                    LoggedAt = _clock.Now
                });

                _logger.LogInformation("Policy {PolicyId} expired on {EndDate}.",
                    p.Id, p.EndDate);

                count++;
            }

            if (count > 0)
                await db.SaveChangesAsync(ct);

            return count;
        }
    }
}

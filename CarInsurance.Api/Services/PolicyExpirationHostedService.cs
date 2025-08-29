using CarInsurance.Api.Data;

namespace CarInsurance.Api.Services
{
    public class PolicyExpirationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IClock _clock;
        private readonly ILogger<PolicyExpirationHostedService> _logger;

        public PolicyExpirationHostedService(
            IServiceScopeFactory scopeFactory,
            IClock clock,
            ILogger<PolicyExpirationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _clock = clock;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var job = scope.ServiceProvider.GetRequiredService<PolicyExpirationJob>();

                    var logged = await job.RunOnceAsync(db, stoppingToken);
                    if (logged > 0)
                        _logger.LogInformation("PolicyExpirationJob: logged {Count} expirations.", logged);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PolicyExpirationJob failed.");
                }

                // compute delay until next 00:30 local
                var now = _clock.Now.LocalDateTime;
                var next = now.Date.AddDays(1).AddHours(0).AddMinutes(30);
                var delay = next - now;
                if (delay < TimeSpan.FromMinutes(1)) delay = TimeSpan.FromMinutes(1);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException) { }
            }
        }
    }
}

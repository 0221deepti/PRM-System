using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;

namespace PRM.Infrastructure.Scheduler;

/// <summary>
/// Background scheduler that periodically computes employee statuses and flags project health.
/// Interval is read from SystemConfig on each run, allowing live config changes.
/// </summary>
public class HealthFlaggingScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HealthFlaggingScheduler> _logger;

    public HealthFlaggingScheduler(IServiceScopeFactory scopeFactory, ILogger<HealthFlaggingScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health Flagging Scheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunHealthCheckAsync(stoppingToken);

            // Read interval from config each iteration
            var intervalHours = await GetSchedulerIntervalAsync(stoppingToken);
            _logger.LogInformation("Next health check in {Hours} hours.", intervalHours);

            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    private async Task RunHealthCheckAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var healthService = scope.ServiceProvider.GetRequiredService<IHealthFlaggingService>();
            await healthService.ComputeEmployeeStatusesAsync(ct);
            await healthService.FlagProjectHealthAsync(ct);
            _logger.LogInformation("Health check completed at {Time}.", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled health check.");
        }
    }

    private async Task<int> GetSchedulerIntervalAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
            var config = await configRepo.GetAsync(ct);
            return config.SchedulerIntervalHours;
        }
        catch
        {
            return 4; // Default fallback
        }
    }
}


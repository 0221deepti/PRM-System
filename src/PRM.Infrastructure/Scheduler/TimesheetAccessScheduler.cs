using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PRM.Application.Interfaces.Services;

namespace PRM.Infrastructure.Scheduler;

public class TimesheetAccessScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TimesheetAccessScheduler> _logger;

    public TimesheetAccessScheduler(IServiceScopeFactory scopeFactory, ILogger<TimesheetAccessScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timesheet access scheduler started.");

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        do
        {
            await ProcessAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITimesheetAccessService>();
            await service.ProcessDailyAsync(ct);
            _logger.LogInformation("Timesheet access daily process completed at {Time}.", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing timesheet reminder and freeze workflow.");
        }
    }
}
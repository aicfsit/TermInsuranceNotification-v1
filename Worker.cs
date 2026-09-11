using TermInsuranceNotification.Abstractions;

namespace TermInsuranceNotification
{
    /// <summary>
    /// Decides WHEN a notification run happens. The run itself belongs to
    /// INotificationService, so scheduling and business logic stay separate.
    /// </summary>
    public class Worker : BackgroundService
    {
        private const string DefaultScheduledTime = "15:20";

        private readonly ILogger<Worker> _logger;
        private readonly INotificationService _notificationService;
        private readonly string _scheduledTime;

        public Worker(ILogger<Worker> logger,
                      INotificationService notificationService,
                      IConfiguration configuration)
        {
            _logger = logger;
            _notificationService = notificationService;
            _scheduledTime = configuration["Notification:ScheduledTime"] ?? DefaultScheduledTime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Term Insurance Notification worker started. Daily run at {time}.", _scheduledTime);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilNextRun(_scheduledTime);
                _logger.LogInformation("Next run in {hours:0.00} hour(s), at {when}.",
                    delay.TotalHours, DateTime.Now.Add(delay));

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;  // shutting down
                }

                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    _notificationService.Run(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification run failed; the worker stays alive for the next run.");
                }
            }

            _logger.LogInformation("Term Insurance Notification worker stopped.");
        }

        /// <summary>Time left until the next occurrence of the configured time. Accepts "9", "9:30" or "09:30".</summary>
        private static TimeSpan GetDelayUntilNextRun(string scheduledTime)
        {
            var parts = (scheduledTime ?? string.Empty).Trim().Split(':');

            if (!int.TryParse(parts.ElementAtOrDefault(0), out var hour)) hour = 9;
            if (!int.TryParse(parts.ElementAtOrDefault(1), out var minute)) minute = 0;

            hour = Math.Clamp(hour, 0, 23);
            minute = Math.Clamp(minute, 0, 59);

            var now = DateTime.Now;
            var next = now.Date.AddHours(hour).AddMinutes(minute);
            if (next <= now) next = next.AddDays(1);

            return next - now;
        }
    }
}

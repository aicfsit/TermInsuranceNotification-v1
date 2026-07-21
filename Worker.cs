using TermInsuranceNotification.Helper;
using TermInsuranceNotification.Repository;
using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly NotificationRepository _repo;
        private readonly ApplicationLogsConfig _config;
        private readonly IConfiguration _appSettings;

        public Worker(ILogger<Worker> logger,
                      NotificationRepository repo,
                      ApplicationLogsConfig config,
                      IConfiguration appSettings)
        {
            _logger = logger;
            _repo = repo;
            _config = config;
            _appSettings = appSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Scheduled run time, e.g. "09:30" (falls back to WorldCheck:DailyScheduledTimes, then 09:30)
            var scheduledTime = _appSettings["Notification:ScheduledTime"]
                                ?? _appSettings["WorldCheck:DailyScheduledTimes"]
                                ?? "09:30";

            _logger.LogInformation("Term Insurance Notification worker started. Scheduled daily at {time}.", scheduledTime);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilNextRun(scheduledTime);
                _logger.LogInformation("Next notification run in {hrs:0.00} hours (at {when}).",
                    delay.TotalHours, DateTime.Now.Add(delay));

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break; // shutting down
                }

                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    ProcessNotifications();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification run failed.");
                }
            }
        }

        private void ProcessNotifications()
        {
            var configs = _repo.GetActiveConfigs();
            _logger.LogInformation("Loaded {count} active notification config(s).", configs.Count);

            foreach (var cfg in configs)
            {
                List<ClientRecipient> recipients;
                try
                {
                    recipients = _repo.GetRecipients(cfg.QueryToExecute);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed executing query for report '{report}': {query}",
                        cfg.ReportName, cfg.QueryToExecute);
                    continue;
                }

                _logger.LogInformation("Report '{report}': {count} recipient(s).",
                    cfg.ReportName, recipients.Count);

                foreach (var r in recipients)
                {
                    if (string.IsNullOrWhiteSpace(r.Email))
                    {
                        _logger.LogWarning("Skipping {client} (policy {policy}) - no email.",
                            r.ClientName, r.PolicyNumber);
                        continue;
                    }

                    try
                    {
                        var body = ApplyPlaceholders(cfg.EmailBody, r);
                        var subject = ApplyPlaceholders(cfg.EmailSubject, r);

                        // Advisor gets CC'd per recipient; fall back to the static CC column.
                        var cc = BuildCc(cfg.CcEmail, r);

                       EmailAndSms.SendEmail(
                            emailto: r.Email,
                            emailbody: body,
                            emailsubject: subject,
                            attachmentbytes: null,
                            attachmentname: "",
                            _config: _config,
                            _strAttachment: "",
                            Bcc: cfg.BccEmail ?? string.Empty,
                            CC: cc,
                            emailpriority: "");

                        _logger.LogInformation("Sent '{report}' to {email} (policy {policy}).",
                            cfg.ReportName, r.Email, r.PolicyNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed sending '{report}' to {email}.",
                            cfg.ReportName, r.Email);
                    }
                }
            }
        }

        /// <summary>Replaces {ClientName}, {PolicyNumber}, {RenewalDate}, {Days} tokens.</summary>
        private static string ApplyPlaceholders(string template, ClientRecipient r)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            return template
                .Replace("{ClientName}", r.ClientName ?? string.Empty)
                .Replace("{ClientId}", r.ClientId ?? string.Empty)
                .Replace("{PolicyNumber}", r.PolicyNumber ?? string.Empty)
                .Replace("{RenewalDate}", r.RenewalDate?.ToString("dd-MMM-yyyy") ?? string.Empty)
                .Replace("{Days}", r.DaysToRenewal.ToString())
                .Replace("{AdvisorEmail}", r.AdvisorEmail ?? string.Empty);
        }

        /// <summary>Combines the config's static CC with the per-recipient advisor email(s).</summary>
        private static string BuildCc(string configCc, ClientRecipient r)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(configCc)) parts.Add(configCc.Trim());
            if (!string.IsNullOrWhiteSpace(r.AdvisorEmailServicing)) parts.Add(r.AdvisorEmailServicing.Trim());
            if (!string.IsNullOrWhiteSpace(r.AdvisorEmail)) parts.Add(r.AdvisorEmail.Trim());

            // de-duplicate, keep it as a comma-separated string
            return string.Join(",", parts
                .SelectMany(p => p.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(e => e.Trim())
                .Where(e => e.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static TimeSpan GetDelayUntilNextRun(string scheduledTime)
        {
            // Accept "9", "9:30", "09:30"
            int hour = 9, minute = 30;
            var t = scheduledTime.Trim();
            if (t.Contains(':'))
            {
                var parts = t.Split(':');
                int.TryParse(parts[0], out hour);
                if (parts.Length > 1) int.TryParse(parts[1], out minute);
            }
            else
            {
                int.TryParse(t, out hour);
                minute = 0;
            }

            var now = DateTime.Now;
            var next = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            if (next <= now) next = next.AddDays(1);
            return next - now;
        }
    }
}

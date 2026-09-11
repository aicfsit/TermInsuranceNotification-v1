using TermInsuranceNotification.Abstractions;
using TermInsuranceNotification.Helper;
using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Services
{
    /// <summary>
    /// Orchestrates one notification run.
    /// The stored procedure returns every due policy in a single result set;
    /// each row is matched to its interval rule by DaysToRenewal.
    /// This class owns no data access, no templating and no transport - only the sequence.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly ITemplateProvider _templateProvider;
        private readonly ITemplateRenderer _renderer;
        private readonly IRecipientResolver _recipientResolver;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationLogsConfig _config;
        private readonly ILogger<NotificationService> _logger;
        private readonly string _recipientQuery;

        public NotificationService(
            INotificationRepository repository,
            ITemplateProvider templateProvider,
            ITemplateRenderer renderer,
            IRecipientResolver recipientResolver,
            IEmailSender emailSender,
            ApplicationLogsConfig config,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _repository = repository;
            _templateProvider = templateProvider;
            _renderer = renderer;
            _recipientResolver = recipientResolver;
            _emailSender = emailSender;
            _config = config;
            _logger = logger;
            _recipientQuery = configuration["Notification:RecipientQuery"]
                              ?? "EXEC dbo.sp_GetTermInsuranceRenewalsNotification";
        }

        public void Run(CancellationToken cancellationToken)
        {
            // rules keyed by the day offset the stored procedure reports
            var rules = _repository.GetActiveConfigs()
                                   .ToDictionary(c => c.DaysBeforeExpiry);

            if (rules.Count == 0)
            {
                _logger.LogWarning("No active notification rules configured - run skipped.");
                return;
            }

            _logger.LogInformation("Run started. Active intervals: {intervals}. Testing mode: {testing}.",
                string.Join(", ", rules.Values.Select(r => r.IntervalCode)), _config.IsTestingEmail);

            List<ClientRecipient> recipients;
            try
            {
                recipients = _repository.GetRecipients(_recipientQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recipient query failed: {query}", _recipientQuery);
                return;
            }

            _logger.LogInformation("{count} policy(ies) returned.", recipients.Count);

            foreach (var recipient in recipients)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // day offsets without a rule (90 / 120 / 365) are reported and ignored
                if (!rules.TryGetValue(recipient.DaysToRenewal, out var rule))
                {
                    _logger.LogDebug("Policy {policy} at {days} day(s) has no configured interval - ignored.",
                        recipient.PolicyNumber, recipient.DaysToRenewal);
                    continue;
                }

                ProcessRecipient(rule, recipient);
            }

            _logger.LogInformation("Run finished.");
        }

        private void ProcessRecipient(NotificationConfig rule, ClientRecipient recipient)
        {
            var addresses = _recipientResolver.Resolve(rule, recipient);

            if (!addresses.HasRecipient)
            {
                _logger.LogWarning("[{interval}] Policy {policy} skipped - no client email.",
                    rule.IntervalCode, recipient.PolicyNumber);
                return;
            }

            if (_repository.IsAlreadySent(recipient.PolicyNumber, rule.IntervalCode))
            {
                _logger.LogInformation("[{interval}] Policy {policy} skipped - already notified.",
                    rule.IntervalCode, recipient.PolicyNumber);
                return;
            }

            var template = _templateProvider.GetTemplate(rule);
            if (string.IsNullOrWhiteSpace(template))
            {
                _logger.LogError("[{interval}] No template body available - policy {policy} skipped.",
                    rule.IntervalCode, recipient.PolicyNumber);
                return;
            }

            var log = CreateLog(rule, recipient, addresses);

            try
            {
                var body = _renderer.Render(template, recipient);
                var subject = _renderer.Render(rule.EmailSubject, recipient);

                // captured before sending, so a failed attempt still records what was attempted
                log.EmailSubject = subject;

                _emailSender.Send(addresses.To, addresses.Cc, addresses.Bcc, subject, body);

                log.IsSuccess = true;
                _logger.LogInformation("[{interval}] Sent for policy {policy} to {to} (cc {cc}).",
                    rule.IntervalCode, recipient.PolicyNumber, addresses.To, addresses.Cc);
            }
            catch (Exception ex)
            {
                log.IsSuccess = false;
                log.ErrorMessage = ex.Message;
                _logger.LogError(ex, "[{interval}] Send failed for policy {policy}.",
                    rule.IntervalCode, recipient.PolicyNumber);
            }

            SafeLog(log);
        }

        private NotificationLog CreateLog(NotificationConfig rule, ClientRecipient recipient, EmailAddresses addresses) => new()
        {
            ReportId = rule.ReportId,
            IntervalCode = rule.IntervalCode,
            PolicyNumber = recipient.PolicyNumber,
            PolicyRefNo = recipient.PolicyRefNo,
            ClientId = recipient.ClientId,
            ClientName = recipient.ClientName,
            AdvisorCode = recipient.AdvisorCode,
            ToEmail = addresses.To,
            CcEmail = addresses.Cc,
            BccEmail = addresses.Bcc,
            ExpiryDate = recipient.RenewalDate,
            DaysToRenewal = recipient.DaysToRenewal,
            IsTestingEmail = _config.IsTestingEmail
        };

        /// <summary>A logging failure must never abort the run.</summary>
        private void SafeLog(NotificationLog log)
        {
            try
            {
                _repository.LogAttempt(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not write the notification log for policy {policy}.", log.PolicyNumber);
            }
        }
    }
}

using TermInsuranceNotification.Abstractions;
using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Services
{
    /// <summary>
    /// To  = client.
    /// CC  = assigned advisor (servicing + primary) + the config CC list (CLRT team).
    /// BCC = the config BCC list.
    /// Duplicates are removed, comparison is case-insensitive.
    /// </summary>
    public class RecipientResolver : IRecipientResolver
    {
        public EmailAddresses Resolve(NotificationConfig config, ClientRecipient recipient)
        {
            return new EmailAddresses
            {
                To  = Combine(recipient.Email),
                Cc  = Combine(recipient.AdvisorEmailServicing, recipient.AdvisorEmail, config.CcEmail),
                Bcc = Combine(config.BccEmail)
            };
        }

        /// <summary>Splits on , and ;, trims, de-duplicates and re-joins with commas.</summary>
        private static string Combine(params string?[] values)
        {
            var separators = new[] { ',', ';' };

            var addresses = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .SelectMany(v => v!.Split(separators, StringSplitOptions.RemoveEmptyEntries))
                .Select(a => a.Trim())
                .Where(a => a.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join(",", addresses);
        }
    }
}

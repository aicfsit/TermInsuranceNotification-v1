using TermInsuranceNotification.Abstractions;
using TermInsuranceNotification.Helper;

namespace TermInsuranceNotification.Services
{
    /// <summary>
    /// Adapter over the static EmailAndSms helper so the rest of the app
    /// depends on IEmailSender only (DIP) and can be unit tested with a fake.
    /// The helper itself applies the IsTestingEmail redirect.
    /// </summary>
    public class EmailApiSender : IEmailSender
    {
        private readonly ApplicationLogsConfig _config;

        public EmailApiSender(ApplicationLogsConfig config) => _config = config;

        public void Send(string to, string cc, string bcc, string subject, string htmlBody)
        {
            EmailAndSms.SendEmail(
                emailto: to,
                emailbody: htmlBody,
                emailsubject: subject,
                attachmentbytes: null!,
                attachmentname: string.Empty,
                _config: _config,
                _strAttachment: string.Empty,
                Bcc: bcc,
                CC: cc,
                emailpriority: string.Empty);
        }
    }
}

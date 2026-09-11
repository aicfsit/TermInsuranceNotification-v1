using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Abstractions
{
    /// <summary>Data access for notification configs, recipients and the sent log.</summary>
    public interface INotificationRepository
    {
        List<NotificationConfig> GetActiveConfigs();

        List<ClientRecipient> GetRecipients(string queryToExecute);

        /// <summary>True when this policy already got a successful production mail for this interval.</summary>
        bool IsAlreadySent(string policyNumber, string intervalCode);

        void LogAttempt(NotificationLog log);
    }
}

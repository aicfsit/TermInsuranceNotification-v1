using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Abstractions
{
    /// <summary>Decides the final To / CC / BCC addresses for one notification.</summary>
    public interface IRecipientResolver
    {
        EmailAddresses Resolve(NotificationConfig config, ClientRecipient recipient);
    }
}

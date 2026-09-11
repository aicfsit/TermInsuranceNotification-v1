using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Abstractions
{
    /// <summary>Supplies the raw (un-rendered) HTML body for a notification config.</summary>
    public interface ITemplateProvider
    {
        string GetTemplate(NotificationConfig config);
    }
}

using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Abstractions
{
    /// <summary>Replaces the {Token} placeholders in a template with recipient values.</summary>
    public interface ITemplateRenderer
    {
        string Render(string template, ClientRecipient recipient);
    }
}

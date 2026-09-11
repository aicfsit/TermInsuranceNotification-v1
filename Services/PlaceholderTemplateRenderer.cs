using TermInsuranceNotification.Abstractions;
using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Services
{
    /// <summary>
    /// Replaces the {Token} placeholders used by the term insurance templates.
    /// A new token only needs a new entry in BuildTokens - no caller changes (OCP).
    /// </summary>
    public class PlaceholderTemplateRenderer : ITemplateRenderer
    {
        private const string DateFormat = "dd MMM yyyy";   // e.g. 12 Aug 2027

        public string Render(string template, ClientRecipient recipient)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            var result = template;
            foreach (var token in BuildTokens(recipient))
                result = result.Replace(token.Key, token.Value);

            return result;
        }

        private static Dictionary<string, string> BuildTokens(ClientRecipient r) => new()
        {
            ["{FirstName}"]    = r.FirstName,
            ["{ClientName}"]   = r.ClientName,
            ["{ClientId}"]     = r.ClientId,
            ["{AdvisorName}"]  = r.AdvisorName,
            ["{PolicyNumber}"] = r.PolicyNumber,
            ["{PolicyRefNo}"]  = r.PolicyRefNo,
            ["{ExpiryDate}"]   = r.RenewalDate?.ToString(DateFormat) ?? string.Empty,
            ["{RenewalDate}"]  = r.RenewalDate?.ToString(DateFormat) ?? string.Empty,
            ["{Days}"]         = r.DaysToRenewal.ToString()
        };
    }
}

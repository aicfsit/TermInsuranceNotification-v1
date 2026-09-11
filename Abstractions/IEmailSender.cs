namespace TermInsuranceNotification.Abstractions
{
    /// <summary>Sends one email. Abstracted so the worker never touches the email API directly.</summary>
    public interface IEmailSender
    {
        void Send(string to, string cc, string bcc, string subject, string htmlBody);
    }
}

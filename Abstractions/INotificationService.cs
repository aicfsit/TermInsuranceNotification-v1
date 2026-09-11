namespace TermInsuranceNotification.Abstractions
{
    /// <summary>Executes one complete notification run (all active intervals).</summary>
    public interface INotificationService
    {
        void Run(CancellationToken cancellationToken);
    }
}

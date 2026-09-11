namespace TermInsuranceNotification.Model
{
    public class DBModel
    {
        /// <summary>
        /// One row of tbl_client_notification_details: the rule for a single interval.
        /// DaysBeforeExpiry is matched against ClientRecipient.DaysToRenewal.
        /// </summary>
        public class NotificationConfig
        {
            public int ReportId { get; set; }
            public string ReportName { get; set; } = string.Empty;
            public string IntervalCode { get; set; } = string.Empty;   // 2M | 1M | 15D
            public int DaysBeforeExpiry { get; set; }                  // 60 | 30 | 15
            public string TemplateFile { get; set; } = string.Empty;
            public string EmailBody { get; set; } = string.Empty;      // fallback body
            public string EmailSubject { get; set; } = string.Empty;
            public string CcEmail { get; set; } = string.Empty;        // CLRT team list
            public string BccEmail { get; set; } = string.Empty;
        }

        /// <summary>One row returned by sp_GetTermInsuranceRenewalsNotification.</summary>
        public class ClientRecipient
        {
            public string ClientId { get; set; } = string.Empty;
            public string ClientName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PolicyNumber { get; set; } = string.Empty;
            public string PolicyRefNo { get; set; } = string.Empty;
            public DateTime? RenewalDate { get; set; }
            public int DaysToRenewal { get; set; }
            public string AdvisorCode { get; set; } = string.Empty;
            public string AdvisorEmailServicing { get; set; } = string.Empty;
            public string AdvisorEmail { get; set; } = string.Empty;
            public string AdvisorName { get; set; } = string.Empty;

            /// <summary>First word of the client name, used for the {FirstName} token.</summary>
            public string FirstName =>
                (ClientName ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault() ?? string.Empty;
        }

        /// <summary>Resolved To / CC / BCC for a single notification.</summary>
        public class EmailAddresses
        {
            public string To { get; set; } = string.Empty;
            public string Cc { get; set; } = string.Empty;
            public string Bcc { get; set; } = string.Empty;

            public bool HasRecipient => !string.IsNullOrWhiteSpace(To);
        }

        /// <summary>One row of tbl_term_insurance_notification_log.</summary>
        public class NotificationLog
        {
            public int ReportId { get; set; }
            public string IntervalCode { get; set; } = string.Empty;
            public string PolicyNumber { get; set; } = string.Empty;
            public string PolicyRefNo { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string ClientName { get; set; } = string.Empty;
            public string AdvisorCode { get; set; } = string.Empty;
            public string ToEmail { get; set; } = string.Empty;
            public string CcEmail { get; set; } = string.Empty;
            public string BccEmail { get; set; } = string.Empty;
            public DateTime? ExpiryDate { get; set; }
            public int DaysToRenewal { get; set; }
            public string EmailSubject { get; set; } = string.Empty;
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public bool IsTestingEmail { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace TermInsuranceNotification.Model
{
    public class DBModel
    {
        public class NotificationConfig
        {
            public int ReportId { get; set; }
            public string ReportName { get; set; }
            public string QueryToExecute { get; set; }
            public string EmailBody { get; set; }
            public string EmailSubject { get; set; }
            public string CcEmail { get; set; }
            public string BccEmail { get; set; }
        }

        public class ClientRecipient
        {
            public string ClientId { get; set; }
            public string ClientName { get; set; }
            public string Email { get; set; }
            public string PolicyNumber { get; set; }
            public DateTime? RenewalDate { get; set; }
            public int DaysToRenewal { get; set; }
            public string AdvisorEmailServicing { get; set; }
            public string AdvisorEmail { get; set; }
        }
    }
    }

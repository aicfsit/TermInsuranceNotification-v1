using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TermInsuranceNotification.Model

{
    public class SendEmailModel
    {
        public string strToEmail { get; set; }
        public string strSubject { get; set; }
        public string strBody { get; set; }
        public bool IsBodyHTML { get; set; } = false;
        public string strAttachment { get; set; }
        public string strCC { get; set; }
        public string strBcc { get; set; }
        public string strFromEmailId { get; set; }
        public string strPassword { get; set; }
        public string strHost { get; set; }
        public string strDisplayName { get; set; }
        public string userid { get; set; }
        public string Source { get; set; }
        public string replyTo { get; set; } = string.Empty;
        public string emailPriority { get; set; } = string.Empty;
        public string strPhmobile { get; set; }
        public string strMessage { get; set; }
        public string ModuleName { get; set; }
        public string Application { get; set; }

    }
    public class EmailSendwithBytesModel
    {
        public string strToEmail { get; set; }
        public string strSubject { get; set; }
        public string strBody { get; set; }
        public bool IsBodyHTML { get; set; } = false;
        public string strCC { get; set; }
        public string strBcc { get; set; }
        public string strFromEmailId { get; set; }
        public string strPassword { get; set; }
        public string strHost { get; set; }
        public string strDisplayName { get; set; }
        public string userid { get; set; }
        public string Source { get; set; }
        public string replyTo { get; set; } = string.Empty;
        public string emailPriority { get; set; } = string.Empty;
        public Dictionary<string, object> attachmentBytes { get; set; } = null;
        public string ModuleName { get; set; }
        public string Application { get; set; }

    }

    public class SendEmailResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }
        public string Error { get; set; }
    }
}

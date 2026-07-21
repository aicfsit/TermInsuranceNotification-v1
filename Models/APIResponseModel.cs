using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TermInsuranceNotification.Model
{
    public class APIResponseModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }
        public object Error { get; set; }
        public string Result { get; set; }
        public bool IsKycDataChange { get; set; }
    }

    public class Response
    {
        public string Status { get; set; }
        public string Error { get; set; }
    }

}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TermInsuranceNotification.Helper;
using TermInsuranceNotification.Model;


namespace TermInsuranceNotification.Helper
{
    public static class EmailAndSms
    {

        static string user = "contHttpApi";
        static string pass = "TchZgGaO";
        static string sender = "Continental";
        static string urlsms = "https://globalsms.wisoftsolutions.com:1111/API/SendSMS";
        
        public static bool SendEmail(string emailto, string emailbody, string emailsubject, byte[] attachmentbytes, string attachmentname = "", ApplicationLogsConfig _config = null, string _strAttachment = "", string Bcc = "", string CC = "", string emailpriority = "")
        {
            var response = CallEmailAPI(emailto, emailsubject, emailbody, _config, "Email", null, null, _strAttachment, Bcc, CC, emailpriority);
            return true;
        }

        public static string sendSMS(string phmobile, string message)
        {
            string SentResult = String.Empty;
            string StatusCode = String.Empty;

            String url = urlsms;

            string postData = HttpUtility.UrlEncode("username") + "=" + HttpUtility.UrlEncode(user);
            postData += "&" + HttpUtility.UrlEncode("apiId") + "=" + HttpUtility.UrlEncode(pass);
            postData += "&" + HttpUtility.UrlEncode("json") + "=" + HttpUtility.UrlEncode("True");
            postData += "&" + HttpUtility.UrlEncode("destination") + "=" + HttpUtility.UrlEncode(phmobile);
            postData += "&" + HttpUtility.UrlEncode("source") + "=" + HttpUtility.UrlEncode(sender);
            postData += "&" + HttpUtility.UrlEncode("text") + "=" + HttpUtility.UrlEncode(message);

            string getUri = "";
            getUri = url + "?" + postData;

            ServicePointManager.ServerCertificateValidationCallback = (object a, System.Security.Cryptography.X509Certificates.X509Certificate b, System.Security.Cryptography.X509Certificates.X509Chain c, System.Net.Security.SslPolicyErrors d) => { return true; };
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(getUri);
            String resultmsg = "";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader responseReader = new StreamReader(response.GetResponseStream());
            resultmsg = responseReader.ReadToEnd();

            string ResponseId_New = resultmsg;

            Console.WriteLine("Response ID:" + ResponseId_New.ToString().Trim());
            responseReader.Close();
            response.Close();
            return ResponseId_New;
        }
        
        private static string CallEmailAPI(string emails, string sub, string body, ApplicationLogsConfig _config, string emailsms, string mobileno, string message, string _strAttachment, string _bcc, string _cc, string emailpriority = "")
        {
            string result = string.Empty;
            string successAttachment = string.Empty;
            string failAttachment = string.Empty;
            string sendEmailEndpoint = string.Empty;
            string fullUrl = string.Empty;
            SendEmailModel model = new SendEmailModel();
            SendEmailResponse apires = new SendEmailResponse();
            try
            {
                using (HttpClient _client = new HttpClient())
                {
                    model.Source = "Web Api";
                    model.userid = "System";
                    model.ModuleName = "Email";
                    model.Application = "TermInsuranceNotification";
                    if (emailsms == "Email")
                    {
                        if (_config.IsTestingEmail)
                        {
                            model.strToEmail = _config.TestEmails ?? string.Empty;
                            model.strCC = "";
                            model.strBcc = "";
                            model.strSubject = sub;
                        }
                        else
                        {
                            model.strToEmail = emails;
                            model.strCC = _cc;
                            model.strBcc = _bcc;
                            model.strSubject = sub;
                        }
                        model.strDisplayName = "";
                        model.strHost = "";
                       
                        model.strBody = body;
                        model.IsBodyHTML = true;
                        model.strFromEmailId = "donotreply@cfsgroup.com";
                        model.strPassword = "";
                        model.replyTo = "";
                        model.emailPriority = emailpriority;
                        model.strAttachment = _strAttachment;
                        sendEmailEndpoint = _config.emailAPI?.APIEndPoint ?? string.Empty;
                    }
                    else if (emailsms == "SMS")
                    {
                        model.strPhmobile = mobileno;
                        model.strMessage = message;
                        sendEmailEndpoint = _config.emailAPI?.APIEndPointSMS ?? string.Empty;
                    }
                    fullUrl = $"{_config.emailAPI?.APIURL}{sendEmailEndpoint}";
                    string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                    var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, fullUrl) { Content = stringContent };
                    requestMessage.Headers.Add("ClientId", _config.emailAPI.ClientID);
                    requestMessage.Headers.Add("ClientSecret", _config.emailAPI.ClientSecret);
                    requestMessage.Headers.Add("Accept", "application/json");
                    _client.Timeout = TimeSpan.FromMinutes(2);
                    HttpResponseMessage response = _client.Send(requestMessage);
                    if (response.IsSuccessStatusCode)
                    {
                        result = response.Content.ReadAsStringAsync().Result;  // Sync read
                        if (result != null)
                            apires = JsonConvert.DeserializeObject<SendEmailResponse>(result);
                  
                        //apires.Message +=($"User: {Environment.UserName}, Machine: {Environment.MachineName}");
                        //apires.Message += ($"Path exists: {File.Exists(path)}");
                        //apires.Message += ($"Dir exists: {Directory.Exists(Path.GetDirectoryName(path))}");
                        //apires.Message += ($"Path bytes: {BitConverter.ToString(Encoding.UTF8.GetBytes(path))}");
                    }
                    else
                    {
                        result = response.Content.ReadAsStringAsync().Result;  // Sync read
                        if (result != null)
                            apires = JsonConvert.DeserializeObject<SendEmailResponse>(result);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return apires.Message;
        }

        public static bool SendEmailWithAttachments(string emails, string sub, string body, ApplicationLogsConfig _config, string emailsms, string mobileno, string message, Dictionary<string, object> attachmentBytes, string bcc = "", string cc = "")
        {
            try
            {
                var response = CallEmailWithAttachmentsAPI(emails, sub, body, _config, emailsms, mobileno, message, attachmentBytes, bcc, cc);
            }
            catch (Exception ex) { }
            return true;
        }
        
        private static string CallEmailWithAttachmentsAPI(string emails, string sub, string body, ApplicationLogsConfig _config, string emailsms, string mobileno, string message, Dictionary<string, object> attachmentBytes, string bcc = "", string cc = "")
        {
            string result = string.Empty;
            string successAttachment = string.Empty;
            string failAttachment = string.Empty;
            string sendEmailEndpoint = string.Empty;
            string fullUrl = string.Empty;
            EmailSendwithBytesModel model = new EmailSendwithBytesModel();
            SendEmailResponse apires = new SendEmailResponse();

            try
            {
                using (HttpClient _client = new HttpClient())
                {
                    model.Source = "Web Api";
                    model.userid = "SystemUser";
                    model.ModuleName = "WorldCheck";
                    model.Application = "WorldCheck Automation";

                    if (emailsms == "Email")
                    {
                        if (_config.IsTestingEmail)
                        {
                            model.strToEmail = _config.TestEmails ?? string.Empty;
                            model.strCC = "";
                            model.strBcc = "";
                        }
                        else
                        {
                            model.strToEmail = emails;
                            model.strCC = bcc;
                            model.strBcc = cc;
                        }
                        model.strDisplayName = "";
                        model.strHost = "";
                        model.strSubject = (_config.IsTestingEmail == true ? "Testing - " : "") + sub;
                        model.strBody = body;
                        model.IsBodyHTML = true;
                        model.strFromEmailId = "donotreply@cfsgroup.com";
                        model.strPassword = "";
                        model.replyTo = "";
                        model.emailPriority = "";

                        model.attachmentBytes = attachmentBytes;
                        sendEmailEndpoint = _config.emailAPI?.APIEndPointWithAttachment ?? string.Empty;
                    }

                    fullUrl = $"{_config.emailAPI?.APIURL}{sendEmailEndpoint}";
                    string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                    var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, fullUrl) { Content = stringContent };
                    requestMessage.Headers.Add("ClientId", _config.emailAPI.ClientID);
                    requestMessage.Headers.Add("ClientSecret", _config.emailAPI.ClientSecret);
                    requestMessage.Headers.Add("Accept", "application/json");
                    _client.Timeout = TimeSpan.FromMinutes(5);
                    HttpResponseMessage response = _client.Send(requestMessage);
                    if (response.IsSuccessStatusCode)
                    {
                        result = response.Content.ReadAsStringAsync().Result;  // Sync read
                        if (result != null)
                            apires = JsonConvert.DeserializeObject<SendEmailResponse>(result);
                    }
                    else
                    {
                        result = response.Content.ReadAsStringAsync().Result;  // Sync read
                        if (result != null)
                            apires = JsonConvert.DeserializeObject<SendEmailResponse>(result);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return apires.Message;
        }
    }
}

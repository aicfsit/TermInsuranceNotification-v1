namespace TermInsuranceNotification.Helper
{
    public class ApplicationLogsConfig
    {
        public string? Application { get; set; } = string.Empty;
        public string? Source { get; set; } = string.Empty;
        public string? APIKey { get; set; }
        public string? APIEndPoint { get; set; }
        public string? AzureAPIKey { get; set; }
        public string? AzureEndPoint { get; set; }
        public string? ClientID { get; set; }
        public string? ClientSecret { get; set; }
        public string? ArchiveDoc { get; set;} 
        public string? ERP_IBS { get; set; }
        public string? ApplicationLogs { get; set; }
        public string? ArcPath { get; set; }
        public InsuranceCompanies? insuranceCompanies { get; set; }
        public DocumentType? documenttype { get; set; }
        public bool IsTesting { get; set; }
        public string? ListofInsCompanies { get; set; }
        public string? TestEmails { get; set; }
        public bool IsSentSms { get; set; }
        public EmailAPI? emailAPI { get; set; }
        public string? Domain { get; set; } = string.Empty;
        public string? Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public bool IsTestingEmail { get; set; }
        public string CDPEmails { get; set; }
        public bool IsInvoiceExist { get; set; }
        public bool IsAdjustmentExist { get; set; }
        public ProviderKeywords? providerKeywords { get; set; }
        public string? MEDCCEmails { get; set; }
        public string? GENCCEmails { get; set; }
        public string? EmailBCC { get; set; }
        public decimal? thresholdpremium { get; set; }
        public bool IsWorldCheckEnabled { get; set; }

    }
    public class InsuranceCompanies
    {
        public string? Sukoon { get; set; }
        public string? Rak { get; set; }
        public string? DNI { get; set; }
        public string? AlSagr { get; set; }
        public string? Orient { get; set; }
        public string? NGI { get; set; }
        public string? WataniaTakaful { get; set; }
        public string? Adnic { get; set; }
        public string? Saico { get; set; }
    }
    public class DocumentType
    {
        public string? EID { get; set; }
        public string? PP { get; set; }
    }
    public class EmailAPI
    {
        public string? APIURL { get; set; }
        public string? APIEndPoint { get; set; }
        public string? APIEndPointWithAttachment { get; set; }
        public string? APIEndPointSMS { get; set; }
        public string? ClientID { get; set; }
        public string? ClientSecret { get; set; }
    }
    public class ProviderKeywords
    {
        public string? sukoon { get; set; }
        public string? rak { get; set; }
        public string? dni { get; set; }
        public string? alsagr { get; set; }
        public string? orient { get; set; }
        public string? wataniatakaful { get; set; }
        public string? adnic { get; set; }
        public string? saico { get; set; }
    }
}

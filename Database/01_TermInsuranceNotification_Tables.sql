/* =====================================================================
   Term Insurance Expiry Notification - tables
   Target DB : ERP_IBS        Idempotent - safe to re-run.
   ===================================================================== */

/* ---------------------------------------------------------------------
   1. CONFIG - one row per reminder interval.
      DaysBeforeExpiry is the join key: it is matched against the
      DaysToRenewal column returned by sp_GetTermInsuranceRenewalsNotification.
      A day value with no active row here is simply not notified.
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.tbl_client_notification_details', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_client_notification_details
    (
        ReportId         INT           IDENTITY(1,1) NOT NULL,
        ReportName       VARCHAR(150)  NOT NULL,
        IntervalCode     VARCHAR(20)   NOT NULL,   -- 2M | 1M | 15D  (log key)
        DaysBeforeExpiry INT           NOT NULL,   -- 60 | 30 | 15   (match key)
        TemplateFile     VARCHAR(200)  NULL,       -- file under EmailTemplate
        Email_Subject    VARCHAR(500)  NOT NULL,
        Email_Body       VARCHAR(MAX)  NULL,       -- fallback when TemplateFile is empty
        CC_Email         VARCHAR(1000) NULL,       -- CLRT team list (NULL for now)
        Bcc_Email        VARCHAR(1000) NULL,
        Status           BIT           NOT NULL CONSTRAINT DF_tcnd_Status    DEFAULT (1),
        CreatedOn        DATETIME      NOT NULL CONSTRAINT DF_tcnd_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn       DATETIME      NULL,
        CONSTRAINT PK_tbl_client_notification_details PRIMARY KEY CLUSTERED (ReportId),
        CONSTRAINT UQ_tcnd_IntervalCode UNIQUE (IntervalCode),
        CONSTRAINT UQ_tcnd_DaysBeforeExpiry UNIQUE (DaysBeforeExpiry)
    );
END
GO

/* ---------------------------------------------------------------------
   2. SENT LOG - one row per send attempt.
      The filtered unique index is the de-duplication guard:
        - a SUCCESSFUL production mail can never be sent twice;
        - a FAILED attempt can be retried;
        - TESTING mails are excluded, so test runs never block production.
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.tbl_term_insurance_notification_log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_term_insurance_notification_log
    (
        LogId          BIGINT        IDENTITY(1,1) NOT NULL,
        ReportId       INT           NOT NULL,
        IntervalCode   VARCHAR(20)   NOT NULL,
        PolicyNumber   VARCHAR(100)  NOT NULL,
        PolicyRefNo    VARCHAR(100)  NULL,
        ClientId       VARCHAR(50)   NULL,       -- client code (applicant1)
        ClientName     VARCHAR(250)  NULL,
        AdvisorCode    VARCHAR(50)   NULL,       -- servicing consultant (actcod)
        ToEmail        VARCHAR(1000) NULL,
        CcEmail        VARCHAR(1000) NULL,
        BccEmail       VARCHAR(1000) NULL,
        ExpiryDate     DATE          NULL,
        DaysToRenewal  INT           NULL,
        EmailSubject   VARCHAR(500)  NULL,
        IsSuccess      BIT           NOT NULL CONSTRAINT DF_tinl_IsSuccess DEFAULT (0),
        ErrorMessage   VARCHAR(MAX)  NULL,
        IsTestingEmail BIT           NOT NULL CONSTRAINT DF_tinl_IsTesting DEFAULT (0),
        SentOn         DATETIME      NOT NULL CONSTRAINT DF_tinl_SentOn    DEFAULT (GETDATE()),
        CONSTRAINT PK_tbl_term_insurance_notification_log PRIMARY KEY CLUSTERED (LogId)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_tinl_Policy_Interval
        ON dbo.tbl_term_insurance_notification_log (PolicyNumber, IntervalCode)
        WHERE IsSuccess = 1 AND IsTestingEmail = 0;

    CREATE NONCLUSTERED INDEX IX_tinl_SentOn
        ON dbo.tbl_term_insurance_notification_log (SentOn);
END
GO

/* ---------------------------------------------------------------------
   2b. Upgrade an already-created log table with the audit columns.
   --------------------------------------------------------------------- */
IF COL_LENGTH('dbo.tbl_term_insurance_notification_log', 'AdvisorCode') IS NULL
    ALTER TABLE dbo.tbl_term_insurance_notification_log ADD AdvisorCode VARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.tbl_term_insurance_notification_log', 'PolicyRefNo') IS NULL
    ALTER TABLE dbo.tbl_term_insurance_notification_log ADD PolicyRefNo VARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.tbl_term_insurance_notification_log', 'DaysToRenewal') IS NULL
    ALTER TABLE dbo.tbl_term_insurance_notification_log ADD DaysToRenewal INT NULL;
GO
IF COL_LENGTH('dbo.tbl_term_insurance_notification_log', 'EmailSubject') IS NULL
    ALTER TABLE dbo.tbl_term_insurance_notification_log ADD EmailSubject VARCHAR(500) NULL;
GO
/* Already created the table with EmailBody? Uncomment to drop it.
IF COL_LENGTH('dbo.tbl_term_insurance_notification_log', 'EmailBody') IS NOT NULL
    ALTER TABLE dbo.tbl_term_insurance_notification_log DROP COLUMN EmailBody;
GO
*/

/* ---------------------------------------------------------------------
   2c. Tracking view - who was successfully notified, and with what.
   --------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_term_insurance_notification_sent
AS
    SELECT  l.SentOn,
            l.IntervalCode,
            l.DaysToRenewal,
            l.ClientId        AS ClientCode,
            l.ClientName,
            l.AdvisorCode,
            l.PolicyNumber,
            l.PolicyRefNo,
            l.ExpiryDate,
            l.ToEmail,
            l.CcEmail,
            l.EmailSubject,
            l.IsTestingEmail
    FROM    dbo.tbl_term_insurance_notification_log l
    WHERE   l.IsSuccess = 1;
GO

/* ---------------------------------------------------------------------
   3. SEED - the three intervals currently in scope.
      The SP also returns 90, 120 and 365 day rows; they stay un-notified
      until a template exists and a row is added here.
   --------------------------------------------------------------------- */
MERGE dbo.tbl_client_notification_details AS tgt
USING
(
    VALUES
      ('Term Insurance - 2 Months Before Expiry', '2M',  60, 'Term_Insurance_2_Months.html', 'Two months to take care of the details'),
      ('Term Insurance - 1 Month Before Expiry',  '1M',  30, 'Term_Insurance_1_Month.html',  'One month to make sure everything is in order'),
      ('Term Insurance - 15 Days Before Expiry',  '15D', 15, 'Term_Insurance_15_Days.html',  'Your term insurance policy ends in 15 days')
) AS src (ReportName, IntervalCode, DaysBeforeExpiry, TemplateFile, Email_Subject)
    ON tgt.IntervalCode = src.IntervalCode
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ReportName, IntervalCode, DaysBeforeExpiry, TemplateFile, Email_Subject, CC_Email, Status)
    VALUES (src.ReportName, src.IntervalCode, src.DaysBeforeExpiry, src.TemplateFile, src.Email_Subject,
            NULL,            -- CLRT team distribution list goes here
            1)
WHEN MATCHED THEN
    UPDATE SET tgt.ReportName       = src.ReportName,
               tgt.DaysBeforeExpiry = src.DaysBeforeExpiry,
               tgt.TemplateFile     = src.TemplateFile,
               tgt.Email_Subject    = src.Email_Subject,
               tgt.ModifiedOn       = GETDATE();
GO

SELECT ReportId, IntervalCode, DaysBeforeExpiry, TemplateFile, Email_Subject, Status
FROM   dbo.tbl_client_notification_details
ORDER  BY DaysBeforeExpiry DESC;
GO

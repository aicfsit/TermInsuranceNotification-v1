/* =====================================================================
   sp_GetTermInsuranceRenewalsNotification
   Returns every term insurance policy that is due for a reminder today,
   for ALL intervals at once. DaysToRenewal tells the caller which
   reminder the row belongs to (15, 30, 60, 90, 120, 365).
   Target DB : ERP_IBS
   ===================================================================== */
IF OBJECT_ID('dbo.sp_GetTermInsuranceRenewalsNotification', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetTermInsuranceRenewalsNotification;
GO

CREATE PROCEDURE dbo.sp_GetTermInsuranceRenewalsNotification
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.applicant1                      AS ClientId,
        a.client                          AS ClientName,
        a.Email                           AS Email,
        a.polcod                          AS PolicyNumber,
        a.polrendt                        AS RenewalDate,
        a.polrefno                        AS PolicyRefNo,
        DATEDIFF(DAY, GETDATE(), a.polrendt) AS DaysToRenewal,
        b.actcod                          AS AdvisorCode,
        b.email_servicing                 AS AdvisorEmailServicing,
        b.email                           AS AdvisorEmail,
        b.actnam                          AS AdvisorName   -- feeds the {AdvisorName} token
    FROM view_policy_details a
    JOIN view_employee_list  b ON a.ServicingConsultantCode = b.actcod
    WHERE b.Status = 1
      AND a.polctgcod = 'TI'
      AND a.polsts IN ('ACT', 'PAID', 'PAR_SUR')
      AND a.polrendt IS NOT NULL
      AND a.polrendt >= GETDATE()
      AND DATEDIFF(DAY, GETDATE(), a.polrendt) < 365
      AND DATEDIFF(DAY, GETDATE(), a.polrendt) IN (15, 30, 60, 90, 120, 365);
END
GO

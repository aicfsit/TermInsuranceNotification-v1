# CLAUDE.md — Term Insurance Expiry Notification

.NET 8 Worker Service. Sends term-insurance expiry reminders to the client, CCing the assigned advisor (and later the CLRT team).

## 1. Scope

| Interval | Code | `DaysToRenewal` | Template | Status |
|---|---|---|---|---|
| 2 months before expiry | `2M` | 60 | `Term_Insurance_2_Months.html` | **done** |
| 1 month before expiry | `1M` | 30 | `Term_Insurance_1_Month.html` | **done** |
| 15 days before expiry | `15D` | 15 | `Term_Insurance_15_Days.html` | **done** |
| cross-sell reminders | — | 90 / 120 / 365 | — | returned by the SP, **no rule row, not sent** |

All three in scope are standing-instruction-review reminders. Rule: **one successful email per policy per interval**, enforced in the DB.

## 2. Architecture

`Worker` decides *when*; `NotificationService` decides *what*; everything else does one job.

```
Worker : BackgroundService              scheduling only (Notification:ScheduledTime)
  └─ INotificationService ── NotificationService      orchestrates one run
       ├─ INotificationRepository ── NotificationRepository   configs, recipients, sent log
       ├─ ITemplateProvider       ── FileTemplateProvider     EmailTemplate\*.html (cached)
       ├─ ITemplateRenderer       ── PlaceholderTemplateRenderer  {Token} substitution
       ├─ IRecipientResolver      ── RecipientResolver        To / CC / BCC
       └─ IEmailSender            ── EmailApiSender           adapter over EmailAndSms
```

Interfaces live in [Abstractions/](Abstractions/), implementations in [Services/](Services/). All wired in [Program.cs](Program.cs).

- **SRP** – one reason to change per class.
- **OCP** – a new interval is a config row + an HTML file; a new token is one line in `BuildTokens`.
- **LSP** – swap `FileTemplateProvider` for a DB provider without touching the service.
- **ISP** – six small interfaces, no fat helper.
- **DIP** – nothing outside [Program.cs](Program.cs) names a concrete type.

The SP takes **no parameters** and returns every due policy in one result set. `NotificationService`
loads the rules into a `Dictionary<DaysBeforeExpiry, NotificationConfig>` and matches each row by
its `DaysToRenewal`; a day offset with no active rule is logged at Debug and ignored.

Run flow per policy: match rule by day offset → resolve addresses → skip if no client email →
skip if already sent → render → send → write log (success or failure).
A failure on one recipient never aborts the run.

## 3. Templates

Files under [EmailTemplate/](EmailTemplate/), copied to the output folder by the csproj. Tokens:

| Token | Source |
|---|---|
| `{FirstName}` | first word of `ClientName` |
| `{AdvisorName}` | `AdvisorName` from the SP |
| `{ExpiryDate}` | `RenewalDate`, formatted `dd MMM yyyy` |

Also supported: `{ClientName}`, `{ClientId}`, `{PolicyNumber}`, `{PolicyRefNo}`, `{RenewalDate}`, `{Days}`.

## 4. Database

Run against **ERP_IBS**, in order; both scripts are re-runnable:

- [01_TermInsuranceNotification_Tables.sql](Database/01_TermInsuranceNotification_Tables.sql)
  - `tbl_client_notification_details` — one rule per interval, seeded with 60 / 30 / 15.
    `DaysBeforeExpiry` is the join key against the SP output. `CC_Email` is **NULL** —
    the CLRT list goes there when known.
  - `tbl_term_insurance_notification_log` — one row per attempt, the audit trail:
    `SentOn`, `ClientId` (client code), `ClientName`, `AdvisorCode`, `PolicyNumber`,
    `PolicyRefNo`, `ExpiryDate`, `DaysToRenewal`, `ToEmail`, `CcEmail`, `BccEmail`,
    `EmailSubject`, `IsSuccess`, `ErrorMessage`, `IsTestingEmail`.
    De-dup guard: `UNIQUE (PolicyNumber, IntervalCode) WHERE IsSuccess = 1 AND IsTestingEmail = 0`.
    Failures can retry; test runs never block production.
  - `vw_term_insurance_notification_sent` — the successful sends only.
    The sent HTML is deliberately not stored; the template is reproducible from
    `IntervalCode` plus the logged client and policy values.
- [02_sp_GetTermInsuranceRenewalsNotification.sql](Database/02_sp_GetTermInsuranceRenewalsNotification.sql)
  - No parameters. Returns all offsets in `(15, 30, 60, 90, 120, 365)` in one result set.
  - Filters `polctgcod=TI`, `polsts IN (ACT, PAID, PAR_SUR)`, advisor `Status=1`, `polrendt >= GETDATE()`.
  - Returns `b.actnam AS AdvisorName`, which feeds the `{AdvisorName}` token.
  - The offset list has **120** (4 months) but not **180** (6 months), which the requirement asks for.

## 5. Testing switch

```json
"IsTesting": true,
"IsTestingEmail": true,
"TestEmails": "techsupport2@cfsgroup.com",
```

When true, [EmailsAndSMS.cs:81-87](Helper/EmailsAndSMS.cs#L81-L87) sends **only** to `TestEmails`:
To = `TestEmails`, CC and BCC cleared, subject prefixed `"Testing - "`.
Set both to `false` for production. Never bypass this.

## 6. Conventions

- .NET 8, nullable enabled, `Microsoft.Data.SqlClient`. No EF, no extra frameworks.
- Namespace root `TermInsuranceNotification`.
- Repository readers tolerate a missing column (empty/0/null) instead of throwing.
- `ILogger` everything; prefix interval logs with `[{IntervalCode}]`.
- Secrets currently sit in [appsettings.json](appsettings.json) — do not add new ones there.

## 7. Commands

```bash
dotnet build
dotnet run
```

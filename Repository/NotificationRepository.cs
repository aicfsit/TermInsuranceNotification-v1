using Microsoft.Data.SqlClient;
using System.Data;
using TermInsuranceNotification.Abstractions;
using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Repository
{
    /// <summary>
    /// SQL Server implementation: reads the interval configs, executes the
    /// EXEC statement of each config and maintains the sent log.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private const int CommandTimeoutSeconds = 120;

        private readonly string _connectionString;

        public NotificationRepository(string connectionString) => _connectionString = connectionString;

        public List<NotificationConfig> GetActiveConfigs()
        {
            const string sql = @"
                SELECT ReportId,
                       ReportName,
                       IntervalCode,
                       DaysBeforeExpiry,
                       TemplateFile,
                       Email_Body    AS EmailBody,
                       Email_Subject AS EmailSubject,
                       CC_Email      AS CcEmail,
                       Bcc_Email     AS BccEmail
                FROM   dbo.tbl_client_notification_details
                WHERE  Status = 1
                ORDER  BY DaysBeforeExpiry DESC";

            var configs = new List<NotificationConfig>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
            connection.Open();

            using var reader = command.ExecuteReader();
            var columns = GetColumns(reader);

            while (reader.Read())
            {
                configs.Add(new NotificationConfig
                {
                    ReportId = ReadInt(reader, columns, "ReportId"),
                    ReportName = ReadString(reader, columns, "ReportName"),
                    IntervalCode = ReadString(reader, columns, "IntervalCode"),
                    DaysBeforeExpiry = ReadInt(reader, columns, "DaysBeforeExpiry"),
                    TemplateFile = ReadString(reader, columns, "TemplateFile"),
                    EmailBody = ReadString(reader, columns, "EmailBody"),
                    EmailSubject = ReadString(reader, columns, "EmailSubject"),
                    CcEmail = ReadString(reader, columns, "CcEmail"),
                    BccEmail = ReadString(reader, columns, "BccEmail")
                });
            }

            return configs;
        }

        /// <summary>
        /// Runs the configured EXEC statement. One call returns every due policy
        /// for all intervals; DaysToRenewal identifies which reminder each row is.
        /// </summary>
        public List<ClientRecipient> GetRecipients(string queryToExecute)
        {
            var recipients = new List<ClientRecipient>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(queryToExecute, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = CommandTimeoutSeconds
            };
            connection.Open();

            using var reader = command.ExecuteReader();
            var columns = GetColumns(reader);

            while (reader.Read())
            {
                recipients.Add(new ClientRecipient
                {
                    ClientId = ReadString(reader, columns, "ClientId"),
                    ClientName = ReadString(reader, columns, "ClientName"),
                    Email = ReadString(reader, columns, "Email"),
                    PolicyNumber = ReadString(reader, columns, "PolicyNumber"),
                    PolicyRefNo = ReadString(reader, columns, "PolicyRefNo"),
                    RenewalDate = ReadDate(reader, columns, "RenewalDate"),
                    DaysToRenewal = ReadInt(reader, columns, "DaysToRenewal"),
                    AdvisorCode = ReadString(reader, columns, "AdvisorCode"),
                    AdvisorEmailServicing = ReadString(reader, columns, "AdvisorEmailServicing"),
                    AdvisorEmail = ReadString(reader, columns, "AdvisorEmail"),
                    AdvisorName = ReadString(reader, columns, "AdvisorName")
                });
            }

            return recipients;
        }

        /// <summary>Testing mails are ignored here so a test run never blocks a real one.</summary>
        public bool IsAlreadySent(string policyNumber, string intervalCode)
        {
            const string sql = @"
                SELECT TOP 1 1
                FROM   dbo.tbl_term_insurance_notification_log
                WHERE  PolicyNumber   = @PolicyNumber
                  AND  IntervalCode   = @IntervalCode
                  AND  IsSuccess      = 1
                  AND  IsTestingEmail = 0";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
            command.Parameters.AddWithValue("@PolicyNumber", policyNumber ?? string.Empty);
            command.Parameters.AddWithValue("@IntervalCode", intervalCode ?? string.Empty);

            connection.Open();
            return command.ExecuteScalar() != null;
        }

        public void LogAttempt(NotificationLog log)
        {
            const string sql = @"
                INSERT INTO dbo.tbl_term_insurance_notification_log
                    (ReportId, IntervalCode, PolicyNumber, PolicyRefNo, ClientId, ClientName,
                     AdvisorCode, ToEmail, CcEmail, BccEmail, ExpiryDate, DaysToRenewal,
                     EmailSubject, IsSuccess, ErrorMessage, IsTestingEmail)
                VALUES
                    (@ReportId, @IntervalCode, @PolicyNumber, @PolicyRefNo, @ClientId, @ClientName,
                     @AdvisorCode, @ToEmail, @CcEmail, @BccEmail, @ExpiryDate, @DaysToRenewal,
                     @EmailSubject, @IsSuccess, @ErrorMessage, @IsTestingEmail)";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };

            command.Parameters.AddWithValue("@ReportId", log.ReportId);
            command.Parameters.AddWithValue("@IntervalCode", log.IntervalCode ?? string.Empty);
            command.Parameters.AddWithValue("@PolicyNumber", log.PolicyNumber ?? string.Empty);
            command.Parameters.AddWithValue("@PolicyRefNo", ToDb(log.PolicyRefNo));
            command.Parameters.AddWithValue("@ClientId", ToDb(log.ClientId));
            command.Parameters.AddWithValue("@ClientName", ToDb(log.ClientName));
            command.Parameters.AddWithValue("@AdvisorCode", ToDb(log.AdvisorCode));
            command.Parameters.AddWithValue("@ToEmail", ToDb(log.ToEmail));
            command.Parameters.AddWithValue("@CcEmail", ToDb(log.CcEmail));
            command.Parameters.AddWithValue("@BccEmail", ToDb(log.BccEmail));
            command.Parameters.AddWithValue("@ExpiryDate", (object?)log.ExpiryDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@DaysToRenewal", log.DaysToRenewal);
            command.Parameters.AddWithValue("@EmailSubject", ToDb(log.EmailSubject));
            command.Parameters.AddWithValue("@IsSuccess", log.IsSuccess);
            command.Parameters.AddWithValue("@ErrorMessage", ToDb(log.ErrorMessage));
            command.Parameters.AddWithValue("@IsTestingEmail", log.IsTestingEmail);

            connection.Open();
            command.ExecuteNonQuery();
        }

        // ---- reader helpers: a missing column yields the default, never an exception ----

        private static HashSet<string> GetColumns(SqlDataReader reader)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));
            return columns;
        }

        private static string ReadString(SqlDataReader reader, HashSet<string> columns, string column)
        {
            if (!columns.Contains(column)) return string.Empty;
            var i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? string.Empty : reader.GetValue(i).ToString() ?? string.Empty;
        }

        private static int ReadInt(SqlDataReader reader, HashSet<string> columns, string column)
        {
            if (!columns.Contains(column)) return 0;
            var i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? 0 : Convert.ToInt32(reader.GetValue(i));
        }

        private static DateTime? ReadDate(SqlDataReader reader, HashSet<string> columns, string column)
        {
            if (!columns.Contains(column)) return null;
            var i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : Convert.ToDateTime(reader.GetValue(i));
        }

        private static object ToDb(string? value) =>
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }
}

using Microsoft.Data.SqlClient;
using System.Data;
using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Repository
{
    /// <summary>
    /// Reads notification setup from tbl_client_notification_details and
    /// executes each config's QueryToExecute (an EXEC ... statement) to get recipients.
    /// </summary>
    public class NotificationRepository
    {
        private readonly string _connectionString;

        public NotificationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>Returns all active (Status = 1) notification configs.</summary>
        public List<NotificationConfig> GetActiveConfigs()
        {
            var configs = new List<NotificationConfig>();

            const string sql = @"
                SELECT ReportId,
                       ReportName,
                       QueryToExecute,
                       Email_Body    AS EmailBody,
                       Email_Subject AS EmailSubject,
                       CC_Email      AS CcEmail,
                       Bcc_Email     AS BccEmail
                FROM tbl_client_notification_details
                WHERE Status = 1";

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, con);
            con.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                configs.Add(new NotificationConfig
                {
                    ReportId = reader.GetInt32(reader.GetOrdinal("ReportId")),
                    ReportName = GetString(reader, "ReportName"),
                    QueryToExecute = GetString(reader, "QueryToExecute"),
                    EmailBody = GetString(reader, "EmailBody"),
                    EmailSubject = GetString(reader, "EmailSubject"),
                    CcEmail = GetString(reader, "CcEmail"),
                    BccEmail = GetString(reader, "BccEmail")
                });
            }
            return configs;
        }

        /// <summary>
        /// Executes the config's QueryToExecute (e.g. "EXEC sp_GetTermInsuranceRenewalsNotification 6, 'MONTH'")
        /// and maps the returned rows to ClientRecipient.
        /// </summary>
        public List<ClientRecipient> GetRecipients(string queryToExecute)
        {
            var recipients = new List<ClientRecipient>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(queryToExecute, con)
            {
                CommandType = CommandType.Text,   // stored as a full "EXEC ..." string
                CommandTimeout = 120
            };
            con.Open();
            using var reader = cmd.ExecuteReader();

            var cols = GetColumnSet(reader);

            while (reader.Read())
            {
                recipients.Add(new ClientRecipient
                {
                    ClientId = ReadStr(reader, cols, "ClientId"),
                    ClientName = ReadStr(reader, cols, "ClientName"),
                    Email = ReadStr(reader, cols, "Email"),
                    PolicyNumber = ReadStr(reader, cols, "PolicyNumber"),
                    RenewalDate = ReadDate(reader, cols, "RenewalDate"),
                    DaysToRenewal = ReadInt(reader, cols, "DaysToRenewal"),
                    AdvisorEmailServicing = ReadStr(reader, cols, "AdvisorEmailServicing"),
                    AdvisorEmail = ReadStr(reader, cols, "AdvisorEmail")
                });
            }
            return recipients;
        }

        // ---- helpers -------------------------------------------------------

        private static HashSet<string> GetColumnSet(SqlDataReader reader)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                set.Add(reader.GetName(i));
            return set;
        }

        private static string GetString(SqlDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? string.Empty : r.GetValue(i).ToString() ?? string.Empty;
        }

        private static string ReadStr(SqlDataReader r, HashSet<string> cols, string col)
        {
            if (!cols.Contains(col)) return string.Empty;
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? string.Empty : r.GetValue(i).ToString() ?? string.Empty;
        }

        private static int ReadInt(SqlDataReader r, HashSet<string> cols, string col)
        {
            if (!cols.Contains(col)) return 0;
            int i = r.GetOrdinal(col);
            if (r.IsDBNull(i)) return 0;
            return Convert.ToInt32(r.GetValue(i));
        }

        private static DateTime? ReadDate(SqlDataReader r, HashSet<string> cols, string col)
        {
            if (!cols.Contains(col)) return null;
            int i = r.GetOrdinal(col);
            if (r.IsDBNull(i)) return null;
            return Convert.ToDateTime(r.GetValue(i));
        }
    }
}

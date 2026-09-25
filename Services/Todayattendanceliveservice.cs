using System.Data;
using Microsoft.Data.SqlClient;

namespace TSSC.Unified.Services
{
    public interface ITodayAttendanceLiveService
    {
        Task<(DateTime? InTime, DateTime? OutTime)> GetTodayAttendanceAsync(string employeeCode);
    }

    public class TodayAttendanceLiveService : ITodayAttendanceLiveService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TodayAttendanceLiveService> _logger;

        public TodayAttendanceLiveService(IConfiguration configuration, ILogger<TodayAttendanceLiveService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(DateTime? InTime, DateTime? OutTime)> GetTodayAttendanceAsync(string employeeCode)
        {
            DateTime? inTime = null;
            DateTime? outTime = null;

            try
            {
                var connectionString = _configuration.GetConnectionString("BiometricConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogError("BiometricConnection connection string is missing.");
                    return (null, null);
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var today = DateTime.Today;
                var tableName = $"DeviceLogs_{today.Month}_{today.Year}";

                var sql = $@"
                    SELECT
                        MIN(LogDate) AS InTime,
                        CASE
                            WHEN MAX(LogDate) >= DATEADD(MINUTE, 30, MIN(LogDate))
                            THEN MAX(LogDate)
                            ELSE NULL
                        END AS OutTime
                    FROM [{tableName}]
                    WHERE UserId = @EmployeeCode
                      AND LogDate >= @Today
                      AND LogDate < DATEADD(DAY, 1, @Today);";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@EmployeeCode", SqlDbType.VarChar, 50).Value = employeeCode;
                command.Parameters.Add("@Today", SqlDbType.DateTime).Value = today;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    if (reader["InTime"] != DBNull.Value)
                        inTime = Convert.ToDateTime(reader["InTime"]);
                    if (reader["OutTime"] != DBNull.Value)
                        outTime = Convert.ToDateTime(reader["OutTime"]);
                }
            }
            catch (SqlException ex) when (ex.Number == 208) // Invalid object name — today's monthly table doesn't exist yet
            {
                _logger.LogWarning(ex, "Attendance table not found for EmployeeCode: {EmployeeCode}", employeeCode);
                return (null, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Biometric database error while getting today's attendance for EmployeeCode: {EmployeeCode}", employeeCode);
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting today's attendance for EmployeeCode: {EmployeeCode}", employeeCode);
                return (null, null);
            }

            return (inTime, outTime);
        }
    }
}

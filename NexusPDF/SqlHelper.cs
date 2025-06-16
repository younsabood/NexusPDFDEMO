using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusPDF
{
    public class SqlHelper : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed;

        public SqlHelper(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            _connectionString = connectionString;
        }

        public async Task<int> ExecuteNonQueryAsync(string query, SqlParameter[] parameters = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);

                        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(ExecuteNonQueryAsync));
                throw;
            }
        }

        public async Task<object> ExecuteScalarAsync(string query, SqlParameter[] parameters = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);

                        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(ExecuteScalarAsync));
                throw;
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, SqlParameter[] parameters = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);

                        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                        {
                            var dt = new DataTable();
                            dt.Load(reader); // synchronous but no async alternative
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(ExecuteQueryAsync));
                throw;
            }
        }

        private void LogError(Exception ex, string methodName)
        {
            // Capture the current time for better traceability
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create a more detailed error message for the MessageBox
            string errorMessage = $"An error occurred in method: {methodName}\n" +
                                  $"Timestamp: {timestamp}\n\n" +
                                  $"Error Message: {ex.Message}\n" +
                                  $"Stack Trace:\n{ex.StackTrace}\n\n" +
                                  $"Source: {ex.Source}\n" +
                                  $"Inner Exception: {ex.InnerException?.Message ?? "None"}";

            // Display the error in a MessageBox
            MessageBox.Show(errorMessage, "Error Occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources if needed
                }
                _disposed = true;
            }
        }

        ~SqlHelper()
        {
            Dispose(false);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusPDF
{
    internal class APPUpdater
    {
        private readonly SqlHelper _sqlHelper = new SqlHelper(Properties.Settings.Default.ConnectionString);
        public async Task CompareAndUpdateJsonAsync()
        {
            string jsonFromUrl = null;
            string jsonFromDb = null;

            try
            {
                jsonFromUrl = await UrlContentReader.ReadContentFromUrlAsync(Api.Release);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading JSON from URL: {ex.Message}", "URL Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                jsonFromDb = await GetFirstJsonFromDbAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading JSON from database: {ex.Message}", "Database Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (jsonFromUrl != null && jsonFromUrl != jsonFromDb)
            {
                await InsertJsonInDbAsync(jsonFromUrl);
            }
        }

        private async Task<string> GetFirstJsonFromDbAsync()
        {
            string query = "SELECT TOP 1 [json] FROM [api].[Json] ORDER BY [Id] ASC;";
            object result = await _sqlHelper.ExecuteScalarAsync(query);
            return result?.ToString();
        }

        private async Task InsertJsonInDbAsync(string newJson)
        {
            string deleteQuery = "DELETE FROM [api].[Json];";
            await _sqlHelper.ExecuteNonQueryAsync(deleteQuery);

            string insertQuery = "INSERT INTO [api].[Json] ([json]) VALUES (@json);";
            SqlParameter[] parameters = new SqlParameter[]
            {
            new SqlParameter("@json", System.Data.SqlDbType.NVarChar, -1) { Value = newJson }
            };
            await _sqlHelper.ExecuteNonQueryAsync(insertQuery, parameters);
        }
    }
}

using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using static System.Resources.ResXFileRef;

namespace NexusPDF
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                var initTask = InitializeApplicationAsync();
                initTask.Wait(); 

                RunApplication();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"A fatal error occurred: {ex.Message}", "Application Error");
                Application.Exit();
            }
        }

        private static async Task InitializeApplicationAsync()
        {

            // Check internet connectivity and validate location
            await ValidateLocationAccessAsync();

            ConfigureDatabaseConnection();
            ValidateTrialPeriod();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        }

        private static async Task ValidateLocationAccessAsync()
        {
            try
            {
                var (publicIp, country, isBanned) = await IPLocationService.GetIpCountryAndBannedStatus();

                if (isBanned)
                {
                    ShowErrorMessage(
                        $"Access to this application is not available in {country}. If you believe this is an error, please contact support. Alternatively, you may try using a VPN to access the application.",
                        "Access Restricted");
                    Environment.Exit(0);
                }

            }
            catch (Exception ex)
            {
                ShowErrorMessage(
                    "Unable to verify location access. Please check your internet connection and try again.",
                    "Location Verification Failed : " + ex);
                Environment.Exit(0);
            }
        }

        private static void RunApplication()
        {
            using (var splashScreen = new splash())
            {
                DialogResult splashResult = splashScreen.ShowDialog();

                if (splashResult == DialogResult.OK)
                {
                    Application.Run(new Home());
                }
                else
                {
                    HandleLogin();
                }
            }
        }

        private static void HandleLogin()
        {
            using (var loginPage = new LoginPage())
            {
                if (loginPage.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new Home());
                }
                else
                {
                    Application.Exit();
                }
            }
        }

        private static void ValidateTrialPeriod()
        {
            try
            {
                var trialManager = new TrialManager();
                var trialStatus = trialManager.GetTrialStatus();

                if (trialStatus.HasExpired)
                {
                    ShowErrorMessage(
                        $"Your {trialStatus.TrialMonths}-month trial has expired. Please purchase a license.",
                        "Trial Expired");
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to validate trial period", ex);
            }
        }

        private static void ConfigureDatabaseConnection()
        {
            try
            {
                var dbConfig = new DatabaseConfiguration();
                dbConfig.Initialize();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Database configuration failed", ex);
            }
        }

        private static void ShowErrorMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    public class TrialManager
    {
        private const int TRIAL_MONTHS = 2;

        public TrialStatus GetTrialStatus()
        {
            var startDate = GetTrialStartDate();
            var currentDate = DateTime.Today;

            if (startDate == null)
            {
                startDate = currentDate;
                SaveTrialStartDate(startDate.Value);
                LogInfo("New trial period started");
            }

            var expirationDate = startDate.Value.AddMonths(TRIAL_MONTHS);
            var daysRemaining = (expirationDate - currentDate).Days;
            var hasExpired = currentDate > expirationDate;

            return new TrialStatus
            {
                StartDate = startDate.Value,
                ExpirationDate = expirationDate,
                DaysRemaining = Math.Max(0, daysRemaining),
                HasExpired = hasExpired,
                TrialMonths = TRIAL_MONTHS
            };
        }

        private DateTime? GetTrialStartDate()
        {
            const string query = "SELECT TOP 1 StartDate FROM auth.TrialInfo ORDER BY StartDate DESC";

            using (var conn = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    var result = cmd.ExecuteScalar();

                    return result == DBNull.Value || result == null
                        ? (DateTime?)null
                        : Convert.ToDateTime(result);
                }
            }
        }

        private void SaveTrialStartDate(DateTime startDate)
        {
            const string query = "INSERT INTO auth.TrialInfo (StartDate) VALUES (@StartDate)";

            using (var conn = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    LogInfo($"Trial start date saved: {startDate:yyyy-MM-dd}");
                }
            }
        }

        private static void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - TrialManager: {message}");
        }
    }

    public class TrialStatus
    {
        public DateTime StartDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool HasExpired { get; set; }
        public int TrialMonths { get; set; }
    }

    public class DatabaseConfiguration
    {
        public void Initialize()
        {
            var connectionString = BuildConnectionString();
            Properties.Settings.Default.ConnectionString = connectionString;

            ValidateConnection();
            LogInfo("Database configuration completed successfully");
        }

        private string BuildConnectionString()
        {
            var currentDirectory = Environment.CurrentDirectory;
            var databasePath = Path.Combine(currentDirectory, "NexusPDFDB.mdf");

            return $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={databasePath};Integrated Security=True;Connect Timeout=30;";
        }

        private void ValidateConnection()
        {
            try
            {
                using (var conn = new SqlConnection(Properties.Settings.Default.ConnectionString))
                {
                    conn.Open();
                    LogInfo("Database connection validated successfully");
                }
            }
            catch (Exception ex)
            {
                LogError($"Database connection validation failed: {ex.Message}");
                throw new ApplicationException("Cannot connect to database", ex);
            }
        }

        private static void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - DatabaseConfig: {message}");
        }

        private static void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - DatabaseConfig: {message}");
        }
    }
}
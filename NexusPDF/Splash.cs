using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusPDF
{
    public partial class splash : Form
    {
        public static SqlHelper sqlHelper = new SqlHelper(Properties.Settings.Default.ConnectionString);
        private bool isLoginCheckCompleted = false;
        private DialogResult loginResult = DialogResult.Abort;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED for smooth rendering
                return cp;
            }
        }

        public splash()
        {
            InitializeComponent();

            splashTimer.Tick += SplashTimer_Tick;
            splashTimer.Start();

            _ = CheckLoginAsync();

            this.Resize += Splash_Resize;
        }

        private void SplashTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (progressBar.Value < progressBar.Maximum)
                {
                    progressBar.Value += 1;
                    progressBar.Text = progressBar.Value.ToString();
                }
                else
                {
                    splashTimer.Stop();
                    if (isLoginCheckCompleted)
                    {
                        this.DialogResult = loginResult;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحديث شاشة البداية: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CheckLoginAsync()
        {
            try
            {
                string query = "SELECT * FROM [auth].[Users]";
                DataTable result = await sqlHelper.ExecuteQueryAsync(query);

                loginResult = (result.Rows.Count > 0) ? DialogResult.OK : DialogResult.Abort;
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء التحقق من حالة تسجيل الدخول: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                loginResult = DialogResult.Abort;
            }
            finally
            {
                isLoginCheckCompleted = true;
                if (progressBar.Value >= progressBar.Maximum)
                {
                    this.DialogResult = loginResult;
                    this.Close();
                }
            }
        }


        private void Splash_Resize(object sender, EventArgs e)
        {
            if (progressBar.Height > 0)
            {
                progressBar.BorderRadius = Math.Max(10, progressBar.Height / 2 - 2);
            }
            float newFontSize = Math.Max(10f, progressBar.Height / 2.5f);
            progressBar.Font = new System.Drawing.Font(progressBar.Font.FontFamily, newFontSize, System.Drawing.FontStyle.Regular);
        }
    }
}

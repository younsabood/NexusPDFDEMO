using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace NexusPDF
{
    public partial class ShowVersion : Form
    {
        public ShowVersion()
        {
            InitializeComponent();
        }

        private async void ShowVersion_Load(object sender, EventArgs e)
        {
            string json = await UrlContentReader.ReadContentFromUrlAsync(Api.Release);
            version version = new version
            {
                Json = json
            };
            version.Dock = DockStyle.Fill;
            this.Controls.Add(version);
        }
    }
}
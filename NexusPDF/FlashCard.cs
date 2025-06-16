using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusPDF
{
    public partial class FlashCard : UserControl
    {
        private WebView2 webView;
        private string userDataFolder;
        private string _Explanation;
        private string _Verbatim;
        private string _Source;
        private string _Subject;
        private bool _isWebViewInitialized = false;

        public FlashCard()
        {
            InitializeComponent();
            try
            {
                userDataFolder = Path.Combine(Path.GetTempPath(), "WebView2_FlashCard_" + Guid.NewGuid().ToString());
                this.Height = 650;
                this.Width = 600;
                this.MaximumSize = new System.Drawing.Size(0, 650);
                InitializeWebView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing FlashCard UserControl: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string QuestionText { get; set; } = "Question goes here.";

        public string Explanation
        {
            get => _Explanation;
            set
            {
                _Explanation = value;
                RenderHtmlIfInitialized();
            }
        }

        public string Verbatim
        {
            get => _Verbatim;
            set
            {
                _Verbatim = value;
                RenderHtmlIfInitialized();
            }
        }

        public string Source
        {
            get => _Source;
            set
            {
                _Source = value;
                RenderHtmlIfInitialized();
            }
        }

        public string Subject
        {
            get => _Subject;
            set
            {
                _Subject = value;
                RenderHtmlIfInitialized();
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                webView = new WebView2 { Dock = DockStyle.Fill };
                Controls.Add(webView);

                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                }

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                // Add message handler for JavaScript communication
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                _isWebViewInitialized = true;
                RenderHtml();

                this.Disposed += (sender, e) =>
                {
                    try
                    {
                        if (Directory.Exists(userDataFolder))
                        {
                            Directory.Delete(userDataFolder, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deleting WebView2 user data folder: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView: {ex.Message}", "WebView Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _isWebViewInitialized = false;
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();

            if (message == "openPdfDirectory")
            {
                OpenPdfDirectory();
            }
        }

        private void OpenPdfDirectory()
        {
            try
            {
                if (!string.IsNullOrEmpty(PdfSplitter.CurrentPdfDirectory) && Directory.Exists(PdfSplitter.CurrentPdfDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", PdfSplitter.CurrentPdfDirectory);
                }
                else
                {
                    MessageBox.Show("No PDF directory available. Please split a PDF first.",
                                  "Directory Not Found",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open directory:\n{ex.Message}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private void RenderHtmlIfInitialized()
        {
            if (_isWebViewInitialized)
            {
                RenderHtml();
            }
        }

        private void RenderHtml()
        {
            if (webView?.CoreWebView2 == null)
            {
                System.Diagnostics.Debug.WriteLine("WebView2.CoreWebView2 is not initialized when RenderHtml is called.");
                return;
            }

            string sanitizedQuestion = System.Net.WebUtility.HtmlEncode(QuestionText);
            string sanitizedExplanation = System.Net.WebUtility.HtmlEncode(Explanation);
            string sanitizedVerbatim = System.Net.WebUtility.HtmlEncode(Verbatim);
            string sanitizedSource = System.Net.WebUtility.HtmlEncode(Source);
            string sanitizedSubject = System.Net.WebUtility.HtmlEncode(Subject);

            var html = $@"
            <!DOCTYPE html>
            <html lang='ar' dir='auto'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Flashcard</title>
                <style>
                    {GetStyles()}
                </style>
            </head>
            <body>
                <div class='flip-container' id='flipCard'>
                    <div class='card'>
                        <div class='card-face card-front'>
                            <div class='question' id='questionText'>{sanitizedQuestion}</div>
                            <button class='flip-btn' onclick='flipCard()'>Check Answer</button>
                        </div>
                        <div class='card-face card-back'>
                            <div class='subject-text' id='subjectText'>{sanitizedSubject}</div>
                            <hr>
                            <div class='explanation-container'>
                                <div class='explanation-label'>Explanation:</div>
                                <div class='explanation-text' id='explanationText'>{sanitizedExplanation}</div>
                                <div class='verbatim-label'>Verbatim:</div>
                                <div class='verbatim-text' id='verbatimText'>{sanitizedVerbatim}</div>
                            </div>
                            <div class='source-box' id='sourceText' onclick='openPdfDirectory()'>{sanitizedSource}</div>
                            <button class='flip-btn back-btn' onclick='flipBack()'>Back to Question</button>
                        </div>
                    </div>
                </div>

                <script>
                    function flipCard() {{
                        document.getElementById('flipCard').classList.add('flipped');
                    }}
                    
                    function flipBack() {{
                        document.getElementById('flipCard').classList.remove('flipped');
                    }}
                    
                    function openPdfDirectory() {{
                        window.chrome.webview.postMessage('openPdfDirectory');
                    }}
                </script>
            </body>
            </html>";

            try
            {
                webView.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navigating WebView2 with HTML: {ex.Message}", "HTML Rendering Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetStyles()
        {
            return @"
            /* Global reset and remove ALL scrollbars */
            * {
                box-sizing: border-box;
                margin: 0;
                padding: 0;
                scrollbar-width: none;
                -ms-overflow-style: none;
            }
    
            *::-webkit-scrollbar {
                display: none;
            }

            html {
                height: 100%;
                overflow: hidden !important;
                scrollbar-width: none;
                -ms-overflow-style: none;
            }
    
            html::-webkit-scrollbar {
                display: none;
            }

            body {
                font-family: 'Segoe UI', 'Arial', 'Tahoma', 'Noto Sans Arabic', 'Traditional Arabic', sans-serif;
                background: #fcfaf5;
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                height: 650px;
                max-height: 650px;
                padding: 10px;
                color: #333;
                line-height: 1.6;
                direction: auto;
                unicode-bidi: embed;
                overflow: hidden !important;
                scrollbar-width: none;
                scrollbar-height: none;
                -ms-overflow-style: none;
            }
    
            body::-webkit-scrollbar {
                display: none;
            }

            .flip-container {
                perspective: 1000px;
                width: 100%;
                max-width: 900px;
                height: 100%;
                max-height: 630px;
                display: flex;
                overflow: hidden;
                align-items: center;
                justify-content: center;
            }

            .card {
                width: 100%;
                height: 100%;
                max-height: 630px;
                transition: transform .6s ease-in-out;
                transform-style: preserve-3d;
                position: relative;
            }

            .flip-container.flipped .card {
                transform: rotateY(180deg);
            }

            .card-face {
                background: #fff;
                border-radius: 16px;
                box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
                position: absolute;
                width: 100%;
                height: 100%;
                max-height: 630px;
                backface-visibility: hidden;
                padding: 30px;
                display: flex;
                flex-direction: column;
                -webkit-backface-visibility: hidden;
                overflow: hidden;
                direction: auto;
                unicode-bidi: embed;
            }

            .card-front {
                transform: rotateY(0deg);
                z-index: 2;
                align-items: center;
                justify-content: center;
                text-align: center;
            }

            .card-back {
                transform: rotateY(180deg);
                justify-content: flex-start;
            }

            .question {
                font-size: 1.6rem;
                font-weight: 500;
                text-align: center;
                margin-bottom: 20px;
                flex-grow: 1;
                display: flex;
                align-items: center;
                justify-content: center;
                overflow: hidden;
                word-wrap: break-word;
                overflow-wrap: break-word;
                padding: 10px;
                direction: auto;
                unicode-bidi: embed;
                white-space: pre-wrap;
            }

            .subject-text {
                color: #333;
                font-size: 1.2rem;
                font-weight: 600;
                margin-bottom: 15px;
                text-align: center;
                padding-top: 10px;
                flex-shrink: 0;
                direction: auto;
                unicode-bidi: embed;
                white-space: pre-wrap;
            }

            .explanation-container {
                flex-grow: 1;
                overflow-y: auto;
                overflow-x: hidden;
                padding-right: 10px;
                margin-bottom: 20px;
                min-height: 0;
                max-height: calc(630px - 200px);
                scrollbar-width: thin;
                direction: auto;
                unicode-bidi: embed;
            }

            .explanation-label, .verbatim-label {
                font-weight: bold;
                margin-top: 10px;
                margin-bottom: 5px;
                color: #555;
                font-size: 0.95rem;
                direction: ltr;
                text-align: left;
            }

            .explanation-text, .verbatim-text {
                font-size: 1rem;
                line-height: 1.7;
                color: #444;
                margin-bottom: 10px;
                word-wrap: break-word;
                overflow-wrap: break-word;
                direction: auto;
                unicode-bidi: embed;
                text-align: start;
                white-space: pre-wrap;
                font-feature-settings: 'liga' 1, 'kern' 1;
                -webkit-font-feature-settings: 'liga' 1, 'kern' 1;
                -moz-font-feature-settings: 'liga' 1, 'kern' 1;
            }

            /* Special handling for mixed content */
            .explanation-text p, .verbatim-text p {
                direction: auto;
                unicode-bidi: embed;
                margin-bottom: 8px;
            }

            /* RTL specific adjustments */
            [dir='rtl'] .explanation-text,
            [dir='rtl'] .verbatim-text {
                text-align: right;
            }

            /* LTR specific adjustments */
            [dir='ltr'] .explanation-text,
            [dir='ltr'] .verbatim-text {
                text-align: left;
            }

            .source-box {
                position: absolute;
                bottom: 70px;
                left: 20px;
                background: #bb9b55;
                color: white;
                padding: 6px 12px;
                border-radius: 4px;
                font-size: 0.9rem;
                font-weight: 500;
                max-width: calc(100% - 40px);
                word-wrap: break-word;
                cursor: pointer;
                transition: background-color 0.3s ease;
                user-select: none;
                direction: auto;
                unicode-bidi: embed;
                white-space: pre-wrap;
            }
    
            .source-box:hover {
                background: #a16f1b;
            }

            .back-btn, .flip-btn {
                font-size: 1rem;
                text-align: center;
                margin-top: 20px;
                display: inline-block;
                padding: 12px 24px;
                font-weight: 600;
                border: none;
                border-radius: 10px;
                cursor: pointer;
                transition: background .3s, box-shadow .3s;
                align-self: center;
                flex-shrink: 0;
            }

            .flip-btn {
                background: #bb9b55;
                color: #fff;
            }

            .flip-btn:focus, .flip-btn:hover {
                background: #a16f1b;
                box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);
            }

            .back-btn {
                background: #e0e0e0;
                color: #333;
            }

            .back-btn:focus, .back-btn:hover {
                background: #ccc;
                box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
            }

            .explanation-container::-webkit-scrollbar {
                width: 6px;
            }

            .explanation-container::-webkit-scrollbar-track {
                background: #f1f1f1;
                border-radius: 3px;
            }

            .explanation-container::-webkit-scrollbar-thumb {
                background: #bb9b55;
                border-radius: 3px;
            }

            .explanation-container::-webkit-scrollbar-thumb:hover {
                background: #a16f1b;
            }

            @media (max-width: 768px) {
                .question {
                    font-size: 1.4rem;
                }

                .explanation-text, .verbatim-text {
                    font-size: 0.9rem;
                    line-height: 1.6;
                }

                .back-btn, .flip-btn {
                    width: 100%;
                    padding: 12px;
                }

                .card-face {
                    padding: 20px;
                }

                .source-box {
                    bottom: 60px;
                }
            }

            /* Improve Arabic text rendering */
            @supports (font-variant-ligatures: common-ligatures) {
                .explanation-text, .verbatim-text, .question, .subject-text {
                    font-variant-ligatures: common-ligatures;
                }
            }

            /* Ensure proper text spacing for mixed content */
            .explanation-text code, .verbatim-text code {
                font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
                background-color: #f5f5f5;
                padding: 2px 4px;
                border-radius: 3px;
                direction: ltr;
                unicode-bidi: embed;
            }";
        }
    }
}
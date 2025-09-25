using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using GenerativeAI.Types;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Security;
using System.Collections.Generic;

namespace NexusPDF
{
    public partial class HtmlMultipleChoiceCode : UserControl
    {
        private WebView2 webView;
        private string userDataFolder;
        private string[] _options;
        private string _correctAnswer;
        private string _Explanation;
        private string _Difficulty;
        public decimal QAencrement;
        private string _Source;
        public static int numOfOption;
        private bool _hasBeenAnsweredCorrectly = false; // Flag to track if answered correctly

        public HtmlMultipleChoiceCode()
        {
            try
            {
                userDataFolder = Path.Combine(Path.GetTempPath(), "WebView2", Guid.NewGuid().ToString());
                InitializeWebView();
                this.Height = 800;
                this.Width = 600;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public string QuestionText { get; set; } = "Question";

        public string CorrectAnswer
        {
            get => _correctAnswer;
            set
            {
                try
                {
                    _correctAnswer = value;
                    RenderHtml();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting Correct Answer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public string Explanation
        {
            get => _Explanation;
            set
            {
                try
                {
                    _Explanation = value;
                    RenderHtml();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting Explanation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        public string Source
        {
            get => _Source;
            set
            {
                try
                {
                    _Source = value;
                    RenderHtml();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting Source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        public string Difficulty
        {
            get => _Difficulty;
            set
            {
                try
                {
                    _Difficulty = value;
                    RenderHtml();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting Difficulty: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                webView = new WebView2 { Dock = DockStyle.Fill };
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);
                webView.CoreWebView2.WebMessageReceived += (sender, args) =>
                {
                    var message = args.TryGetWebMessageAsString();
                    if (message == "correct")
                    {
                        // Only increment counter if this is the first correct answer
                        if (!_hasBeenAnsweredCorrectly)
                        {
                            AI.counter += QAencrement;
                            _hasBeenAnsweredCorrectly = true;
                        }
                    }
                    else if (message == "openPdfDirectory")
                    {
                        OpenPdfDirectory();
                    }
                };
                Controls.Add(webView);
                RenderHtml();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        public string[] Options
        {
            get => _options;
            set
            {
                try
                {
                    if (value == null || value.Length != numOfOption)
                    {
                        throw new ArgumentException("Exactly four options required");
                    }
                    _options = value;
                    RenderHtml();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting Options: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public async Task ResetAsync()
        {
            try
            {
                // Reset the flag when resetting the question
                _hasBeenAnsweredCorrectly = false;

                if (webView?.CoreWebView2 != null)
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(
                        "document.querySelectorAll('input').forEach(i => i.checked = false);");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting WebView: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Add a method to manually reset the answered flag if needed
        public void ResetAnsweredFlag()
        {
            _hasBeenAnsweredCorrectly = false;
        }

        private string GenerateOptions(int numOfOptions)
        {
            string optionsHtml = "";
            for (int i = 0; i < numOfOptions; i++)
            {
                optionsHtml += $"{GenerateOptionHtml(i)}";
            }
            return optionsHtml;
        }

        private string GenerateOptionHtml(int index)
        {
            try
            {
                string formattedOptionText = FormatCodeMarkdown(_options[index]);
                return $@"
    <label class='option' for='option{index}'>
        <input type='radio' name='answer' id='option{index}' value='{SecurityElement.Escape(_options[index])}'>
        <span class='radio-custom'></span>
        <span class='option-text'>{formattedOptionText}</span>
    </label>";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating option HTML: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Converts markdown for code blocks (```) and inline code (`) into HTML tags (<pre><code> and <code>).
        /// This method safely escapes content to prevent HTML injection.
        /// </summary>
        /// <param name="text">The raw text possibly containing markdown for code.</param>
        /// <returns>A string with markdown converted to HTML.</returns>
        private string FormatCodeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var htmlBlocks = new List<string>();
            string placeholderPrefix = $"__CODE_PLACEHOLDER_{Guid.NewGuid()}__";

            // 1. Process block code ```...``` and replace with placeholders
            text = new Regex(@"```(\w*)\r?\n([\s\S]+?)\r?\n```").Replace(text, match => {
                string lang = match.Groups[1].Value;
                string code = match.Groups[2].Value;

                // If no language is specified, default to 'none' for Prism to handle as plain text.
                if (string.IsNullOrEmpty(lang))
                {
                    lang = "none";
                }

                string escapedCode = SecurityElement.Escape(code.Trim());
                // Prism.js uses the class "language-xxxx" for highlighting.
                string html = $"<pre><code class=\"language-{lang}\">{escapedCode}</code></pre>";
                htmlBlocks.Add(html);
                return $"{placeholderPrefix}{htmlBlocks.Count - 1}";
            });

            // 2. Process inline code `...` and replace with placeholders
            text = new Regex(@"`([^`\n]+?)`").Replace(text, match => {
                string code = match.Groups[1].Value;
                string escapedCode = SecurityElement.Escape(code);
                string html = $"<code>{escapedCode}</code>";
                htmlBlocks.Add(html);
                return $"{placeholderPrefix}{htmlBlocks.Count - 1}";
            });

            // 3. Escape the rest of the text to prevent any remaining HTML from rendering
            text = SecurityElement.Escape(text);

            // 4. Put the pristine HTML blocks back into the escaped text
            for (int i = 0; i < htmlBlocks.Count; i++)
            {
                string escapedPlaceholder = SecurityElement.Escape($"{placeholderPrefix}{i}");
                text = text.Replace(escapedPlaceholder, htmlBlocks[i]);
            }

            return text;
        }


        private void RenderHtml()
        {
            try
            {
                if (webView?.CoreWebView2 == null || _options == null || _correctAnswer == null) return;

                string formattedQuestion = FormatCodeMarkdown(QuestionText);
                string formattedCorrectAnswer = FormatCodeMarkdown(CorrectAnswer);
                string formattedExplanation = FormatCodeMarkdown(Explanation);

                var html = $@"
               <!DOCTYPE html>
                <html lang='{(AI.LanguageAI == "Arabic" ? "ar" : "en")}' dir='{(AI.LanguageAI == "Arabic" ? "rtl" : "ltr")}'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Flashcard MCQ</title>
                    <link rel='stylesheet' href='[https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-okaidia.min.css](https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-okaidia.min.css)'>
                    <style>
                        {GetNewStyles()}
                    </style>
                </head>
                <body>
                    <div class='flip-container' id='flipCard'>
                        <div class='card'>
                            <div class='card-face card-front'>
                                <div class='question' id='questionContainer'>
                                    <div class='question-content'>{formattedQuestion}</div>
                                </div>
                                <div class='options'>
                                {(numOfOption > 2 ? GenerateOptions(4) : GenerateOptions(2))}
                                </div>
                                <button class='flip-btn' onclick='flipCard()'>Check Answer</button>
                            </div>
                            <div class='card-face card-back'>
                                <div class='correct-answer'>{formattedCorrectAnswer}</div>
                                <div class='explanation-text'>{formattedExplanation}</div>
                                <hr style=""height:2px;border-width:0;color:gray;background-color:gray"">
                                <div class='source-text' onclick='openPdfDirectory()' title='Click to open PDF directory'>{Source}</div>
                                <div class='difficulty-box'>{Difficulty}</div>
                                <button class='flip-btn back-btn' onclick='flipBack()'>Back to Question</button>
                            </div>
                        </div>
                    </div>
                    <script src='[https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js](https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js)'></script>
                    <script src='[https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js](https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js)'></script>
                    <script>
                        document.addEventListener('DOMContentLoaded', () => {{
                            // C# now pre-formats the HTML with <pre><code class='language-xxx'> blocks,
                            // so we just need to tell Prism.js to find and style them.
                            Prism.highlightAll();
                        }});
                        
                        const correctAnswerRaw = `{SecurityElement.Escape(CorrectAnswer)}`;
                        const correctAnswer = correctAnswerRaw.trim().toLowerCase();

                        function flipCard() {{
                            const selected = document.querySelector('input[name=""answer""]:checked');
                            if (!selected) {{
                                return;
                            }}
                            const options = document.querySelectorAll('.option');

                            // Reset all styles first
                            options.forEach(option => {{
                                option.classList.remove('correct', 'incorrect');
                            }});

                            // Highlight the CORRECT answer
                            options.forEach(option => {{
                                const value = option.querySelector('input').value.trim().toLowerCase();
                                if (value === correctAnswer) {{
                                    option.classList.add('correct');
                                }}
                            }});

                            // Highlight the INCORRECT selection (if any)
                            if (selected && selected.value.trim().toLowerCase() !== correctAnswer) {{
                                const incorrectOption = selected.closest('.option');
                                incorrectOption.classList.add('incorrect');
                            }}

                            // Disable inputs and notify
                            document.querySelectorAll('input[name=""answer""]').forEach(input => {{
                                input.disabled = true;
                            }});
                            if (selected?.value.trim().toLowerCase() === correctAnswer) {{
                                chrome.webview.postMessage('correct');
                            }}

                            // Delay flip to show highlights
                            setTimeout(() => {{
                                document.getElementById('flipCard').classList.add('flipped');
                            }}, 1000); 
                        }}

                        function flipBack() {{
                            document.getElementById('flipCard').classList.remove('flipped');
                        }}

                        function openPdfDirectory() {{
                            chrome.webview.postMessage('openPdfDirectory');
                        }}
                    </script>
                </body>
                </html>";

                webView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error rendering HTML: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetNewStyles()
        {
            var baseStyles = @"
/* Common Styles */
.correct {
    background: linear-gradient(135deg, #e8f5e8 0%, #d4edda 100%) !important;
    border: 2px solid #4CAF50 !important;
    box-shadow: 0 4px 12px rgba(76, 175, 80, 0.3) !important;
    transform: translateY(-2px) !important;
}
.incorrect {
    background: linear-gradient(135deg, #fce8e8 0%, #f8d7da 100%) !important;
    border: 2px solid #f44336 !important;
    box-shadow: 0 4px 12px rgba(244, 67, 54, 0.3) !important;
    transform: translateY(-2px) !important;
}
.correct-answer {
    color: #2e7d32;
    font-size: 1.3rem;
    font-weight: 700;
    margin-bottom: 20px;
    text-align: center;
    white-space: pre-wrap;
    text-shadow: 0 2px 4px rgba(46, 125, 50, 0.1);
    max-height: 150px;
    overflow-y: auto;
}
.explanation-text {
    font-size: 1.1rem;
    line-height: 1.8;
    margin: 25px 0;
    color: #495057;
    white-space: pre-wrap;
    word-wrap: break-word;
    overflow-wrap: break-word;
    max-height: 200px;
    overflow-y: auto;
}
.source-text {
    position: absolute;
    bottom: 80px;
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
    z-index:5;
}
.source-text:hover {
    background: #a16f1b;
}

/* LTR Specific Styles */
[dir='ltr'] .difficulty-box {
    position: absolute;
    bottom: 20px;
    right: 20px;
    background: linear-gradient(135deg, #bb9b55 0%, #a68640 100%);
    color: white;
    padding: 8px 16px;
    border-radius: 20px;
    font-size: 0.95rem;
    font-weight: 600;
    white-space: pre-wrap;
    box-shadow: 0 3px 8px rgba(187, 155, 85, 0.3);
    z-index: 5;
}
[dir='ltr'] .option::before {
    left: 0;
    background: linear-gradient(90deg, transparent, rgba(187, 155, 85, 0.1));
}
[dir='ltr'] .explanation-text {
    text-align: left;
}

/* RTL Specific Styles */
[dir='rtl'] .difficulty-box {
    position: absolute;
    bottom: 20px;
    right: 20px;
    background: linear-gradient(135deg, #bb9b55 0%, #a68640 100%);
    color: white;
    padding: 8px 16px;
    border-radius: 20px;
    font-size: 0.95rem;
    font-weight: 600;
    white-space: pre-wrap;
    box-shadow: 0 3px 8px rgba(187, 155, 85, 0.3);
    z-index: 5;
}
[dir='rtl'] .option::before {
    right: 0;
    background: linear-gradient(-90deg, transparent, rgba(187, 155, 85, 0.1));
}
[dir='rtl'] .explanation-text {
    text-align: right;
}";

            var rotationValue = (AI.LanguageAI == "Arabic") ? "-180deg" : "180deg";

            var conditionalStyles = @"
    * {
        box-sizing: border-box;
        margin: 0;
        padding: 0;
    }
    body {
        font-family: 'Segoe UI', 'Arial', 'Tahoma', 'Noto Sans Arabic', 'Traditional Arabic', sans-serif;
        background: linear-gradient(135deg, #fcfaf5 0%, #f8f6f0 100%);
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: flex-start;
        min-height: 600px;
        padding: 20px;
        color: #333;
        line-height: 1.6;
    }
    .flip-container {
        perspective: 1000px;
        width: 100%;
        max-width: 900px;
        margin: 20px 0;
    }
    .card {
        width: 100%;
        height: auto;
        transition: transform .6s cubic-bezier(.175, .885, .32, 1.275);
        transform-style: preserve-3d;
        position: relative;
    }
    .card-back,
    .flip-container.flipped .card {
        transform: rotateY(" + rotationValue + @");
    }
    .card-face {
        background: linear-gradient(135deg, #ffffff 0%, #fefefe 100%);
        border-radius: 20px;
        box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1), 0 1px 8px rgba(0, 0, 0, 0.06);
        position: absolute;
        width: 100%;
        height: auto;
        min-height: 550px;
        backface-visibility: hidden;
        padding: 35px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        border: 1px solid rgba(0, 0, 0, 0.05);
    }
    .card-front {
        z-index: 2;
    }
    
    /* Question container with scroll */
    .question {
        max-height: 200px;
        overflow-y: auto;
        margin-bottom: 30px;
        padding: 15px;
        border-radius: 10px;
        background: rgba(187, 155, 85, 0.05);
        border: 1px solid rgba(187, 155, 85, 0.2);
    }
    
    .question-content {
        font-size: 1.4rem;
        font-weight: 600;
        text-align: center;
        white-space: pre-wrap;
        word-wrap: break-word;
        overflow-wrap: break-word;
        color: #2c3e50;
        line-height: 1.5;
    }
    
    /* Custom scrollbar */
    .question::-webkit-scrollbar,
    .correct-answer::-webkit-scrollbar,
    .explanation-text::-webkit-scrollbar {
        width: 8px;
    }
    
    .question::-webkit-scrollbar-track,
    .correct-answer::-webkit-scrollbar-track,
    .explanation-text::-webkit-scrollbar-track {
        background: rgba(187, 155, 85, 0.1);
        border-radius: 4px;
    }
    
    .question::-webkit-scrollbar-thumb,
    .correct-answer::-webkit-scrollbar-thumb,
    .explanation-text::-webkit-scrollbar-thumb {
        background: #bb9b55;
        border-radius: 4px;
    }
    
    .question::-webkit-scrollbar-thumb:hover,
    .correct-answer::-webkit-scrollbar-thumb:hover,
    .explanation-text::-webkit-scrollbar-thumb:hover {
        background: #a68640;
    }
    
    .options {
        flex-direction: column;
        gap: 18px;
        display: flex;
        margin-bottom: 30px;
        flex-grow: 1;
    }
    .option {
        position: relative;
        display: flex;
        align-items: flex-start;
        padding: 20px 25px 20px 60px;
        border: 2px solid #e9ecef;
        border-radius: 15px;
        background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
        cursor: pointer;
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        white-space: pre-wrap;
        word-wrap: break-word;
        overflow-wrap: break-word;
        overflow: hidden;
        min-height: 60px;
    }
    .option::before {
        content: '';
        position: absolute;
        top: 0;
        width: 0;
        height: 100%;
        transition: width 0.3s ease;
    }
    .option:hover {
        border-color: #bb9b55;
        background: linear-gradient(135deg, #fefefe 0%, #f9f7f0 100%);
        transform: translateY(-3px);
        box-shadow: 0 8px 25px rgba(187, 155, 85, 0.15);
    }
    .option:hover::before {
        width: 100%;
    }
    .option input[type='radio'] {
        position: absolute;
        opacity: 0;
        cursor: pointer;
        height: 30px;
        width: 30px;
        left: 15px;
        top: 25px;
        z-index: 1;
        margin: 0;
        padding: 0;
    }
    .option .radio-custom {
        display: inline-block;
        width: 24px;
        height: 24px;
        border: 2px solid #a68640;
        border-radius: 50%;
        margin-right: 15px;
        transition: all 0.3s ease;
        position: absolute;
        left: 20px;
        top: 23px;
        box-sizing: border-box;
        flex-shrink: 0;
    }
    .option .radio-custom::after {
        content: '';
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%) scale(0);
        width: 12px;
        height: 12px;
        border-radius: 50%;
        background-color: #bb9b55;
        transition: transform 0.3s ease;
    }
    .option input[type='radio']:checked + .radio-custom {
        border-color: #8f6f2b;
        background-color: rgba(187, 155, 85, 0.1);
    }
    .option input[type='radio']:checked + .radio-custom::after {
        transform: translate(-50%, -50%) scale(1);
    }
    .option-text {
        display: block;
        flex-grow: 1;
        white-space: pre-wrap;
        word-wrap: break-word;
        overflow-wrap: break-word;
        font-size: 1.05rem;
        line-height: 1.6;
        color: #495057;
        transition: all 0.3s ease;
        margin-top: 2px;
    }
    .back-btn,
    .flip-btn {
        display: inline-block;
        padding: 15px 30px;
        font-weight: 600;
        font-size: 1.05rem;
        border: none;
        border-radius: 12px;
        cursor: pointer;
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        text-transform: uppercase;
        letter-spacing: 0.5px;
        flex-shrink: 0;
    }
    .flip-btn {
        background: linear-gradient(135deg, #bb9b55 0%, #a68640 100%);
        color: #fff;
        margin-left: 10px;
        box-shadow: 0 4px 15px rgba(187, 155, 85, 0.3);
    }
    .flip-btn:hover {
        background: linear-gradient(135deg, #a68640 0%, #8f6f2b 100%);
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(187, 155, 85, 0.4);
    }
    .back-btn {
        background: linear-gradient(135deg, #6c757d 0%, #5a6268 100%);
        color: #fff;
        margin-right: 10px;
        box-shadow: 0 4px 15px rgba(108, 117, 125, 0.3);
    }
    .back-btn:hover {
        background: linear-gradient(135deg, #5a6268 0%, #495057 100%);
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(108, 117, 125, 0.4);
    }
    
    /* Code styling */
    .option pre, .question pre, .correct-answer pre, .explanation-text pre {
        margin: 8px 0;
        border-radius: 8px;
        overflow-x: auto;
        max-width: 100%;
    }
    
    .option code, .question code, .correct-answer code, .explanation-text code {
        font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
        font-size: 0.9em;
        line-height: 1.4;
        direction: ltr;
        text-align: left;
    }
    
    /* Inline code for small snippets */
    .option-text code:not(pre code), 
    .question-content code:not(pre code), 
    .correct-answer code:not(pre code), 
    .explanation-text code:not(pre code) {
        background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
        padding: 2px 6px;
        border-radius: 4px;
        font-size: 0.95em;
        border: 1px solid #dee2e6;
        display: inline;
    }
    
    @media (max-width: 768px) {
        .card-face {
            min-height: 500px;
            padding: 25px;
        }
        .question {
            max-height: 150px;
            padding: 10px;
        }
        .question-content {
            font-size: 1.2rem;
        }
        .back-btn,
        .flip-btn {
            width: 100%;
            padding: 15px;
            margin: 8px 0;
        }
        .option {
            padding: 15px 20px 15px 50px;
            min-height: 50px;
        }
        .option .radio-custom {
            width: 20px;
            height: 20px;
            left: 15px;
            top: 18px;
        }
        .option .radio-custom::after {
            width: 10px;
            height: 10px;
        }
        .option input[type='radio'] {
            height: 24px;
            width: 24px;
            top: 20px;
        }
        .correct-answer {
            max-height: 120px;
        }
        .explanation-text {
            max-height: 150px;
        }
    }
    
    /* Syntax highlighting styles for code blocks */
    pre code {
        display: block;
        overflow-x: auto;
        padding: 1em;
        background: #282c34;
        color: #abb2bf;
        border-radius: 8px;
        text-align: left;
        font-size: 0.9em;
        line-height: 1.4;
    }
    
    .hljs {
        background: #282c34;
        color: #abb2bf;
    }
    
    .hljs-comment,
    .hljs-quote {
      color: #5c6370;
      font-style: italic;
    }
    
    .hljs-doctag,
    .hljs-keyword,
    .hljs-formula {
      color: #c678dd;
    }
    
    .hljs-section,
    .hljs-name,
    .hljs-selector-tag,
    .hljs-deletion,
    .hljs-subst {
      color: #e06c75;
    }
    
    .hljs-literal {
      color: #56b6c2;
    }
    
    .hljs-string,
    .hljs-regexp,
    .hljs-addition,
    .hljs-attribute,
    .hljs-meta-string {
      color: #98c379;
    }
    
    .hljs-built_in,
    .hljs-class .hljs-title {
      color: #e6c07b;
    }
    
    .hljs-attr,
    .hljs-variable,
    .hljs-template-variable,
    .hljs-type,
    .hljs-selector-class,
    .hljs-selector-attr,
    .hljs-selector-pseudo,
    .hljs-number {
      color: #d19a66;
    }
    
    .hljs-symbol,
    .hljs-bullet,
    .hljs-link,
    .hljs-meta,
    .hljs-selector-id,
    .hljs-title {
      color: #61aeee;
    }
    
    .hljs-emphasis {
      font-style: italic;
    }
    
    .hljs-strong {
      font-weight: bold;
    }
    
    .hljs-link {
      text-decoration: underline;
    }";

            return baseStyles + conditionalStyles;
        }
    }
}


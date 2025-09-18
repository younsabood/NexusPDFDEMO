using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using GenerativeAI.Types;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Math;
using NexusPDF;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Web.UI.WebControls;
using Word = Microsoft.Office.Interop.Word;

namespace NexusPDF
{
    public partial class HtmlMultipleChoiceMath : UserControl
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
        private bool _hasBeenAnsweredCorrectly = false;

        public HtmlMultipleChoiceMath()
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
                        throw new ArgumentException($"Exactly {numOfOption} options required");
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

        public async System.Threading.Tasks.Task ResetAsync()
        {
            // Reset the flag when resetting the question
            _hasBeenAnsweredCorrectly = false;

            if (webView?.CoreWebView2 != null)
            {
                await webView.CoreWebView2.ExecuteScriptAsync(
                    "document.querySelectorAll('input').forEach(i => i.checked = false);");
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

        // Helper function to clean and normalize text for comparison
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            return text.Trim()
                      .Replace("\\", "\\\\") // Escape backslashes for JavaScript
                      .Replace("'", "\\'")   // Escape single quotes
                      .Replace("\"", "\\\"") // Escape double quotes
                      .Replace("\r\n", "\\n")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\n");
        }

        private string GenerateOptionHtml(int index)
        {
            try
            {
                if (_options == null || index >= _options.Length) return "";

                var optionText = _options[index] ?? "";
                var normalizedOption = NormalizeText(optionText);

                return $@"
    <label class='option' for='option{index}'>
        <input type='radio' name='answer' id='option{index}' value='{normalizedOption}' data-original-value='{optionText.Replace("'", "&#39;").Replace("\"", "&quot;")}'>
        <span class='radio-custom'></span>
        <span class='option-text tex2jax_process' data-index='{index}'>{optionText}</span>
    </label>";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating option HTML: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        private void RenderHtml()
        {
            try
            {
                if (webView?.CoreWebView2 == null || _options == null || _correctAnswer == null) return;

                var normalizedCorrectAnswer = NormalizeText(_correctAnswer);

                var html = $@"
<!DOCTYPE html>
<html lang='{(AI.LanguageAI == "Arabic" ? "ar" : "en")}' dir='{(AI.LanguageAI == "Arabic" ? "rtl" : "ltr")}'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Mathematical Flashcard MCQ</title>
    
    <!-- MathJax Configuration and Loading -->
    <script>
        window.MathJax = {{
            tex: {{
                inlineMath: [['$', '$'], ['\\(', '\\)']],
                displayMath: [['$$', '$$'], ['\\[', '\\]']],
                processEscapes: true,
                processEnvironments: true,
                packages: {{'{{ [+] }}'': ['ams', 'amssymb', 'amsmath', 'physics', 'mhchem', 'cancel', 'color', 'bbox', 'xcolor']}},
                tags: 'ams',
                tagSide: 'right',
                tagIndent: '.8em',
                multlineWidth: '85%'
            }},
            chtml: {{
                scale: 1.2,
                minScale: 0.8,
                maxScale: 2.0,
                displayAlign: 'center',
                displayIndent: '0',
                matchFontHeight: true,
                exFactor: 0.5
            }},
            options: {{
                enableMenu: false,
                skipHtmlTags: ['script', 'noscript', 'style', 'textarea', 'pre', 'code'],
                ignoreHtmlClass: 'tex2jax_ignore',
                processHtmlClass: 'tex2jax_process'
            }},
            startup: {{
                typeset: false,
                ready() {{
                    MathJax.startup.defaultReady();
                    MathJax.startup.promise.then(() => {{
                        console.log('MathJax is ready');
                    }});
                }}
            }}
        }};
    </script>
    <script id='MathJax-script' async src='https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js'></script>
    
    <style>
        {GetMathStyles()}
    </style>
</head>
<body>
    <div class='flip-container' id='flipCard'>
        <div class='card'>
            <div class='card-face card-front'>
                <div class='question' id='questionContainer'>
                    <div class='question-content tex2jax_process'>{QuestionText}</div>
                </div>
                <div class='options'>
                    {(numOfOption > 2 ? GenerateOptions(4) : GenerateOptions(2))}
                </div>
                <button class='flip-btn' onclick='flipCard()'>Check Answer</button>
            </div>
            <div class='card-face card-back'>
                <div class='correct-answer tex2jax_process'>{CorrectAnswer}</div>
                <div class='explanation-text tex2jax_process'>{Explanation}</div>
                <hr style=""height:2px;border-width:0;color:gray;background-color:gray"">
                <div class='source-text' onclick='openPdfDirectory()' title='Click to open PDF directory'>{Source}</div>
                <div class='difficulty-box'>{Difficulty}</div>
                <button class='flip-btn back-btn' onclick='flipBack()'>Back to Question</button>
            </div>
        </div>
    </div>
    
    <script>
        let mathInitialized = false;
        const CORRECT_ANSWER = '{normalizedCorrectAnswer}';
        
        // Function to normalize text for comparison
        function normalizeForComparison(text) {{
            if (!text) return '';
            return text.toString().trim();
        }}
        
        function initializeMath() {{
            try {{
                if (window.MathJax && window.MathJax.typesetPromise) {{
                    window.MathJax.typesetPromise().then(() => {{
                        console.log('MathJax rendering completed');
                        mathInitialized = true;
                    }}).catch((err) => {{
                        console.error('MathJax error:', err);
                        // Try again after a short delay
                        setTimeout(initializeMath, 1000);
                    }});
                }} else {{
                    console.log('MathJax not ready, retrying...');
                    setTimeout(initializeMath, 500);
                }}
            }} catch (error) {{
                console.error('Math initialization error:', error);
                setTimeout(initializeMath, 1000);
            }}
        }}
        
        // Wait for libraries to load before initializing
        document.addEventListener('DOMContentLoaded', function() {{
            // Wait longer to ensure MathJax is loaded
            setTimeout(initializeMath, 300);
        }});

        // Render math again when flipping the card
        function flipCard() {{
            const selected = document.querySelector('input[name=""answer""]:checked');
            if (!selected) {{
                // Use a message box instead of alert
                let messageBox = document.createElement('div');
                messageBox.innerHTML = 'يرجى اختيار إجابة أولاً';
                messageBox.style.cssText = 'position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); background-color: #fff; border: 2px solid #a68640; padding: 20px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.1); z-index: 1000; text-align: center; font-size: 1.2rem;';
                document.body.appendChild(messageBox);
                setTimeout(() => {{ document.body.removeChild(messageBox); }}, 2000);
                return;
            }}

            const options = document.querySelectorAll('.option');
            
            // Reset all styles first
            options.forEach(option => {{
                option.classList.remove('correct', 'incorrect');
            }});

            // Find and highlight the correct answer
            let correctOptionFound = false;
            options.forEach(option => {{
                const input = option.querySelector('input');
                const inputValue = normalizeForComparison(input.value);
                const originalValue = normalizeForComparison(input.getAttribute('data-original-value'));
                const correctAnswerNormalized = normalizeForComparison(CORRECT_ANSWER);
                
                console.log('Comparing:', {{
                    inputValue: inputValue,
                    originalValue: originalValue,
                    correctAnswer: correctAnswerNormalized
                }});
                
                // Compare with both values
                if (inputValue === correctAnswerNormalized || originalValue === correctAnswerNormalized) {{
                    option.classList.add('correct');
                    correctOptionFound = true;
                }}
            }});

            // Highlight the selected incorrect answer
            const selectedValue = normalizeForComparison(selected.value);
            const selectedOriginalValue = normalizeForComparison(selected.getAttribute('data-original-value'));
            const correctAnswerNormalized = normalizeForComparison(CORRECT_ANSWER);
            
            const isCorrect = selectedValue === correctAnswerNormalized || selectedOriginalValue === correctAnswerNormalized;
            
            if (!isCorrect) {{
                const incorrectOption = selected.closest('.option');
                incorrectOption.classList.add('incorrect');
            }}

            // Disable inputs and send notification
            document.querySelectorAll('input[name=""answer""]').forEach(input => {{
                input.disabled = true;
            }});

            if (isCorrect) {{
                chrome.webview.postMessage('correct');
            }}

            // Delay the flip to show highlights
            setTimeout(() => {{
                document.getElementById('flipCard').classList.add('flipped');
                // Re-render math on the back side
                setTimeout(() => {{
                    if (window.MathJax && window.MathJax.typesetPromise) {{
                        MathJax.typesetPromise();
                    }}
                }}, 200);
            }}, 1000);
        }}

        function flipBack() {{
            document.getElementById('flipCard').classList.remove('flipped');
            // Re-render math on the front side
            setTimeout(() => {{
                if (window.MathJax && window.MathJax.typesetPromise) {{
                    MathJax.typesetPromise();
                }}
            }}, 100);
        }}

        function openPdfDirectory() {{
            chrome.webview.postMessage('openPdfDirectory');
        }}
        
        // Re-render math on window resize
        window.addEventListener('resize', function() {{
            if (window.MathJax && window.MathJax.typesetPromise) {{
                setTimeout(() => MathJax.typesetPromise(), 100);
            }}
        }});
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
        private string GetMathStyles()
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
    z-index: 5;
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
}

/* Base Styles */
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
    transform: rotateY(" + (AI.LanguageAI == "Arabic" ? "-180deg" : "180deg") + @");
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
.question {
    font-size: 1.5rem;
    font-weight: 600;
    text-align: center;
    margin-bottom: 30px;
    white-space: pre-wrap;
    word-wrap: break-word;
    overflow-wrap: break-word;
    color: #2c3e50;
    line-height: 1.5;
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
    align-items: center;
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
    top: 50%;
    transform: translateY(-50%);
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
    top: 50%;
    transform: translateY(-50%);
    box-sizing: border-box;
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
.option code, .explanation-text code, .question code {
    font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
    background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
    padding: 4px 8px;
    border-radius: 6px;
    direction: ltr;
    border: 1px solid #dee2e6;
}
@media (max-width: 768px) {
    .card-face {
        min-height: 500px;
    }
    .question {
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
    }
    .card-face {
        padding: 25px;
    }
    .option .radio-custom {
        width: 20px;
        height: 20px;
        left: 15px;
    }
    .option .radio-custom::after {
        width: 10px;
        height: 10px;
    }
    .option input[type='radio'] {
        height: 24px;
        width: 24px;
    }
}
/* Math rendering enhancement */
.MathJax {
    font-size: 1.3em !important;
    line-height: 1.4 !important;
}

.MathJax_Display {
    margin: 1em 0 !important;
    text-align: center !important;
}

/* Improving math display in different elements */
.question-content .MathJax,
.option-text .MathJax,
.correct-answer .MathJax,
.explanation-text .MathJax {
    display: inline-block !important;
    vertical-align: middle !important;
}

/* Ensure math text is readable */
.tex2jax_process {
    font-family: 'Times New Roman', 'STIX Two Math', serif !important;
}

/* Fixing overlap issues */
.MathJax * {
    max-width: none !important;
    box-sizing: content-box !important;
}

/* Improving responsiveness */
@media (max-width: 768px) {
    .MathJax {
        font-size: 1.1em !important;
    }
}
";

            return baseStyles;
        }
    }
}

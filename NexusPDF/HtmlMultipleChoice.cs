using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using GenerativeAI.Types;

namespace NexusPDF
{
    public partial class HtmlMultipleChoice : UserControl
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


        public HtmlMultipleChoice()
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
        private void RenderHtml()
        {
            try
            {
                if (webView?.CoreWebView2 == null || _options == null || _correctAnswer == null) return;

                var html = $@"
               <!DOCTYPE html>
                <html lang='{(AI.LanguageAI == "Arabic" ? "ar" : "en")}' dir='{(AI.LanguageAI == "Arabic" ? "rtl" : "ltr")}'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Flashcard MCQ</title>
                    <style>
                        {GetNewStyles()}
                    </style>
                </head>
                <body>
                    <div class='flip-container' id='flipCard'>
                        <div class='card'>
                            <div class='card-face card-front'>
                                <div class='question'>{QuestionText}</div>
                                <div class='options'>
                                {(numOfOption > 2 ? GenerateOptions(4) : GenerateOptions(2))}
                                </div>
                                <button class='flip-btn' onclick='flipCard()'>Check Answer</button>
                            </div>
                            <div class='card-face card-back'>
                                <div class='correct-answer'>{CorrectAnswer}</div>
                                <div class='explanation-text'>{Explanation}</div>
                                <hr style=""height:2px;border-width:0;color:gray;background-color:gray"">
                                <div class='source-text' onclick='openPdfDirectory()' title='Click to open PDF directory'>{Source}</div>
                                <div class='difficulty-box'>{Difficulty}</div>
                                <button class='flip-btn back-btn' onclick='flipBack()'>Back to Question</button>
                            </div>
                        </div>
                    </div>
                    <script>
                        const correctAnswer = '{CorrectAnswer}'.trim().toLowerCase();

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
        private string GenerateOptionHtml(int index)
        {
            try
            {
                return $@"
    <label class='option' for='option{index}'>
        <input type='radio' name='answer' id='option{index}' value='{_options[index]}'>
        <span class='radio-custom'></span>
        <span class='option-text'>{_options[index]}</span>
    </label>";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating option HTML: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
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
}
.explanation-text {
    font-size: 1.1rem;
    line-height: 1.8;
    margin: 25px 0;
    color: #495057;
    white-space: pre-wrap;
    word-wrap: break-word;
    overflow-wrap: break-word;
}
.source-text {
    position: absolute; /* Changed to absolute positioning */
    bottom: 80px; /* Aligned with source-box bottom */
    left: 20px; /* Aligned with source-box left */
    background: #bb9b55; /* Solid background color as in source-box */
    color: white; /* Text color */
    padding: 6px 12px; /* Padding adjusted */
    border-radius: 4px; /* Border radius adjusted */
    font-size: 0.9rem; /* Font size adjusted */
    font-weight: 500; /* Font weight adjusted */
    max-width: calc(100% - 40px); /* Max width to prevent overflow */
    word-wrap: break-word; /* Ensure text wraps */
    cursor: pointer; /* Pointer cursor on hover */
    transition: background-color 0.3s ease; /* Smooth transition for hover */
    user-select: none; /* Prevent text selection */
    z-index:5;
}
.source-text:hover {
    background: #a16f1b; /* Darker background on hover */
    /* Removed transform and box-shadow as they were not in the reference source-box hover */
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

            // Fixed: Use proper conditional operator syntax with parentheses
            var rotationValue = (AI.LanguageAI == "Arabic") ? "-180deg" : "180deg";

            var conditionalStyles1 = @"
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
        height: 600px;
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
        min-height: 550px;
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
        height: 100%;
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
    }
    .option {
        position: relative;
        display: flex; /* Changed from block to flex */
        align-items: center; /* Added for vertical alignment */
        padding: 20px 25px 20px 60px; /* Adjusted left padding for custom radio */
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
    /* Hide the native radio button but keep it accessible */
    .option input[type='radio'] {
        position: absolute;
        opacity: 0;
        cursor: pointer;
        /* Make the hit area larger for better touch interaction */
        height: 30px; /* Increased size */
        width: 30px;  /* Increased size */
        left: 15px; /* Position it to align with the custom radio */
        top: 50%;
        transform: translateY(-50%);
        z-index: 1; /* Ensure it's above the custom radio for clicks */
        margin: 0; /* Ensure no default margin */
        padding: 0; /* Ensure no default padding */
    }
    /* Custom radio button visual */
    .option .radio-custom {
        display: inline-block;
        width: 24px; /* Desired size for the radio button */
        height: 24px; /* Desired size for the radio button */
        border: 2px solid #a68640; /* Outer circle border color */
        border-radius: 50%;
        margin-right: 15px; /* Space between radio and text */
        transition: all 0.3s ease;
        position: absolute; /* Position it precisely within the label */
        left: 20px;
        top: 50%;
        transform: translateY(-50%);
        box-sizing: border-box; /* Include padding/border in element's total width/height */
    }

    /* Inner dot of the custom radio button */
    .option .radio-custom::after {
        content: '';
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%) scale(0); /* Start scaled to 0 */
        width: 12px; /* Size of the inner dot */
        height: 12px; /* Size of the inner dot */
        border-radius: 50%;
        background-color: #bb9b55; /* Inner dot color */
        transition: transform 0.3s ease; /* Animate the dot appearance */
    }

    /* When the radio input is checked, style the custom radio */
    .option input[type='radio']:checked + .radio-custom {
        border-color: #8f6f2b; /* Darker border when checked */
        background-color: rgba(187, 155, 85, 0.1); /* Subtle background when checked */
    }

    /* When the radio input is checked, make the inner dot visible */
    .option input[type='radio']:checked + .radio-custom::after {
        transform: translate(-50%, -50%) scale(1); /* Scale to 1 to show the dot */
    }
    .option-text {
        display: block; /* Allows text to wrap */
        flex-grow: 1; /* Allows text to take available space */
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
        .card {
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
            padding: 15px 20px 15px 50px; /* Adjusted padding for smaller screens */
        }
        .card-face {
            padding: 25px;
        }
        .option .radio-custom {
            width: 20px; /* Smaller radio button on mobile */
            height: 20px;
            left: 15px;
        }
        .option .radio-custom::after {
            width: 10px; /* Smaller dot on mobile */
            height: 10px;
        }
        .option input[type='radio'] {
            height: 24px; /* Adjusted hit area for smaller screens */
            width: 24px;
        }
    }";

            var conditionalStyles2 = @"
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
        height: 600px;
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
        min-height: 550px;
        transition: transform .6s cubic-bezier(.175, .885, .32, 1.275);
        transform-style: preserve-3d;
        position: relative;
    }
    .card-back,
    .flip-container.flipped .card {
        transform: rotateY(180deg);
    }
    .card-face {
        background: linear-gradient(135deg, #ffffff 0%, #fefefe 100%);
        border-radius: 20px;
        box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1), 0 1px 8px rgba(0, 0, 0, 0.06);
        position: absolute;
        width: 100%;
        height: 100%;
        backface-visibility: hidden;
        padding: 35px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        direction: auto;
        unicode-bidi: embed;
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
        unicode-bidi: embed;
        direction: auto;
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
    }
    .option {
        position: relative;
        display: flex; /* Changed from block to flex */
        align-items: center; /* Added for vertical alignment */
        padding: 20px 25px 20px 60px; /* Adjusted left padding for custom radio */
        border: 2px solid #e9ecef;
        border-radius: 15px;
        background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
        cursor: pointer;
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        unicode-bidi: embed;
        direction: auto;
        text-align: start;
        white-space: pre-wrap;
        word-wrap: break-word;
        overflow-wrap: break-word;
        overflow: hidden;
    }
    .option::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        width: 0;
        height: 100%;
        background: linear-gradient(90deg, transparent, rgba(187, 155, 85, 0.1));
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
    /* Hide the native radio button but keep it accessible */
    .option input[type='radio'] {
        position: absolute;
        opacity: 0;
        cursor: pointer;
        /* Make the hit area larger for better touch interaction */
        height: 30px; /* Increased size */
        width: 30px;  /* Increased size */
        left: 15px; /* Position it to align with the custom radio */
        top: 50%;
        transform: translateY(-50%);
        z-index: 1; /* Ensure it's above the custom radio for clicks */
        margin: 0; /* Ensure no default margin */
        padding: 0; /* Ensure no default padding */
    }
    /* Custom radio button visual */
    .option .radio-custom {
        display: inline-block;
        width: 24px; /* Desired size for the radio button */
        height: 24px; /* Desired size for the radio button */
        border: 2px solid #a68640; /* Outer circle border color */
        border-radius: 50%;
        margin-right: 15px; /* Space between radio and text */
        transition: all 0.3s ease;
        position: absolute; /* Position it precisely within the label */
        left: 20px;
        top: 50%;
        transform: translateY(-50%);
        box-sizing: border-box; /* Include padding/border in element's total width/height */
    }

    /* Inner dot of the custom radio button */
    .option .radio-custom::after {
        content: '';
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%) scale(0); /* Start scaled to 0 */
        width: 12px; /* Size of the inner dot */
        height: 12px; /* Size of the inner dot */
        border-radius: 50%;
        background-color: #bb9b55; /* Inner dot color */
        transition: transform 0.3s ease; /* Animate the dot appearance */
    }

    /* When the radio input is checked, style the custom radio */
    .option input[type='radio']:checked + .radio-custom {
        border-color: #8f6f2b; /* Darker border when checked */
        background-color: rgba(187, 155, 85, 0.1); /* Subtle background when checked */
    }

    /* When the radio input is checked, make the inner dot visible */
    .option input[type='radio']:checked + .radio-custom::after {
        transform: translate(-50%, -50%) scale(1); /* Scale to 1 to show the dot */
    }
    .option-text {
        display: block;
        direction: auto;
        unicode-bidi: embed;
        text-align: start;
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
    }
    .flip-btn {
        background: linear-gradient(135deg, #bb9b55 0%, #a68640 100%);
        color: #fff;
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
        unicode-bidi: embed;
        border: 1px solid #dee2e6;
    }
    @media (max-width: 768px) {
        .card {
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
            padding: 15px 20px 15px 50px; /* Adjusted padding for smaller screens */
        }
        .card-face {
            padding: 25px;
        }
        .option .radio-custom {
            width: 20px; /* Smaller radio button on mobile */
            height: 20px;
            left: 15px;
        }
        .option .radio-custom::after {
            width: 10px; /* Smaller dot on mobile */
            height: 10px;
        }
        .option input[type='radio'] {
            height: 24px; /* Adjusted hit area for smaller screens */
            width: 24px;
        }
    }";

            // Use proper conditional logic for C# 4.7.2
            if (AI.LanguageAI == "Arabic")
            {
                return baseStyles + conditionalStyles1;
            }
            else
            {
                return baseStyles + conditionalStyles2;
            }
        }

    }
}
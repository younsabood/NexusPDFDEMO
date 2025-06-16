using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.AI;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;

namespace NexusPDF
{
    public partial class version : UserControl
    {
        private WebView2 webView;
        private string userDataFolder;
        private string _Json;
        private bool _isWebViewInitialized = false;
        private bool _isDisposed = false;

        public version()
        {
            InitializeComponent();
            try
            {
                // Set initial size and constraints for the UserControl
                this.Height = 650;
                this.Width = 600;
                this.MaximumSize = new System.Drawing.Size(0, 650); // Setting MaxWidth to 0 removes the width constraint

                // Create a unique user data folder for WebView2 to avoid conflicts and enable clean disposal
                userDataFolder = Path.Combine(Path.GetTempPath(), "WebView2_Version_" + Guid.NewGuid().ToString());

                // Initialize WebView asynchronously
                _ = InitializeWebViewAsync();

                // Register for Disposed event to clean up resources
                this.Disposed += OnDisposed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Version UserControl: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string Json
        {
            get => _Json;
            set
            {
                _Json = value;
                // Update the WebView when JSON data changes, ensuring it's initialized first
                if (_isWebViewInitialized)
                {
                    _ = UpdateCardDataAsync();
                }
            }
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                // Create and add WebView2 control
                webView = new WebView2 { Dock = DockStyle.Fill };
                this.Controls.Add(webView);

                // Ensure user data folder exists
                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                }

                // Create WebView2 environment with a custom user data folder
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                // Handle messages from JavaScript (e.g., download, email)
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // Handle navigation completed to ensure HTML is loaded before pushing data
                webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

                _isWebViewInitialized = true;
                RenderHtml(); // Render initial HTML (loading state)
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView: {ex.Message}", "WebView Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _isWebViewInitialized = false;
            }
        }

        // Handles cleanup when the UserControl is disposed
        private void OnDisposed(object sender, EventArgs e)
        {
            if (_isDisposed) return; // Prevent multiple disposals
            _isDisposed = true;

            try
            {
                if (webView != null)
                {
                    // Detach event handlers to prevent memory leaks or issues after disposal
                    if (webView.CoreWebView2 != null)
                    {
                        webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                        webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                    }
                    this.Controls.Remove(webView); // Remove from the controls collection
                    webView.Dispose(); // Dispose the WebView2 control
                    webView = null;
                }

                // Attempt to delete the temporary user data folder
                if (Directory.Exists(userDataFolder))
                {
                    // Retry logic for robust deletion, as folder might be in use for a brief moment
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            Directory.Delete(userDataFolder, true);
                            break; // Success, exit loop
                        }
                        catch (IOException ioEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Retry {i + 1}: IOException during cleanup of WebView2 user data folder: {ioEx.Message}");
                            Task.Delay(50).Wait(); // Wait a bit before retrying
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        // Called when WebView2 navigation is completed
        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && !string.IsNullOrEmpty(_Json))
            {
                // If navigation is successful and JSON data exists, update the card content
                _ = UpdateCardDataAsync();
            }
        }

        // Handles messages sent from JavaScript to C#
        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                System.Diagnostics.Debug.WriteLine($"Message received from WebView2: {message}");

                var messageData = JsonSerializer.Deserialize<Dictionary<string, object>>(message);

                if (messageData != null && messageData.ContainsKey("type"))
                {
                    var type = messageData["type"]?.ToString(); // Using null-conditional operator for safety

                    switch (type)
                    {
                        case "download":
                            if (messageData.ContainsKey("url"))
                            {
                                var downloadUrl = messageData["url"]?.ToString();
                                if (!string.IsNullOrEmpty(downloadUrl))
                                {
                                    HandleDownload(downloadUrl);
                                }
                            }
                            break;

                        case "email":
                            if (messageData.ContainsKey("email"))
                            {
                                var email = messageData["email"]?.ToString();
                                if (!string.IsNullOrEmpty(email))
                                {
                                    HandleEmail(email);
                                }
                            }
                            break;
                        case "navigate": // New case for handling navigation from logo click
                            if (messageData.ContainsKey("url"))
                            {
                                var navigateUrl = messageData["url"]?.ToString();
                                if (!string.IsNullOrEmpty(navigateUrl))
                                {
                                    HandleNavigation(navigateUrl);
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling web message: {ex.Message}");
            }
        }

        // Opens a URL in the default browser for download
        private void HandleDownload(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening download URL: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Opens the default email client with a pre-filled address
        private void HandleEmail(string email)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"mailto:{email}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening email client: {ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // New method to handle general navigation (e.g., from logo link)
        private void HandleNavigation(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navigating to URL: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Updates the HTML content with new JSON data by executing JavaScript
        private async Task UpdateCardDataAsync()
        {
            // Ensure WebView is initialized and not disposed before executing script
            if (!_isWebViewInitialized || string.IsNullOrEmpty(_Json) || webView?.CoreWebView2 == null || _isDisposed)
            {
                System.Diagnostics.Debug.WriteLine("WebView2 not ready for data update or JSON is empty.");
                return;
            }

            try
            {
                // Escape JSON string for safe JavaScript injection
                var escapedJson = _Json
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");

                // Call the JavaScript function 'updateCardData' with the escaped JSON
                var script = $"if (typeof updateCardData === 'function') {{ updateCardData('{escapedJson}'); }}";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating card data: {ex.Message}");
            }
        }

        // Renders the initial HTML content in the WebView2 control
        private void RenderHtml()
        {
            if (webView?.CoreWebView2 == null || _isDisposed)
            {
                System.Diagnostics.Debug.WriteLine("WebView2.CoreWebView2 is not initialized when RenderHtml is called.");
                return;
            }

            var html = GenerateHtml(); // Get the HTML string with the loading state

            try
            {
                webView.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navigating WebView2 with HTML: {ex.Message}", "HTML Rendering Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method to convert an image file to a Base64 string
        private string GetBase64Image(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                System.Diagnostics.Debug.WriteLine($"Image file not found: {imagePath}");
                return "";
            }
            try
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting image '{imagePath}' to Base64: {ex.Message}");
                return "";
            }
        }

        // Generates the main HTML structure for the card
        private string GenerateHtml()
        {
            // Define the path to your logo image.
            // IMPORTANT: Ensure 'icon.png' is located in an 'Assets' folder
            // relative to your application's executable, and its 'Build Action' is 'Content',
            // 'Copy to Output Directory' is 'Copy if newer'.
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.png");
            string base64Logo = GetBase64Image(logoPath);

            // Use Base64 data URI if image is found, otherwise fallback to the external URL
            string logoSrc = string.IsNullOrEmpty(base64Logo) ?
                             "https://i.ibb.co/BHxVQsT5/icon.png" : // Fallback external URL
                             $"data:image/png;base64,{base64Logo}";

            // Define the URL the logo should link to
            // REPLACE THIS WITH YOUR DESIRED LOGO LINK URL
            string logoLinkUrl = "https://www.yourwebsitedomain.com"; // Example: your company's website or project page

            return $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>NexusPDF Card</title>
                <style>
                    {GetStyles()}
                </style>
            </head>
            <body>
                <div class='card' id='cardContainer'>
                    <div class='header'>
                        <div class='header-content'> 
                            <div class='logo-container'>
                                <a href='#' onclick='handleLogoClick(""{logoLinkUrl}""); return false;'>
                                    <img src=""{logoSrc}"" alt='Logo' class='logo'>
                                </a>
                            </div>
                            <div class='text-content'> 
                                <span class='version'>Version 1.0</span>
                                <h1 class='title'>NexusPDF Document Card</h1>
                                <p class='date'>Generated on: June 21, 2025</p>
                            </div>
                        </div>
                    </div>
                    <div class='section' style='text-align: center; padding: 60px 32px;'>
                        <h2 style='color: #9ca3af; margin-bottom: 16px;'>Loading...</h2>
                        <p style='color: #9ca3af;'>Please wait while the card is being prepared.</p>
                    </div>
                </div>

                <script>
                    {GetJavaScript()}
                    
                    // Auto-update if JSON is available
                    document.addEventListener('DOMContentLoaded', function() {{
                        console.log('DOM Content Loaded');
                    }});
                </script>
            </body>
            </html>";
        }

        // Provides the CSS styles for the HTML card
        private string GetStyles()
        {
            return @"
                :root {
                    --husk: #bb9b55;
                    --sweet-corn: #f7e28a;
                    --korma: #895113;
                    --potters-clay: #9a7034;
                    --putty: #e7ce80;
                    --calico: #dac67f;
                    --mandalay: #a16f1b;
                    --marzipan: #f8e49c;
                    --kumera: #895e1e;
                    --metallic-bronze: #53391a;
                }
                .header {
                    display: flex; /* Make the header a flex container */
                    align-items: center; /* Vertically align items in the center */
                    padding: 32px;
                    background: linear-gradient(135deg, var(--metallic-bronze) 0%, var(--korma) 100%);
                    color: white;
                    position: relative;
                }
                .header-content { /* New flex container to hold logo and text */
                    display: flex;
                    align-items: center; /* Align items vertically in the center */
                    gap: 20px; /* Space between logo and text */
                    width: 100%; /* Ensure it takes full width */
                }
                .logo-container {
                    flex-shrink: 0; /* Prevent logo from shrinking */
                    /* margin-bottom: 16px; Removed, as flexbox handles spacing */
                }
                .logo {
                    max-width: 120px; /* Limits the logo width */
                    height: auto; /* Maintains aspect ratio */
                    border-radius: 8px; /* Adds slight rounding */
                    display: block; /* Ensures the image doesn't have extra space below it from inline display */
                }
                .logo-container a { /* Style for the link around the logo */
                    text-decoration: none; /* Remove underline from the logo link */
                    display: inline-block; /* Allows the link to wrap around the image correctly */
                }
                .text-content { /* New container for version, title, date */
                    flex-grow: 1; /* Allows text content to take up remaining space */
                }
                * {
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }

                body {
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    background: #fafafa;
                    color: var(--metallic-bronze);
                    line-height: 1.6;
                    padding: 16px;
                }

                .card {
                    background: white;
                    border-radius: 16px;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                    overflow: hidden;
                    border: 1px solid #e5e7eb;
                    max-width: 100%;
                }


                .version {
                    display: inline-flex;
                    align-items: center;
                    background: rgba(255, 255, 255, 0.2);
                    padding: 6px 12px;
                    border-radius: 6px;
                    font-size: 13px;
                    font-weight: 500;
                    margin-bottom: 16px;
                    backdrop-filter: blur(10px);
                }

                .title {
                    font-size: 24px;
                    font-weight: 700;
                    margin-bottom: 8px;
                    line-height: 1.3;
                }

                .date {
                    opacity: 0.9;
                    font-size: 14px;
                }

                .section {
                    padding: 32px;
                    border-bottom: 1px solid #f3f4f6;
                }

                .section:last-child {
                    border-bottom: none;
                }

                .section-title {
                    font-size: 18px;
                    font-weight: 600;
                    color: var(--metallic-bronze);
                    margin-bottom: 20px;
                    display: flex;
                    align-items: center;
                    gap: 8px;
                }

                .description {
                    color: #6b7280;
                    font-size: 15px;
                    line-height: 1.7;
                }

                .description strong {
                    color: var(--korma);
                    font-weight: 600;
                }

                .download-btn {
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                    gap: 8px;
                    padding: 12px 24px;
                    background: var(--husk);
                    color: white;
                    text-decoration: none;
                    border-radius: 8px;
                    font-weight: 500;
                    font-size: 14px;
                    transition: all 0.2s ease;
                    border: 2px solid var(--husk);
                    margin-top: 20px;
                    cursor: pointer;
                }

                .download-btn:hover {
                    background: var(--mandalay);
                    border-color: var(--mandalay);
                    transform: translateY(-1px);
                    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
                }

                .features {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
                    gap: 20px;
                }

                .feature {
                    padding: 20px;
                    border: 1px solid #e5e7eb;
                    border-radius: 12px;
                    background: #fafafa;
                    transition: all 0.2s ease;
                }

                .feature:hover {
                    border-color: var(--calico);
                    background: white;
                    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                    transform: translateY(-1px);
                }

                .feature-icon {
                    font-size: 24px;
                    margin-bottom: 12px;
                }

                .feature-title {
                    font-size: 16px;
                    font-weight: 600;
                    color: var(--metallic-bronze);
                    margin-bottom: 8px;
                }

                .feature-desc {
                    font-size: 14px;
                    color: #6b7280;
                    line-height: 1.5;
                }

                .info-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
                    gap: 24px;
                }

                .info-block {
                    background: #f9fafb;
                    border: 1px solid #e5e7eb;
                    border-radius: 12px;
                    padding: 24px;
                }

                .info-block h4 {
                    font-size: 16px;
                    font-weight: 600;
                    color: var(--metallic-bronze);
                    margin-bottom: 16px;
                    display: flex;
                    align-items: center;
                    gap: 8px;
                }

                .info-list {
                    list-style: none;
                }

                .info-list li {
                    padding: 8px 0;
                    font-size: 14px;
                    color: #6b7280;
                    border-bottom: 1px solid #e5e7eb;
                    position: relative;
                    padding-left: 16px;
                }

                .info-list li:last-child {
                    border-bottom: none;
                }

                .info-list li::before {
                    content: '•';
                    color: var(--husk);
                    position: absolute;
                    left: 0;
                    top: 8px;
                    font-weight: bold;
                }

                .footer {
                    background: #f9fafb;
                    padding: 24px 32px;
                    border-top: 1px solid #e5e7eb;
                    text-align: center;
                }

                .footer h4 {
                    font-size: 16px;
                    font-weight: 600;
                    color: var(--metallic-bronze);
                    margin-bottom: 16px;
                }

                .feedback-links {
                    display: flex;
                    justify-content: center;
                    gap: 16px;
                    flex-wrap: wrap;
                }

                .feedback-link {
                    display: inline-flex;
                    align-items: center;
                    gap: 6px;
                    padding: 8px 16px;
                    background: white;
                    border: 1px solid #e5e7eb;
                    border-radius: 6px;
                    color: var(--korma);
                    text-decoration: none;
                    font-size: 14px;
                    font-weight: 500;
                    transition: all 0.2s ease;
                    cursor: pointer;
                }

                .feedback-link:hover {
                    border-color: var(--husk);
                    background: var(--husk);
                    color: white;
                }

                .note {
                    background: #fef3c7;
                    border: 1px solid #fcd34d;
                    border-radius: 8px;
                    padding: 16px;
                    margin-top: 16px;
                }

                .note-title {
                    font-size: 14px;
                    font-weight: 600;
                    color: #92400e;
                    margin-bottom: 8px;
                }

                .note-content {
                    font-size: 13px;
                    color: #a16207;
                    line-height: 1.5;
                }

                @media (max-width: 768px) {
                    body {
                        padding: 8px;
                    }
                    
                    .section {
                        padding: 20px;
                    }
                    
                    .header {
                        padding: 20px;
                        flex-direction: column; /* Stack items on small screens */
                        align-items: flex-start; /* Align to start when stacked */
                    }
                    .header-content {
                        flex-direction: column; /* Stack logo and text on small screens */
                        align-items: flex-start;
                        gap: 10px; /* Adjust gap for stacked layout */
                    }
                    .logo-container {
                        margin-bottom: 10px; /* Add some space below logo when stacked */
                    }
                    .title {
                        font-size: 20px;
                    }
                    
                    .features {
                        grid-template-columns: 1fr;
                    }
                    
                    .info-grid {
                        grid-template-columns: 1fr;
                    }
                }
            ";
        }

        // Provides the JavaScript logic for the HTML card
        private string GetJavaScript()
        {
            return @"
                function parseMarkdown(text) {
                    if (!text) return '';
                    return text
                        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
                        .replace(/\*(.*?)\*/g, '<em>$1</em>')
                        .replace(/\n\n/g, '</p><p>')
                        .replace(/\n\*/g, '</li><li>')
                        .replace(/\n/g, '<br>');
                }

                function createCard(data) {
                    console.log('Creating card with data:', data);
                    var container = document.getElementById('cardContainer');
                    
                    if (!container) {
                        console.error('Card container not found');
                        return;
                    }
                    
                    // Recreate the header with the logo, assuming logo is part of the initial HTML
                    // Ensure the logo link is also recreated
                    var currentLogoSrc = document.querySelector('.header .logo') ? document.querySelector('.header .logo').src : '';
                    var currentLogoLink = document.querySelector('.header .logo-container a') ? document.querySelector('.header .logo-container a').getAttribute('onclick').match(/handleLogoClick\('([^']+)'\)/) : null;
                    var logoHref = currentLogoLink ? currentLogoLink[1] : '#'; // Fallback to '#' if no link is found

                    var html = '<div class=""header"">' +
                        '<div class=""header-content"">' + // New container for logo and text
                            '<div class=""logo-container"">' +
                                '<a href=""#"" onclick=""handleLogoClick(\'' + logoHref + '\'); return false;"">' +
                                    '<img src=""' + currentLogoSrc + '"" alt=""Logo"" class=""logo"">' +
                                '</a>' +
                            '</div>' +
                            '<div class=""text-content"">' + // New container for text elements
                                '<span class=""version"">' + (data.version || 'N/A') + '</span>' +
                                '<h1 class=""title"">' + (data.title || 'Untitled Release') + '</h1>' +
                                '<div class=""date"">Released ' + (data.release_date || 'Unknown Date') + '</div>' +
                            '</div>' +
                        '</div>' +
                        '</div>';

                    // Notes section
                    if (data.notes) {
                        html += '<div class=""section"">' +
                            '<h2 class=""section-title"">📋 Release Notes</h2>' +
                            '<div class=""description"">' + parseMarkdown(data.notes) + '</div>';
                        
                        if (data.download_link && data.download_link.windows) {
                            html += '<button class=""download-btn"" onclick=""handleDownload(\'' + data.download_link.windows + '\')"">' +
                                '💾 Download for Windows</button>';
                        }
                        
                        html += '</div>';
                    }

                    // Features section
                    if (data.features_overview && data.features_overview.length > 0) {
                        html += '<div class=""section"">' +
                            '<h2 class=""section-title"">✨ Features</h2>' +
                            '<div class=""features"">';
                        
                        for (var i = 0; i < data.features_overview.length; i++) {
                            var feature = data.features_overview[i];
                            html += '<div class=""feature"">' +
                                '<div class=""feature-icon"">' + (feature.icon || '🔧') + '</div>' +
                                '<div class=""feature-title"">' + (feature.name || 'Feature') + '</div>' +
                                '<div class=""feature-desc"">' + (feature.description || 'No description available') + '</div>' +
                                '</div>';
                        }
                        
                        html += '</div></div>';
                    }

                    // Bug fixes and Known issues section
                    var hasBugFixes = data.bug_fixes && data.bug_fixes.length > 0;
                    var hasKnownIssues = data.known_issues && data.known_issues.length > 0;

                    if (hasBugFixes || hasKnownIssues) {
                        html += '<div class=""section""><div class=""info-grid"">';

                        if (hasBugFixes) {
                            html += '<div class=""info-block"">' +
                                '<h4>🐛 Bug Fixes</h4>' +
                                '<ul class=""info-list"">';
                            
                            for (var j = 0; j < data.bug_fixes.length; j++) {
                                html += '<li>' + data.bug_fixes[j] + '</li>';
                            }
                            
                            html += '</ul></div>';
                        }

                        if (hasKnownIssues) {
                            html += '<div class=""info-block"">' +
                                '<h4>⚠️ Known Issues</h4>' +
                                '<ul class=""info-list"">';
                            
                            for (var k = 0; k < data.known_issues.length; k++) {
                                html += '<li>' + data.known_issues[k] + '</li>';
                            }
                            
                            html += '</ul></div>';
                        }

                        html += '</div>';

                        // Developer notes
                        if (data.developer_notes) {
                            html += '<div class=""note"">' +
                                '<div class=""note-title"">Developer Notes</div>' +
                                '<div class=""note-content"">' + data.developer_notes + '</div>' +
                                '</div>';
                        }

                        html += '</div>';
                    }

                    // Footer with feedback channels
                    if (data.feedback_channels && data.feedback_channels.length > 0) {
                        html += '<div class=""footer"">' +
                            '<h4>💬 Feedback & Support</h4>' +
                            '<div class=""feedback-links"">';
                        
                        for (var m = 0; m < data.feedback_channels.length; m++) {
                            var channel = data.feedback_channels[m];
                            if (channel.indexOf('Email:') !== -1) {
                                var email = channel.split('Email: ')[1];
                                html += '<button class=""feedback-link"" onclick=""handleEmail(\'' + email + '\')"">📧 ' + email + '</button>';
                            } else {
                                html += '<span class=""feedback-link"">💬 ' + channel + '</span>';
                            }
                        }
                        
                        html += '</div></div>';
                    }

                    container.innerHTML = html;
                }

                // New JavaScript function to handle logo clicks and communicate with C#
                function handleLogoClick(url) {
                    console.log('Logo clicked, navigating to:', url);
                    try {
                        if (window.chrome && window.chrome.webview) {
                            window.chrome.webview.postMessage(JSON.stringify({
                                type: 'navigate', // New message type
                                url: url
                            }));
                        } else {
                            console.warn('WebView2 messaging not available, opening link directly.');
                            window.open(url, '_blank'); // Fallback for non-WebView2 environments
                        }
                    } catch (error) {
                        console.error('Error handling logo click:', error);
                    }
                }

                function handleDownload(url) {
                    console.log('Download requested:', url);
                    
                    try {
                        if (window.chrome && window.chrome.webview) {
                            window.chrome.webview.postMessage(JSON.stringify({
                                type: 'download',
                                url: url
                            }));
                        } else {
                            console.warn('WebView2 messaging not available');
                            // Fallback for non-WebView2 environments or development
                            window.open(url, '_blank'); 
                        }
                    } catch (error) {
                        console.error('Error handling download:', error);
                    }
                }

                function handleEmail(email) {
                    console.log('Email clicked:', email);
                    
                    try {
                        if (window.chrome && window.chrome.webview) {
                            window.chrome.webview.postMessage(JSON.stringify({
                                type: 'email',
                                email: email
                            }));
                        } else {
                            console.warn('WebView2 messaging not available');
                            // Fallback for non-WebView2 environments or development
                            window.location.href = 'mailto:' + email;
                        }
                    } catch (error) {
                        console.error('Error handling email:', error);
                    }
                }

                function updateCardData(jsonString) {
                    console.log('updateCardData called with:', jsonString);
                    
                    try {
                        var data = JSON.parse(jsonString);
                        console.log('Parsed data:', data);
                        
                        if (Array.isArray(data) && data.length > 0) {
                            createCard(data[0]);
                        } else {
                            createCard(data);
                        }
                    } catch (error) {
                        console.error('Error parsing JSON:', error);
                        var container = document.getElementById('cardContainer');
                        if (container) {
                            container.innerHTML = 
                                '<div class=""section"" style=""text-align: center; padding: 60px 32px;"">' +
                                '<h2 style=""color: #dc2626; margin-bottom: 16px;"">Invalid JSON Data</h2>' +
                                '<p style=""color: #9ca3af;"">Please check your JSON format and try again.</p>' +
                                '<pre style=""background: #f3f4f6; padding: 16px; border-radius: 8px; margin-top: 16px; text-align: left; font-size: 12px; color: #dc2626;"">' + error.message + '</pre>' +
                                '</div>';
                        }
                    }
                }

                // Make function available globally for C# to call
                window.updateCardData = updateCardData;
                
                console.log('JavaScript loaded successfully');
            ";
        }
    }
}
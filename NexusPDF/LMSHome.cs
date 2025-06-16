using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GenerativeAI.Types;
using Newtonsoft.Json.Linq;
using NexusPDF;
using Spire.Doc.Documents;

namespace NexusPDF
{
    public partial class LMSHome : Form
    {
        private readonly SqlHelper _sqlHelper;
        private const string PDF_FILTER = "Supported Files (*.pdf;*.docx;*.pptx)|*.pdf;*.docx;*.pptx";
        private bool OperationCanceled = false;
        private bool isFormOpen = false;
        private string currentOperationId = string.Empty; 
        private readonly object lockObject = new object(); 

        protected override CreateParams CreateParams
        {
            get
            {
                try
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                    return cp;
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error setting CreateParams", ex);
                    throw;
                }
            }
        }

        public LMSHome()
        {
            try
            {
                InitializeComponent();
                _sqlHelper = new SqlHelper(Properties.Settings.Default.ConnectionString);
                InitializeUI();
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.UserPaint, true);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error initializing application", ex);
            }
        }

        private void InitializeUI()
        {
            try
            {
                type.SelectedItem = "Regular Multiple Choice";
                contentDomain.SelectedItem = "Same As PDF";
                language.SelectedItem = "Arabic";
                AIModele.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error initializing UI components", ex);
            }
        }

        private async void Home_Load(object sender, EventArgs e)
        {
            try
            {
                await LoadUserDataAsync();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading user data", ex);
            }
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                var user = await UserInfo.GetUserAsync();
                AccountPic.Load(user.PictureUrl);
                Gmail.Text = user.Email;
                UserName.Text = user.Name;
                await LoadTrialInfoAsync();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading user data", ex);
            }
        }

        private async Task LoadTrialInfoAsync()
        {
            try
            {
                string query = @"
                    SELECT TOP 1 StartDate, TrialExpired 
                    FROM [auth].[TrialInfo] 
                    ORDER BY Id DESC";

                DataTable dt = await _sqlHelper.ExecuteQueryAsync(query);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    starting.Text += Convert.ToDateTime(row["StartDate"]).ToString("yyyy-MM-dd");
                    expiration.Text += Convert.ToBoolean(row["TrialExpired"]) ? "Expired" : "Active";
                    expiration.ForeColor = expiration.Text == "Expired" ? Color.Red : Color.Green;
                }
                else
                {
                    starting.Text = "No data found";
                    expiration.Text = "No data found";
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading trial information", ex);
            }
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            try
            {
                reset();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error resetting form", ex);
            }
        }
        public void reset()
        {
            try
            {
                lock (lockObject)
                {
                    OperationCanceled = false;
                    currentOperationId = string.Empty; // Clear operation ID
                }

                // Reset form controls
                type.SelectedItem = "Regular Multiple Choice";
                contentDomain.SelectedItem = "Same As PDF";
                language.SelectedItem = "Arabic";
                deffnum.Value = 5;
                Path_1.Text = string.Empty;
                Path_2.Text = string.Empty;
                supdate.Text = "Source File Name";
                eupdate.Text = "Example Exam File Name";
                AIModele.SelectedIndex = 0;
                PicPDF1.Image = null;
                PicPDF2.Image = null;
                UploadeProgressBar.Value = 0;

                // Reset panels
                ParentPanal.Controls.Remove(PDFDataPanal);
                ParentPanal.Controls.Add(MainDataPanal);
                PDFDataPanal.Visible = false;
                PDFDataPanal.Enabled = false;

                // Reset buttons
                Prevese.Enabled = false;
                Reset.Enabled = true;
                Next.Enabled = true;
                Next.Text = "Next";
                Cancel.Enabled = false;
                Open.Enabled = false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error resetting form", ex);
            }
        }
    private void ResetForm()
    {
        try
        {
            reset();
            AI.Cleanup();
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error resetting form", ex);
        }
    }

        public async void StartClick()
        {
            lock (lockObject)
            {
                if (!ValidateInputs()) return;

                if (AI.IsOperationRunning)
                {
                    MessageBox.Show("An operation is already in progress. Please wait or cancel it first.",
                                  "Operation In Progress", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Reset all state for new operation
                OperationCanceled = false;
                currentOperationId = Guid.NewGuid().ToString(); // Create unique ID for this operation
            }

            try
            {
                AI.InitializeCancellationToken();
                
                // Update UI for processing state
                SetProcessingUI(true);

                List<string> outputPdfPaths = PdfSplitter.SplitPdfEveryNPages(Path_1.Text);
                List<string> formattedJsons = new List<string>();
                supdate.Text = $"Processing: {Path.GetFileName(Path_1.Text)}";

                string operationId = currentOperationId; // Capture current operation ID
                Open.Enabled = true;
                await ProcessPdfsRecursively(outputPdfPaths, 0, formattedJsons, operationId);

                // Only process results if this operation is still current and not canceled
                lock (lockObject)
                {
                    if (!OperationCanceled &&
                        !AI.CancellationToken.IsCancellationRequested &&
                        currentOperationId == operationId && // Ensure this is still the current operation
                        formattedJsons.Count > 0)
                    {
                        JArray mergedJsonArray = new JArray();
                        foreach (var formattedJson in formattedJsons)
                        {
                            if (!string.IsNullOrEmpty(formattedJson))
                            {
                                var currentJsonArray = JArray.Parse(formattedJson);
                                mergedJsonArray.Merge(currentJsonArray);
                            }
                        }

                        string mergedJson = mergedJsonArray.ToString();
                        if (!string.IsNullOrEmpty(mergedJson))
                        {
                            if (type.SelectedIndex == 4)
                            {
                                mergedJson = JsonExtractorFlashCards.ExtractAndFormatJson(mergedJson);
                            }
                            else
                            {
                                mergedJson = JsonExtractor.ExtractAndFormatJson(mergedJson);
                            }

                            // Final check before processing response
                            if (!OperationCanceled &&
                                !AI.CancellationToken.IsCancellationRequested &&
                                currentOperationId == operationId)
                            {
                                ProcessResponse(mergedJson, operationId);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                HandleCancellation("Operation was canceled by user.");
            }
            catch (Exception ex)
            {
                HandleException("Error generating content", ex);
            }
            finally
            {
                // Ensure UI is properly reset if operation was canceled or failed
                lock (lockObject)
                {
                    if (OperationCanceled || AI.CancellationToken.IsCancellationRequested)
                    {
                        SetProcessingUI(false);
                    }
                }
            }
        }

        private async Task ProcessPdfsRecursively(List<string> pdfPaths, int currentIndex, List<string> formattedJsons, string operationId)
        {
            // Check if operation is still current and not canceled
            lock (lockObject)
            {
                if (currentIndex >= pdfPaths.Count ||
                    OperationCanceled ||
                    currentOperationId != operationId)
                {
                    return;
                }
            }

            AI.CancellationToken.ThrowIfCancellationRequested();

            var pdfPath = pdfPaths[currentIndex];

            try
            {
                var sourceFile = await UploadFileAsync(pdfPath, operationId);
                if (IsOperationInvalid(operationId)) return;

                var exampleFile = await GetExampleFileAsync(operationId);
                if (IsOperationInvalid(operationId)) return;

                if (exampleFile != null)
                    eupdate.Text = $"Example: {Path.GetFileName(Path_2.Text)}";

                var response = await GenerateContent(sourceFile, exampleFile, operationId);
                if (IsOperationInvalid(operationId)) return;

                AI.CancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(500, AI.CancellationToken);

                // Update progress only if operation is still current
                lock (lockObject)
                {
                    if (currentOperationId == operationId && !OperationCanceled)
                    {
                        supdate.Text = $"Processed: {Path.GetFileName(pdfPath)} ({currentIndex + 1}/{pdfPaths.Count})";
                        UploadeProgressBar.Value = (currentIndex + 1) * 100 / pdfPaths.Count;
                    }
                }

                if (!string.IsNullOrEmpty(response) && !IsOperationInvalid(operationId))
                {
                    string formattedJson = type.SelectedIndex == 4
                        ? JsonExtractorFlashCards.ExtractAndFormatJson(response)
                        : JsonExtractor.ExtractAndFormatJson(response);

                    if (!string.IsNullOrEmpty(formattedJson))
                    {
                        lock (lockObject)
                        {
                            if (currentOperationId == operationId && !OperationCanceled)
                            {
                                formattedJsons.Add(formattedJson);
                            }
                        }
                    }
                }

                // Continue to next PDF only if operation is still current
                if (!IsOperationInvalid(operationId))
                {
                    await ProcessPdfsRecursively(pdfPaths, currentIndex + 1, formattedJsons, operationId);
                }
            }
            catch (OperationCanceledException)
            {
                lock (lockObject)
                {
                    OperationCanceled = true;
                }
                throw;
            }
            catch (Exception ex)
            {
                if (!IsOperationInvalid(operationId))
                {
                    ShowErrorMessage($"Error processing PDF {currentIndex + 1}/{pdfPaths.Count}", ex);
                    await ProcessPdfsRecursively(pdfPaths, currentIndex + 1, formattedJsons, operationId);
                }
            }
        }

        private void ProcessResponse(string response, string operationId)
        {
            // Critical check: Only process if this is still the current operation
            lock (lockObject)
            {
                if (OperationCanceled ||
                    AI.CancellationToken.IsCancellationRequested ||
                    currentOperationId != operationId ||
                    string.IsNullOrEmpty(response))
                {
                    return; // Exit silently - operation is no longer valid
                }
            }

            try
            {
                isFormOpen = true;
                Next.Text = "Exam is already open";
                Next.Enabled = false;
                Reset.Enabled = false;
                Cancel.Enabled = false;

                if (type.SelectedIndex != 4)
                {
                    var aShow = new QAShow(response);
                    aShow.Show();
                    aShow.FormClosed += (sender, e) => HandleFormClosed();
                }
                else
                {
                    var aShow = new FlashCards(response);
                    aShow.Show();
                    aShow.FormClosed += (sender, e) => HandleFormClosed();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error processing response", ex);
                ResetForm();
            }
        }

        private void HandleFormClosed()
        {
            isFormOpen = false;
            ResetForm();
        }
        private bool ValidateInputs()
        {
            try
            {
                if (new[] { type, contentDomain, language }.Any(c => c.SelectedItem == null))
                {
                    ShowErrorMessage("Please fill all required fields");
                    return false;
                }

                // Validate Path_1 (source file) only when ready to start upload
                if (Next.Text == "Start Uploade")
                {
                    if (string.IsNullOrEmpty(Path_1.Text))
                    {
                        ShowErrorMessage("Please select a source file in the Image Box Double Click");
                        return false;
                    }

                    if (!File.Exists(Path_1.Text))
                    {
                        ShowErrorMessage("Source file does not exist. Please select a valid file.");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error validating inputs", ex);
                return false;
            }
        }

        private async Task<RemoteFile> GetExampleFileAsync(string operationId)
        {
            try
            {
                if (string.IsNullOrEmpty(Path_2.Text) || !File.Exists(Path_2.Text))
                    return null;
                AI.CancellationToken.ThrowIfCancellationRequested();
                return await UploadFileAsync(Path_2.Text, operationId);
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw to propagate cancellation
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error uploading example file", ex);
                return null;
            }
        }

        private async Task<RemoteFile> UploadFileAsync(string path, string operationId)
        {
            try
            {
                AI.AIModels = AIModele.SelectedItem.ToString();
                AI.GeminiModel = AI.CreateGeminiModel(AI.AIModels);

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    throw new FileNotFoundException("Invalid file path", path);

                if (IsOperationInvalid(operationId))
                    throw new OperationCanceledException();

                AI.CancellationToken.ThrowIfCancellationRequested();

                var result = await AI.GeminiModel.Files.UploadFileAsync(path);
                return result;
            }
            catch (OperationCanceledException)
            {
                lock (lockObject)
                {
                    OperationCanceled = true;
                }
                throw;
            }
            catch (Exception ex)
            {
                if (IsOperationInvalid(operationId))
                {
                    throw new OperationCanceledException();
                }

                // Show retry dialog
                DialogResult result = MessageBox.Show(
                    $"Error uploading file: {Path.GetFileName(path)}\n\n{ex.Message}\n\nDo you want to try again?",
                    "Upload Error",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);

                if (result == DialogResult.Yes)
                {
                    // Check if operation is still valid before retrying
                    if (IsOperationInvalid(operationId))
                    {
                        throw new OperationCanceledException();
                    }

                    // Recursively call the same function to retry
                    return await UploadFileAsync(path, operationId);
                }
                else
                {
                    // User chose not to retry - cancel the operation
                    lock (lockObject)
                    {
                        OperationCanceled = true;
                    }
                    throw new OperationCanceledException("User chose to cancel after upload error.");
                }
            }
        }

        private async Task<string> GenerateContent(RemoteFile source, RemoteFile example, string operationId)
        {
            try
            {
                if (IsOperationInvalid(operationId))
                    throw new OperationCanceledException();

                AI.CancellationToken.ThrowIfCancellationRequested();

                string result = example == null
                    ? await AI.GenerateQAContentAsync(source, GetParameters())
                    : await AI.GenerateQAContentAsync(source, example, GetParameters());

                return result;
            }
            catch (OperationCanceledException)
            {
                lock (lockObject)
                {
                    OperationCanceled = true;
                }
                throw;
            }
            catch (Exception ex)
            {
                if (IsOperationInvalid(operationId))
                {
                    throw new OperationCanceledException();
                }

                // Show retry dialog
                DialogResult result = MessageBox.Show(
                    $"Error generating content from files.\n\n{ex.Message}\n\nDo you want to try again?",
                    "Content Generation Error",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);

                if (result == DialogResult.Yes)
                {
                    // Check if operation is still valid before retrying
                    if (IsOperationInvalid(operationId))
                    {
                        throw new OperationCanceledException();
                    }

                    // Recursively call the same function to retry
                    return await GenerateContent(source, example, operationId);
                }
                else
                {
                    // User chose not to retry - cancel the operation
                    lock (lockObject)
                    {
                        OperationCanceled = true;
                    }
                    AI.CancelOperation();
                    ResetForm();
                    throw new OperationCanceledException("User chose to cancel after content generation error.");
                }
            }
        }

        private bool IsOperationInvalid(string operationId)
        {
            lock (lockObject)
            {
                return OperationCanceled ||
                       AI.CancellationToken.IsCancellationRequested ||
                       currentOperationId != operationId ||
                       string.IsNullOrEmpty(currentOperationId);
            }
        }

        private void HandleCancellation(string message)
        {
            lock (lockObject)
            {
                OperationCanceled = true;
                currentOperationId = string.Empty;
            }

            ResetForm();
            MessageBox.Show(message, "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void HandleException(string message, Exception ex)
        {
            lock (lockObject)
            {
                OperationCanceled = true;
                currentOperationId = string.Empty;
            }

            ShowErrorMessage(message, ex);
            ResetForm();
        }

        private void SetProcessingUI(bool isProcessing)
        {
            if (isProcessing)
            {
                Next.Text = "Processing...";
                Next.Enabled = false;
                Reset.Enabled = false;
                Cancel.Enabled = true;
                UploadeProgressBar.Value = 0;
            }
            else
            {
                Next.Text = "Next";
                Next.Enabled = true;
                Reset.Enabled = true;
                Cancel.Enabled = false;
            }
        }


        private GenerationQAParameters GetParameters()
        {
            try
            {
                return new GenerationQAParameters
                {
                    Language = GetSelectedLanguage(),
                    Type = GetSelectedType(),
                    Difficulty = GetDifficulty(),
                    ContentDomain = GetContentDomain()
                };
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error retrieving generation parameters", ex);
                return null;
            }
        }

        private string GetSelectedLanguage() => language.SelectedItem.ToString();
        private string GetSelectedType() => type.SelectedItem.ToString();
        private string GetContentDomain() => contentDomain.SelectedItem.ToString();
        private int GetDifficulty() => (int)deffnum.Value;

        private void ShowErrorMessage(string message, Exception ex = null)
        {
            MessageBox.Show(
                ex == null ? message : $"{message}\n{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        #region Event Handlers

        private void dragimage_DragDrop(object sender, DragEventArgs e) => SharedDragDropLogic(e);
        private void dragimage_DragEnter(object sender, DragEventArgs e) => SharedDragEnterLogic(e);
        private void dragimage_DoubleClick(object sender, EventArgs e) => SharedDoubleClickLogic();

        private void SharedDragDropLogic(DragEventArgs e)
        {
            if (Next.Enabled)
            {
                try
                {
                    if (BothPathsFilled())
                    {
                        ShowErrorMessage("Maximum 2 files allowed");
                        return;
                    }
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (IsValidDrop(files))
                    {
                        string filePath = files[0];
                        string convertedPath = ConvertToPdfIfNeeded(filePath);
                        if (!string.IsNullOrEmpty(convertedPath))
                            UpdatePathLabels(convertedPath);
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error handling drag-and-drop operation", ex);
                }
            }
        }

        private void SharedDragEnterLogic(DragEventArgs e)
        {
            if (Next.Enabled)
            {
                try
                {
                    if (BothPathsFilled())
                    {
                        ShowErrorMessage("Maximum 2 files allowed");
                        return;
                    }
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        e.Effect = IsValidDrop(files) ? DragDropEffects.Copy : DragDropEffects.None;
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error handling drag-enter operation", ex);
                }
            }
        }

        private void SharedDoubleClickLogic()
        {
            if (Next.Enabled)
            {
                try
                {
                    if (BothPathsFilled())
                    {
                        ShowErrorMessage("Maximum 2 files allowed");
                        return;
                    }
                    using (var openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Title = "Select a File";
                        openFileDialog.Filter = PDF_FILTER;
                        openFileDialog.Multiselect = false;

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            string convertedPath = ConvertToPdfIfNeeded(openFileDialog.FileName);
                            if (!string.IsNullOrEmpty(convertedPath))
                                UpdatePathLabels(convertedPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error selecting file", ex);
                }
            }
        }

        private bool BothPathsFilled() =>
            !string.IsNullOrEmpty(Path_1.Text) && !string.IsNullOrEmpty(Path_2.Text);

        private bool IsValidDrop(string[] files)
        {
            try
            {
                return files.Length == 1 &&
                       (System.IO.Path.GetExtension(files[0]).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
                        System.IO.Path.GetExtension(files[0]).Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
                        System.IO.Path.GetExtension(files[0]).Equals(".pptx", StringComparison.OrdinalIgnoreCase)) &&
                       !BothPathsFilled();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error validating dropped files", ex);
                return false;
            }
        }

        private string ConvertToPdfIfNeeded(string inputPath)
        {
            try
            {
                string extension = System.IO.Path.GetExtension(inputPath).ToLower();
                if (extension == ".pdf")
                    return inputPath;

                switch (extension)
                {
                    case ".docx":
                        return PDF.ConvertWordToPdf(inputPath);
                    case ".pptx":
                        return PDF.ConvertPowerPointToPdf(inputPath);
                    default:
                        ShowErrorMessage("Unsupported file format");
                        return null;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Conversion failed", ex);
                return null;
            }
        }

        private void UpdatePathLabels(string filePath)
        {
            try
            {
                if (BothPathsFilled())
                {
                    ShowErrorMessage("Maximum 2 files allowed");
                    return;
                }

                if (string.IsNullOrEmpty(Path_1.Text))
                {
                    Path_1.Text = filePath;
                    PicPDF1.Image = PDF.FirstPage(filePath);
                }
                else
                {
                    Path_2.Text = filePath;
                    PicPDF2.Image = PDF.FirstPage(filePath);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Failed to load preview", ex);
            }
        }

        private void Next_Click(object sender, EventArgs e)
        {
            if (Next.Text == "Next")
            {
                AI.LanguageAI = language.SelectedItem.ToString();
                if (!ValidateInputs()) return;
                ParentPanal.Controls.Remove(MainDataPanal);
                ParentPanal.Controls.Add(PDFDataPanal);
                PDFDataPanal.Dock = DockStyle.Fill;
                PDFDataPanal.Visible = true;
                PDFDataPanal.Enabled = true;
                Prevese.Enabled = true;
                Next.Text = "Start Uploade";
                return;
            }
            if (Next.Text == "Start Uploade")
            {
                Next.Enabled = false;
                Prevese.Enabled = false;
                StartClick();
            }
        }

        private void Prevese_Click(object sender, EventArgs e)
        {
            ParentPanal.Controls.Remove(PDFDataPanal);
            ParentPanal.Controls.Add(MainDataPanal);
            PDFDataPanal.Visible = false;
            PDFDataPanal.Enabled = false;
            Prevese.Enabled = false;
            Next.Enabled = true;
            Next.Text = "Next";
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to cancel the request?",
                "Cancel Request", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dialogResult == DialogResult.Yes)
            {
                OperationCanceled = true; // Set the flag first
                AI.CancelOperation();
                ResetForm();
            }
        }

        #endregion
        private void Open_Click(object sender, EventArgs e)
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
    }
}

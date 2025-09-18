using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace NexusPDF
{
    public class PdfSplitter
    {
        // Static variable to store the current PDF-specific directory path
        public static string CurrentPdfDirectory { get; private set; } = string.Empty;

        public static List<string> SplitPdfEveryNPages(string inputPdfPath)
        {
            List<string> outputPaths = new List<string>();
            int pagesPerSplit = 20;
            string outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Combine the base directory with the "temppdf" folder
            string tempPdfDirectory = Path.Combine(outputDirectory, "TempPDF");

            // Get the original PDF name without extension
            string originalPdfName = Path.GetFileNameWithoutExtension(inputPdfPath);

            // Enhanced sanitization for file names, especially handling Unicode characters
            string sanitizedPdfName = SanitizeFileName(originalPdfName);

            // Create a folder with the sanitized name of the original PDF inside TempPDF
            string pdfSpecificDirectory = Path.Combine(tempPdfDirectory, sanitizedPdfName);

            // Store the path in the static variable for external access
            CurrentPdfDirectory = pdfSpecificDirectory;

            try
            {
                // Check if the input PDF file actually exists before proceeding
                if (!File.Exists(inputPdfPath))
                {
                    MessageBox.Show($"The input PDF file was not found:\n{inputPdfPath}\nPlease ensure the file exists and the path is correct.",
                                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return outputPaths; // Return empty list as no operation can be performed
                }

                // Ensure TempPDF directory exists
                if (!Directory.Exists(tempPdfDirectory))
                {
                    Directory.CreateDirectory(tempPdfDirectory);
                }

                // Delete the PDF-specific folder if it exists, then create a new one
                // This ensures a clean slate for each split operation
                if (Directory.Exists(pdfSpecificDirectory))
                {
                    try
                    {
                        Directory.Delete(pdfSpecificDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not delete existing directory:\n{pdfSpecificDirectory}\n\nError: {ex.Message}\n\nPlease close any files that might be open from this directory and try again.",
                                        "Directory Access Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return outputPaths;
                    }
                }

                // Create the directory with error handling
                try
                {
                    Directory.CreateDirectory(pdfSpecificDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not create directory:\n{pdfSpecificDirectory}\n\nError: {ex.Message}\n\nPlease check permissions and try again.",
                                    "Directory Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return outputPaths;
                }

                // Open the existing PDF document
                using (PdfDocument inputPdf = PdfReader.Open(inputPdfPath, PdfDocumentOpenMode.Import))
                {
                    int totalPages = inputPdf.PageCount;
                    // Calculate how many split PDFs will be created based on pagesPerSplit
                    int splitCount = (int)Math.Ceiling((double)totalPages / pagesPerSplit);

                    for (int splitIndex = 0; splitIndex < splitCount; splitIndex++)
                    {
                        // Create a new PDF document for each split part
                        PdfDocument outputPdf = new PdfDocument();

                        // Add pages to the current split PDF
                        // This loop iterates through the pages for the current split segment
                        for (int pageIndex = splitIndex * pagesPerSplit; pageIndex < (splitIndex + 1) * pagesPerSplit && pageIndex < totalPages; pageIndex++)
                        {
                            outputPdf.AddPage(inputPdf.Pages[pageIndex]);
                        }

                        // Define the output path for the split PDF with a standardized naming convention:
                        // originalname.split (i).pdf, where 'i' is the split part number
                        string outputFileName = $"{sanitizedPdfName}.split ({splitIndex + 1}).pdf";
                        string outputPdfPath = Path.Combine(pdfSpecificDirectory, outputFileName);

                        // Additional check to ensure the full path isn't too long
                        if (outputPdfPath.Length > 260)
                        {
                            // Truncate the sanitized name if the path is too long
                            int maxNameLength = 260 - pdfSpecificDirectory.Length - ".split (999).pdf".Length - 1;
                            if (maxNameLength > 0)
                            {
                                sanitizedPdfName = sanitizedPdfName.Substring(0, Math.Min(sanitizedPdfName.Length, maxNameLength));
                                outputFileName = $"{sanitizedPdfName}.split ({splitIndex + 1}).pdf";
                                outputPdfPath = Path.Combine(pdfSpecificDirectory, outputFileName);
                            }
                        }

                        try
                        {
                            // Save the newly created split PDF
                            outputPdf.Save(outputPdfPath);

                            // Add the path of the saved split PDF to the list of output paths
                            outputPaths.Add(outputPdfPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error saving split PDF {splitIndex + 1}:\n{ex.Message}\n\nPath: {outputPdfPath}",
                                            "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            // Dispose of the output PDF document to release resources
                            outputPdf.Dispose();
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                // Handle file input/output exceptions (e.g., file not found, access denied, invalid path)
                MessageBox.Show($"An error occurred while accessing the file:\n{ioEx.Message}\n\nPath: {inputPdfPath}\n\nPlease check if the file exists or if you have the necessary permissions.",
                                "File I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Handle unauthorized access exceptions (e.g., permission issues when writing to a directory)
                MessageBox.Show($"Access to the file was denied:\n{uaEx.Message}\n\nPlease check if you have the required permissions or try running the program as an administrator.",
                                "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions that might occur during the process
                MessageBox.Show($"An unexpected error occurred:\n{ex.Message}\n\nPlease try again later or contact support if the issue persists.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Return the list of paths to the split PDFs. This list will be empty if an exception occurred
            // before any files could be successfully created.
            return outputPaths;
        }

        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid characters and handling Unicode properly
        /// </summary>
        /// <param name="fileName">The original filename to sanitize</param>
        /// <returns>A sanitized filename safe for use in file systems</returns>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "UnnamedPDF";
            }

            // Start with the original filename
            string sanitized = fileName.Trim();

            // Replace invalid file name characters with underscores
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Replace invalid path characters with underscores
            foreach (char c in Path.GetInvalidPathChars())
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Remove or replace other potentially problematic characters
            sanitized = sanitized.Replace(":", "_");
            sanitized = sanitized.Replace("*", "_");
            sanitized = sanitized.Replace("?", "_");
            sanitized = sanitized.Replace("\"", "_");
            sanitized = sanitized.Replace("<", "_");
            sanitized = sanitized.Replace(">", "_");
            sanitized = sanitized.Replace("|", "_");

            // Handle multiple consecutive underscores
            sanitized = Regex.Replace(sanitized, "_+", "_");

            // Remove leading/trailing underscores and dots
            sanitized = sanitized.Trim('_', '.', ' ');

            // Ensure we don't have an empty string
            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "UnnamedPDF";
            }

            // Limit length to avoid path length issues
            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100);
            }

            return sanitized;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
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
            int pagesPerSplit = 10;
            string outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Combine the base directory with the "temppdf" folder
            string tempPdfDirectory = Path.Combine(outputDirectory, "TempPDF");

            // Get the original PDF name without extension
            string originalPdfName = Path.GetFileNameWithoutExtension(inputPdfPath);

            // Create a folder with the same name as the original PDF inside TempPDF
            string pdfSpecificDirectory = Path.Combine(tempPdfDirectory, originalPdfName);

            // Store the path in the static variable for external access
            CurrentPdfDirectory = pdfSpecificDirectory;

            try
            {
                // Ensure TempPDF directory exists
                if (!Directory.Exists(tempPdfDirectory))
                {
                    Directory.CreateDirectory(tempPdfDirectory);
                }

                // Delete the PDF-specific folder if it exists, then create a new one
                if (Directory.Exists(pdfSpecificDirectory))
                {
                    Directory.Delete(pdfSpecificDirectory, true);
                }
                Directory.CreateDirectory(pdfSpecificDirectory);

                // Open the existing PDF
                using (PdfDocument inputPdf = PdfReader.Open(inputPdfPath, PdfDocumentOpenMode.Import))
                {
                    int totalPages = inputPdf.PageCount;
                    int splitCount = (int)Math.Ceiling((double)totalPages / pagesPerSplit);  // Calculate how many split PDFs we need

                    for (int splitIndex = 0; splitIndex < splitCount; splitIndex++)
                    {
                        // Create a new PDF document for each split
                        PdfDocument outputPdf = new PdfDocument();

                        // Add pages to the split PDF (group of pages)
                        for (int pageIndex = splitIndex * pagesPerSplit; pageIndex < (splitIndex + 1) * pagesPerSplit && pageIndex < totalPages; pageIndex++)
                        {
                            outputPdf.AddPage(inputPdf.Pages[pageIndex]);
                        }

                        // Define the output path with the new naming convention: originalname.split (i).pdf
                        string outputPdfPath = Path.Combine(pdfSpecificDirectory, $"{originalPdfName}.split ({splitIndex + 1}).pdf");

                        // Save the split PDF
                        outputPdf.Save(outputPdfPath);

                        // Add the path to the list
                        outputPaths.Add(outputPdfPath);

                        // Dispose of the output PDF to free resources
                        outputPdf.Dispose();
                    }
                }
            }
            catch (IOException ioEx)
            {
                // Handle file input/output exceptions (e.g., file not found, access denied)
                MessageBox.Show($"An error occurred while accessing the file:\n{ioEx.Message}\nPlease check if the file exists or if you have the necessary permissions.",
                                "File I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Handle unauthorized access (e.g., permission issues)
                MessageBox.Show($"Access to the file was denied:\n{uaEx.Message}\nPlease check if you have the required permissions or try running the program as an administrator.",
                                "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                MessageBox.Show($"An unexpected error occurred:\n{ex.Message}\nPlease try again later or contact support if the issue persists.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Return the list of paths to the split PDFs, even if an exception occurred
            return outputPaths;
        }
    }
}
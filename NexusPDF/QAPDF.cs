using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using static NexusPDF.QuestionsOBJ;
using System.Runtime.InteropServices;

namespace NexusPDF
{
    public class QAPDF
    {
        // Temporary folder to save temporary Word files and final PDF files in the application directory
        private static readonly string TempOutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedDocuments");

        /// <summary>
        /// Generates a PDF document from a JSON string containing questions.
        /// It creates a temporary Word file first, then converts it to PDF.
        /// </summary>
        /// <param name="jsonString">The JSON string containing question data.</param>
        /// <param name="fileNameWithoutExtension">The output file name without extension (e.g., MyQuestions).</param>
        public static void GeneratePdfDocument(string jsonString, string fileNameWithoutExtension)
        {
            string tempWordPath = Path.Combine(TempOutputFolder, $"{fileNameWithoutExtension}_{Guid.NewGuid()}.docx");
            string finalPdfPath = Path.Combine(TempOutputFolder, $"{fileNameWithoutExtension}.pdf");

            try
            {
                // Ensure the temporary folder exists, create it if it doesn't
                if (!Directory.Exists(TempOutputFolder))
                {
                    Directory.CreateDirectory(TempOutputFolder);
                }

                // Parse the JSON string into question objects
                var (result, type) = QuestionsOBJ.FromJson(jsonString);

                if (type == QuestionType.None)
                {
                    MessageBox.Show("No valid questions found in the JSON file.", "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create a temporary Word document
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(tempWordPath, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                    // Create document structure
                    DocumentFormat.OpenXml.Wordprocessing.Body body = new DocumentFormat.OpenXml.Wordprocessing.Body();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(body);

                    // Add styles for better formatting
                    AddDocumentStyles(mainPart);

                    // Add document title
                    AddTitle(body);

                    // Process questions based on type
                    if (type == QuestionType.OptionQuestions && result.OptionQuestions != null)
                    {
                        ProcessOptionQuestions(body, result.OptionQuestions);
                    }
                    else if (type == QuestionType.YesNoQuestions && result.YesNoQuestions != null)
                    {
                        ProcessYesNoQuestions(body, result.YesNoQuestions);
                    }

                    // Save the Word document
                    mainPart.Document.Save();
                }

                // Convert the temporary Word document to PDF
                ConvertWordToPdf(tempWordPath, finalPdfPath);

                MessageBox.Show($"تم إنشاء مستند PDF بنجاح في: {finalPdfPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON parsing error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while generating the PDF document: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Clean up the temporary Word file
                if (File.Exists(tempWordPath))
                {
                    File.Delete(tempWordPath);
                }
            }
        }

        /// <summary>
        /// Adds document styles for Arabic text support
        /// </summary>
        private static void AddDocumentStyles(MainDocumentPart mainPart)
        {
            StyleDefinitionsPart stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            DocumentFormat.OpenXml.Wordprocessing.Styles styles = new DocumentFormat.OpenXml.Wordprocessing.Styles();

            // Default paragraph style with Arabic support
            DocumentFormat.OpenXml.Wordprocessing.Style defaultStyle = new DocumentFormat.OpenXml.Wordprocessing.Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Normal",
                Default = true
            };

            StyleName styleName = new StyleName() { Val = "Normal" };
            StyleRunProperties styleRunProps = new StyleRunProperties();
            RunFonts runFonts = new RunFonts()
            {
                Ascii = "Arial Unicode MS",
                HighAnsi = "Arial Unicode MS",
                ComplexScript = "Arial Unicode MS"
            };
            styleRunProps.Append(runFonts);
            defaultStyle.Append(styleName, styleRunProps);
            styles.Append(defaultStyle);

            stylesPart.Styles = styles;
        }

        /// <summary>
        /// Adds the document title
        /// </summary>
        private static void AddTitle(DocumentFormat.OpenXml.Wordprocessing.Body body)
        {
            DocumentFormat.OpenXml.Wordprocessing.Paragraph titleParagraph = CreateParagraph("أسئلة وإجابات", true, "48", "4F81BD", true, JustificationValues.Center);
            ParagraphProperties titleProps = titleParagraph.GetFirstChild<ParagraphProperties>();
            if (titleProps == null)
            {
                titleProps = new ParagraphProperties();
                titleParagraph.PrependChild(titleProps);
            }
            titleProps.SpacingBetweenLines = new SpacingBetweenLines() { After = "360" };
            body.Append(titleParagraph);
        }

        /// <summary>
        /// Processes option-based questions
        /// </summary>
        private static void ProcessOptionQuestions(DocumentFormat.OpenXml.Wordprocessing.Body body, List<OptionQuestion> questions)
        {
            int questionNumber = 1;
            foreach (var question in questions)
            {
                // Add question
                body.Append(CreateParagraph($"{questionNumber}. {question.Question}", true, "28", "365F91", true));

                // Add options
                foreach (var option in question.Options)
                {
                    body.Append(CreateParagraph($"    {option}", false, "24", "000000", true));
                }

                // Add correct answer
                body.Append(CreateParagraph($"الإجابة الصحيحة: {question.GetCorrectAnswer()}", true, "24", "00B050", true));

                // Add explanation if available
                if (!string.IsNullOrEmpty(question.Explanation))
                {
                    body.Append(CreateParagraph($"الشرح: {question.Explanation}", false, "22", "808080", true, null, true));
                }

                // Add details
                string details = $"المصدر: {question.Source ?? "غير متوفر"} | الصعوبة: {question.Difficulty ?? "غير متوفر"} | المجال: {question.Domain ?? "غير متوفر"}";
                body.Append(CreateParagraph(details, false, "20", "A0A0A0", true));

                // Add page break (except for last question)
                if (questionNumber < questions.Count)
                {
                    body.Append(CreatePageBreak());
                }

                questionNumber++;
            }
        }

        /// <summary>
        /// Processes yes/no questions
        /// </summary>
        private static void ProcessYesNoQuestions(DocumentFormat.OpenXml.Wordprocessing.Body body, List<YesNoQuestion> questions)
        {
            int questionNumber = 1;
            foreach (var question in questions)
            {
                // Add question
                body.Append(CreateParagraph($"{questionNumber}. {question.Question}", true, "28", "365F91", true));

                // Add correct answer
                body.Append(CreateParagraph($"الإجابة الصحيحة: {question.GetCorrectAnswer()}", true, "24", "00B050", true));

                // Add explanation if available
                if (!string.IsNullOrEmpty(question.Explanation))
                {
                    body.Append(CreateParagraph($"الشرح: {question.Explanation}", false, "22", "808080", true, null, true));
                }

                // Add details
                string details = $"المصدر: {question.Source ?? "غير متوفر"} | الصعوبة: {question.Difficulty ?? "غير متوفر"} | المجال: {question.Domain ?? "غير متوفر"}";
                body.Append(CreateParagraph(details, false, "20", "A0A0A0", true));

                // Add page break (except for last question)
                if (questionNumber < questions.Count)
                {
                    body.Append(CreatePageBreak());
                }

                questionNumber++;
            }
        }

        /// <summary>
        /// Creates a formatted paragraph with Arabic support
        /// </summary>
        private static DocumentFormat.OpenXml.Wordprocessing.Paragraph CreateParagraph(string text, bool bold, string fontSize, string color, bool isRTL, JustificationValues? justification = null, bool italic = false)
        {
            DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();

            // Create paragraph properties
            ParagraphProperties paraProps = new ParagraphProperties();
            if (isRTL)
            {
                paraProps.BiDi = new BiDi();
            }
            if (justification.HasValue)
            {
                paraProps.Justification = new Justification() { Val = justification.Value };
            }
            paragraph.Append(paraProps);

            // Create run with formatting
            Run run = new Run();
            RunProperties runProps = new RunProperties();

            // Add Arabic font support
            runProps.RunFonts = new RunFonts()
            {
                Ascii = "Arial Unicode MS",
                HighAnsi = "Arial Unicode MS",
                ComplexScript = "Arial Unicode MS"
            };

            if (isRTL)
            {
                runProps.RightToLeftText = new RightToLeftText();
            }

            if (bold)
            {
                runProps.Bold = new Bold();
            }

            if (italic)
            {
                runProps.Italic = new Italic();
            }

            if (!string.IsNullOrEmpty(fontSize))
            {
                runProps.FontSize = new FontSize() { Val = fontSize };
            }

            if (!string.IsNullOrEmpty(color))
            {
                runProps.Color = new Color() { Val = color };
            }

            run.RunProperties = runProps;
            run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
            paragraph.Append(run);

            return paragraph;
        }

        /// <summary>
        /// Creates a page break paragraph
        /// </summary>
        private static DocumentFormat.OpenXml.Wordprocessing.Paragraph CreatePageBreak()
        {
            return new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new Run(new DocumentFormat.OpenXml.Wordprocessing.Break() { Type = BreakValues.Page }));
        }

        /// <summary>
        /// Converts a Word document to PDF format.
        /// Requires Microsoft Office to be installed on the machine.
        /// </summary>
        /// <param name="inputWordPath">The full path to the input Word document.</param>
        /// <param name="outputPdfPath">The full path where the PDF document will be saved.</param>
        private static void ConvertWordToPdf(string inputWordPath, string outputPdfPath)
        {
            if (!File.Exists(inputWordPath))
            {
                throw new FileNotFoundException($"Word file not found: {inputWordPath}");
            }

            Microsoft.Office.Interop.Word.Application wordApp = null;
            Microsoft.Office.Interop.Word.Document wordDoc = null;
            object missing = System.Reflection.Missing.Value;

            try
            {
                // Create an instance of the Word application
                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = false;

                // Open the Word document
                object inputFile = inputWordPath;
                wordDoc = wordApp.Documents.Open(ref inputFile,
                                                 ref missing, ref missing, ref missing, ref missing, ref missing,
                                                 ref missing, ref missing, ref missing, ref missing, ref missing,
                                                 ref missing, ref missing, ref missing, ref missing, ref missing);

                // Save the document as a PDF file
                object outputFilePath = outputPdfPath;
                object fileFormat = Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatPDF;
                wordDoc.SaveAs2(ref outputFilePath,
                                ref fileFormat, ref missing, ref missing, ref missing, ref missing,
                                ref missing, ref missing, ref missing, ref missing, ref missing,
                                ref missing, ref missing, ref missing, ref missing, ref missing,
                                ref missing);
            }
            catch (Exception ex)
            {
                // Delete the temporary PDF file if an error occurs
                if (File.Exists(outputPdfPath))
                {
                    File.Delete(outputPdfPath);
                }
                throw new Exception($"Failed to convert Word to PDF: {ex.Message}", ex);
            }
            finally
            {
                // Close the document and Word application and release COM objects
                if (wordDoc != null)
                {
                    object saveChanges = Microsoft.Office.Interop.Word.WdSaveOptions.wdDoNotSaveChanges;
                    wordDoc.Close(ref saveChanges, ref missing, ref missing);
                    Marshal.ReleaseComObject(wordDoc);
                    wordDoc = null;
                }
                if (wordApp != null)
                {
                    wordApp.Quit(ref missing, ref missing, ref missing);
                    Marshal.ReleaseComObject(wordApp);
                    wordApp = null;
                }

                // Run garbage collector to ensure resources are released
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
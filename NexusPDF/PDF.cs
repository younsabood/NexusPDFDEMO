using System.Drawing;
using PdfiumViewer;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Word; 
using Microsoft.Office.Interop.PowerPoint; 
using Microsoft.Office.Core; 

namespace NexusPDF
{
    public static class PDF
    {
        private static readonly string TempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempPDF");

        static PDF()
        {
            try
            {
                // التأكد من وجود مجلد مؤقت لملفات PDF المحولة
                if (!Directory.Exists(TempFolder))
                {
                    Directory.CreateDirectory(TempFolder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في إنشاء المجلد المؤقت: {ex.Message}");
                // يمكن التعامل مع هذا الخطأ بشكل أفضل، ربما عن طريق رمي استثناء أو تسجيله
            }
        }
        public static Image FirstPage(string filePath)
        {
            try
            {
                using (var pdfDoc = PdfiumViewer.PdfDocument.Load(filePath))
                {
                    if (pdfDoc.PageCount > 0)
                    {
                        return pdfDoc.Render(0, 300, 300, PdfRenderFlags.None);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("فشل في عرض معاينة PDF", ex);
            }
            return null;
        }
        public static string ConvertWordToPdf(string inputPath)
        {
            // التحقق من وجود ملف الإدخال
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"لم يتم العثور على ملف Word: {inputPath}");
            }

            // إنشاء مسار مؤقت لملف PDF الناتج
            string tempFilePath = GetTempFilePath(inputPath);

            Microsoft.Office.Interop.Word.Application wordApp = null;
            Document wordDoc = null;
            object missing = System.Reflection.Missing.Value; // تعريف missing هنا

            try
            {
                // إنشاء مثيل لتطبيق Word
                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = false; // تشغيل Word في الخلفية

                // فتح مستند Word
                object inputFile = inputPath;
                wordDoc = wordApp.Documents.Open(ref inputFile,
                                                 ref missing, ref missing, ref missing, ref missing, ref missing,
                                                 ref missing, ref missing, ref missing, ref missing, ref missing,
                                                 ref missing, ref missing, ref missing, ref missing, ref missing);

                // حفظ المستند كملف PDF
                object outputFilePath = tempFilePath;
                object fileFormat = WdSaveFormat.wdFormatPDF;
                wordDoc.SaveAs2(ref outputFilePath,
                                ref fileFormat, ref missing, ref missing, ref missing, ref missing,
                                ref missing, ref missing, ref missing, ref missing, ref missing,
                                ref missing, ref missing, ref missing, ref missing, ref missing,
                                ref missing);

                return tempFilePath;
            }
            catch (Exception ex)
            {
                // حذف الملف المؤقت إذا حدث خطأ
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                throw new Exception($"فشل تحويل Word إلى PDF: {ex.Message}", ex);
            }
            finally
            {
                // إغلاق المستند وتطبيق Word وتحرير كائنات COM
                if (wordDoc != null)
                {
                    object saveChanges = WdSaveOptions.wdDoNotSaveChanges;
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
                // تشغيل جامع القمامة لضمان تحرير الموارد
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        public static string ConvertPowerPointToPdf(string inputPath)
        {
            // التحقق من وجود ملف الإدخال
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"لم يتم العثور على ملف PowerPoint: {inputPath}");
            }

            // إنشاء مسار مؤقت لملف PDF الناتج
            string tempFilePath = GetTempFilePath(inputPath);

            Microsoft.Office.Interop.PowerPoint.Application pptApp = null;
            Presentation pptPres = null;
            object missing = System.Reflection.Missing.Value; // تعريف missing هنا

            try
            {
                // إنشاء مثيل لتطبيق PowerPoint
                pptApp = new Microsoft.Office.Interop.PowerPoint.Application();
                pptApp.Visible = MsoTriState.msoFalse; // تشغيل PowerPoint في الخلفية

                // فتح العرض التقديمي
                pptPres = pptApp.Presentations.Open(inputPath,
                                                    MsoTriState.msoFalse, // ReadOnly
                                                    MsoTriState.msoFalse, // Untitled
                                                    MsoTriState.msoFalse); // WithWindow

                // حفظ العرض التقديمي كملف PDF
                pptPres.SaveAs(tempFilePath, PpSaveAsFileType.ppSaveAsPDF);

                return tempFilePath;
            }
            catch (Exception ex)
            {
                // حذف الملف المؤقت إذا حدث خطأ
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                throw new Exception($"فشل تحويل PowerPoint إلى PDF: {ex.Message}", ex);
            }
            finally
            {
                // إغلاق العرض التقديمي وتطبيق PowerPoint وتحرير كائنات COM
                if (pptPres != null)
                {
                    pptPres.Close();
                    Marshal.ReleaseComObject(pptPres);
                    pptPres = null;
                }
                if (pptApp != null)
                {
                    pptApp.Quit();
                    Marshal.ReleaseComObject(pptApp);
                    pptApp = null;
                }
                // تشغيل جامع القمامة لضمان تحرير الموارد
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        private static string GetTempFilePath(string inputPath)
        {
            try
            {
                string fileName = $"{Path.GetFileNameWithoutExtension(inputPath)}_{Guid.NewGuid()}.pdf";
                return Path.Combine(TempFolder, fileName);
            }
            catch (Exception ex)
            {
                throw new Exception("فشل في إنشاء مسار ملف مؤقت", ex);
            }
        }
    }
}

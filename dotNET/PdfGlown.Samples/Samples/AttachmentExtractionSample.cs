using PdfGlown.Bytes;
using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to extract attachments from a PDF document.</summary>
    public class AttachmentExtractionSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                var catalog = document.Catalog;

                // 2. Extracting attachments...
                // 2.1. Embedded files (document level).
                foreach (KeyValuePair<PdfString, FileSpecification> entry in catalog.Names.EmbeddedFiles)
                { EvaluateDataFile(entry.Value); }

                // 2.2. File attachments (page level).
                foreach (var page in catalog.Pages)
                {
                    foreach (Annotation annotation in page.Annotations)
                    {
                        if (annotation is FileAttachment attachments)
                        { EvaluateDataFile(attachments.DataFile); }
                    }
                }
            }
        }

        private void EvaluateDataFile(IFileSpecification dataFile)
        {
            if (dataFile is FileSpecification)
            {
                var embeddedFile = ((FileSpecification)dataFile).EmbeddedFile;
                if (embeddedFile != null)
                { ExportAttachment(embeddedFile.Data, dataFile.FilePath); }
            }
        }

        private void ExportAttachment(IInputStream data, string filename)
        {
            string outputPath = GetOutputPath(filename);
            FileStream outputStream;
            try
            { outputStream = new FileStream(outputPath, FileMode.CreateNew); }
            catch (Exception e)
            { throw new Exception(outputPath + " file couldn't be created.", e); }

            try
            {
                var writer = new BinaryWriter(outputStream);
                writer.Write(data.ToArray());
                writer.Close();
                outputStream.Close();
            }
            catch (Exception e)
            { throw new Exception(outputPath + " file writing has failed.", e); }

            Console.WriteLine("Output: " + outputPath);
        }
    }
}
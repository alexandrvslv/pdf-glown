using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.Objects;

using System;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates the low-level way to extract text from a PDF document.</summary>
    /// <remarks>In order to obtain richer information about the extracted text content,
    /// see the other available samples (<see cref="TextInfoExtractionSample"/>,
    /// <see cref="AdvancedTextExtractionSample"/>).</remarks>
    public class BasicTextExtractionSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // 2. Text extraction from the document pages.
                foreach (var page in document.Pages)
                {
                    if (!PromptNextPage(page, false))
                    {
                        Quit();
                        break;
                    }

                    // Wraps the page contents into a scanner.
                    Extract(new ContentScanner(page));
                }
            }
        }

        /// <summary>Scans a content level looking for text.</summary>
        // NOTE: Page contents are represented by a sequence of content objects,
        // possibly nested into multiple levels.
        private void Extract(ContentScanner level)
        {
            if (level == null)
                return;
            level.OnObjectScanning += OnObjectScanning;
            level.Scan();
            level.OnObjectScanning -= OnObjectScanning;
            bool OnObjectScanning(ContentObject content, ICompositeObject container, int index)
            {
                if (content is ShowText showText)
                {
                    PdfFont font = level.State.Font;
                    // Extract the current text chunk, decoding it!
                    Console.WriteLine(font.Decode(showText.TextBytes));
                    return false;
                }
                return true;
            }
        }
    }
}
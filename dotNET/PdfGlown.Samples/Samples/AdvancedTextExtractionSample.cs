using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Tools;
using System;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to retrieve text content along with its graphic attributes
    /// (font, font size, text color, text rendering mode, text bounding box, and so on) from a PDF document;
    /// text is automatically sorted and aggregated.</summary>
    public class AdvancedTextExtractionSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                var catalog = document.Catalog;

                // 2. Text extraction from the document pages.
                var extractor = new TextExtractor();
                foreach (var page in catalog.Pages)
                {
                    if (!PromptNextPage(page, false))
                    {
                        Quit();
                        break;
                    }

                    var textStrings = extractor.Extract(page)[TextExtractor.DefaultArea];
                    foreach (ITextString textString in textStrings)
                    {
                        var textStringQuad = textString.Quad;
                        Console.WriteLine(
                          $"Text [x:{Math.Round(textStringQuad.MinX)},y:{Math.Round(textStringQuad.MinY)},w:{Math.Round(textStringQuad.Width)},h:{Math.Round(textStringQuad.Height)}]: {textString.Text}");
                    }
                }
            }
        }
    }
}
using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to define, read and modify page labels.</summary>
    public class PageLabelSample : Sample
    {
        public override void Run()
        {
            string outputFilePath;
            {
                // 1. Opening the PDF file...
                string filePath = PromptFileChoice("Please select a PDF file");
                using (var document = new PdfDocument(filePath))
                {

                    // 2. Defining the page labels...
                    PageLabels pageLabels = document.Catalog.PageLabels;
                    pageLabels.Clear();
                    /*
                      NOTE: This sample applies labels to arbitrary page ranges: no sensible connection with their
                      actual content has therefore to be expected.
                    */
                    int pageCount = document.Pages.Count;
                    pageLabels[PdfInteger.Get(0)] = new PageLabel(document, "Introduction ", PageLabel.NumberStyleEnum.UCaseRomanNumber, 5);
                    if (pageCount > 3)
                    { pageLabels[PdfInteger.Get(3)] = new PageLabel(document, PageLabel.NumberStyleEnum.UCaseLetter); }
                    if (pageCount > 6)
                    { pageLabels[PdfInteger.Get(6)] = new PageLabel(document, "Contents ", PageLabel.NumberStyleEnum.ArabicNumber, 0); }

                    // 3. Serialize the PDF file!
                    outputFilePath = Serialize(document, "Page labelling", "labelling a document's pages", "page labels");
                }
            }

            {
                using (var document = new PdfDocument(outputFilePath))
                {
                    foreach (KeyValuePair<PdfInteger, PageLabel> entry in document.Catalog.PageLabels)
                    {
                        Console.WriteLine("Page label " + entry.Value.RefOrSelf);
                        Console.WriteLine("    Initial page: " + (entry.Key.IntValue + 1));
                        Console.WriteLine("    Prefix: " + (entry.Value.Prefix));
                        Console.WriteLine("    Number style: " + (entry.Value.NumberStyle));
                        Console.WriteLine("    Number base: " + (entry.Value.NumberBase));
                    }
                }
            }
        }
    }
}
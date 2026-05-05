using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.XObjects;
using SkiaSharp;
using System;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to retrieve the precise position(page and coordinates)
    /// of each image within a PDF document, using the page content scanning functionality.</summary>
    /// <remarks>This sample leverages the ContentScanner class, a powerful device for accessing
    /// each single content object within a page.</remarks>
    public class ContentScanningSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // 2. Parsing the document...
                Console.WriteLine("\nLooking for images...");
                foreach (var page in document.Pages)
                {
                    Scan(new ContentScanner(page), // Wraps the page contents into the scanner.
                      page);
                }
            }
        }

        /// <summary>Scans a content level looking for images.</summary>
        // NOTE: Page contents are represented by a sequence of content objects,
        // possibly nested into multiple levels.
        private void Scan(ContentScanner scanner, PdfPage page)
        {
            if (scanner == null)
                return;

            scanner.OnObjectScanning += OnObjectScanning;
            scanner.Scan();
            scanner.OnObjectScanning -= OnObjectScanning;
            bool OnObjectScanning(ContentObject content, ICompositeObject container, int index)
            {
                var boxed = content as IBoxed;
                // NOTE: Images can be represented on a page either as
                // external objects (XObject) or inline objects.
                SKSize? imageSize = null; // Image native size.
                if (content is PaintXObject paintXObject)
                {
                    var xObject = paintXObject.GetResource(scanner);
                    // Is the external object an image?
                    if (xObject is ImageXObject)
                    {
                        // Image key and indirect reference.
                        Console.Write($"External Image '{paintXObject.Name}' ({xObject.RefOrSelf})");
                        imageSize = xObject.Size; // Image native size.
                    }
                    paintXObject.Scan(scanner.State);
                }
                else if (content is GraphicsInlineImage inlineImage)
                {
                    Console.Write("Inline Image");
                    imageSize = inlineImage.Size; // Image native size.
                    inlineImage.Scan(scanner.State);
                }

                if (imageSize.HasValue)
                {
                    SKRect box = boxed.GetBox(scanner.State); // Image position (location and size) on the page.
                    Console.WriteLine(
                      " on page " + page.Number + " (" + page.RefOrSelf + ")" // Page index and indirect reference.
                      );
                    Console.WriteLine("  Coordinates:");
                    Console.WriteLine("     x: " + Math.Round(box.Left));
                    Console.WriteLine("     y: " + Math.Round(box.Top));
                    Console.WriteLine("     width: " + Math.Round(box.Width) + " (native: " + Math.Round(imageSize.Value.Width) + ")");
                    Console.WriteLine("     height: " + Math.Round(box.Height) + " (native: " + Math.Round(imageSize.Value.Height) + ")");
                    return false;
                }
                return true;
            }
        }
    }
}
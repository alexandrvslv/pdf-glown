using PdfGlown.Documents;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Fonts;

using System;
using SkiaSharp;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample generates a series of PDF pages from the default page formats available,
    /// varying both in size and orientation.</summary>
    public class PageFormatSample : Sample
    {
        public override void Run()
        {
            // 1. PDF file instantiation.
            var document = new PdfDocument();

            // 2. Populate the document!
            Populate(document);

            // 3. Serialize the PDF file!
            Serialize(document, "Page Format", "page formats", "page formats");
        }

        private void Populate(PdfDocument document)
        {
            var bodyFont = PdfType1Font.Load(document, FontName.CourierBold);

            var pageFormats = Enum.GetValues<PageFormat.SizeEnum>();
            var pageOrientations = Enum.GetValues<PageFormat.OrientationEnum>();
            foreach (var pageFormat in pageFormats)
            {
                foreach (var pageOrientation in pageOrientations)
                {
                    // Add a page to the document!
                    var page = new PdfPage(document, PageFormat.GetSize(pageFormat, pageOrientation));
                    // Instantiates the page inside the document context.
                    document.Pages.Add(page); // Puts the page in the pages collection.

                    // Drawing the text label on the page...
                    SKSize pageSize = page.Size;
                    var composer = new PrimitiveComposer(page);
                    composer.SetFont(bodyFont, 32);
                    composer.ShowText(
                      pageFormat + " (" + pageOrientation + ")", // Text.
                      new SKPoint(pageSize.Width / 2, pageSize.Height / 2), // Location: page center.
                      XAlignmentEnum.Center, // Places the text on horizontal center of the location.
                      YAlignmentEnum.Middle, // Places the text on vertical middle of the location.
                      45); // Rotates the text 45 degrees counterclockwise.

                    composer.Flush();
                }
            }
        }
    }
}
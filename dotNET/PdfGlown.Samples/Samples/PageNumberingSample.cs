using PdfGlown.Documents;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Tools;
using SkiaSharp;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to stamp the page number on alternated corners
    /// of an existing PDF document's pages.</summary>
    /// <remarks>Stamping is just one of the several ways PDF contents can be manipulated using PDF Glown:
    /// contents can be inserted as (raw) data chunks, mid-level content objects, external forms, etc.</remarks>
    public class PageNumberingSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // 2. Stamp the document!
                Stamp(document);

                // 3. Serialize the PDF file!
                Serialize(document, "Page numbering", "numbering a document's pages", "page numbering");
            }
        }

        private void Stamp(PdfDocument document)
        {
            // 1. Instantiate the stamper!
            /* NOTE: The PageStamper is optimized for dealing with pages. */
            var stamper = new PageStamper();

            // 2. Numbering each page...
            var font = PdfType1Font.Load(document, FontName.CourierBold);
            var redColor = RGBColor.Get(SKColors.Red);
            int margin = 32;
            foreach (var page in document.Pages)
            {
                // 2.1. Associate the page to the stamper!
                stamper.Page = page;

                // 2.2. Stamping the page number on the foreground...
                {
                    PrimitiveComposer foreground = stamper.Foreground;

                    foreground.SetFont(font, 16);
                    foreground.SetFillColor(redColor);

                    SKSize pageSize = page.Size;
                    int pageNumber = page.Number;
                    bool pageIsEven = (pageNumber % 2 == 0);
                    foreground.ShowText(
                      pageNumber.ToString(),
                      new SKPoint(
                        (pageIsEven ? margin : pageSize.Width - margin),
                        pageSize.Height - margin),
                      (pageIsEven
                        ? XAlignmentEnum.Left
                        : XAlignmentEnum.Right),
                      YAlignmentEnum.Bottom,
                      0);
                }

                // 2.3. End the stamping!
                stamper.Flush();
            }
        }
    }
}
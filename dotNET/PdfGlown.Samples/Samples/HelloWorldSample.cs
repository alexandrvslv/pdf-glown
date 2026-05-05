using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Fonts;

using SkiaSharp;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample is a minimalist introduction to the use of PDF Glown.</summary>
    public class HelloWorldSample : Sample
    {
        public override void Run()
        {
            // 1. Instantiate a new PDF file!
            /* NOTE: a File object is the low-level (syntactic) representation of a PDF file. */
            var document = new PdfDocument();

            // 3. Insert the contents into the document!
            Populate(document);

            // 4. Serialize the PDF file!
            Serialize(document, "Hello world", "a simple 'hello world'", "Hello world");
        }

        /// <summary>Populates a PDF file with contents.</summary>
        private void Populate(PdfDocument document)
        {
            // 1. Add the page to the document!
            var page = new PdfPage(document); // Instantiates the page inside the document context.
            document.Pages.Add(page); // Puts the page in the pages collection.
            SKSize pageSize = page.Size;

            // 2. Create a content composer for the page!
            var composer = new PrimitiveComposer(page);

            // 3. Inserting contents...
            // Set the font to use!
            composer.SetFont(PdfType1Font.Load(document, FontName.CourierBold), 30);
            // Show the text onto the page (along with its box)!
            /*
              NOTE: PrimitiveComposer's ShowText() method is the most basic way to add text to a page
              -- see BlockComposer for more advanced uses (horizontal and vertical alignment, hyphenation,
              etc.).
            */
            composer.ShowText("Hello World!", new SKPoint(32, 48));

            composer.SetLineWidth(.25);
            composer.SetLineCap(LineCapEnum.Round);
            composer.SetLineDash(new LineDash(new float[] { 5, 10 }));
            composer.SetTextLead(1.2);
            composer.DrawPolygon(
              composer.ShowText(
                "This is a primitive example"
                  + "\nof centered, rotated multi-"
                  + "\nline text."
                  + "\n\nWe recommend you to use"
                  + "\nBlockComposer instead, as it"
                  + "\nautomatically manages text"
                  + "\nwrapping and alignment with-"
                  + "\nin a specified area!",
                new SKPoint(pageSize.Width / 2, pageSize.Height / 2),
                XAlignmentEnum.Center,
                YAlignmentEnum.Middle,
                15
                ).GetPoints()
              );
            composer.Stroke();

            // 4. Flush the contents into the page!
            composer.Flush();
        }
    }
}
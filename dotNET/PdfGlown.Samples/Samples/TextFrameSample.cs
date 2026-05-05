using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Fonts;

using SkiaSharp;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to obtain the actual area occupied by text shown in a PDF page.</summary>
    public class TextFrameSample : Sample
    {
        public override void Run()
        {
            // 1. Instantiate a new PDF file!
            var document = new PdfDocument();

            // 2. Insert the contents into the document!
            Populate(document);

            // 3. Serialize the PDF file!
            Serialize(document, "Text frame", "getting the actual bounding box of text shown", "text frames");
        }

        /// <summary>Populates a PDF file with contents.</summary>
        private void Populate(PdfDocument document)
        {
            // 1. Add the page to the document!
            var page = new PdfPage(document); // Instantiates the page inside the document context.
            document.Pages.Add(page); // Puts the page in the pages collection.

            // 2. Create a content composer for the page!
            var composer = new PrimitiveComposer(page);

            var textColor = new RGBColor(115 / 255d, 164 / 255d, 232 / 255d);
            composer.SetFillColor(textColor);
            composer.SetLineDash(new LineDash(new float[] { 10 }));
            composer.SetLineWidth(.25);

            var blockComposer = new BlockComposer(composer);
            blockComposer.Begin(SKRect.Create(300, 400, 200, 100), XAlignmentEnum.Left, YAlignmentEnum.Middle);
            composer.SetFont(PdfType1Font.Load(document, FontName.TimesItalic), 12);
            blockComposer.ShowText("PrimitiveComposer.ShowText(...) methods return the actual bounding box of the text shown, allowing to precisely determine its location on the page.");
            blockComposer.End();

            // 3. Inserting contents...
            // Set the font to use!
            composer.SetFont(PdfType1Font.Load(document, FontName.CourierBold), 72);
            composer.DrawPolygon(
              composer.ShowText(
                "Text frame",
                new SKPoint(150, 360),
                XAlignmentEnum.Left,
                YAlignmentEnum.Middle,
                45).GetPoints());
            composer.Stroke();

            composer.SetFont(PdfType0Font.Load(document, GetResourcePath("fonts" + System.IO.Path.DirectorySeparatorChar + "Ruritania-Outline.ttf")), 102);
            composer.DrawPolygon(
              composer.ShowText(
                "Text frame",
                new SKPoint(250, 600),
                XAlignmentEnum.Center,
                YAlignmentEnum.Middle,
                -25).GetPoints());
            composer.Stroke();

            // 4. Flush the contents into the page!
            composer.Flush();
        }
    }
}
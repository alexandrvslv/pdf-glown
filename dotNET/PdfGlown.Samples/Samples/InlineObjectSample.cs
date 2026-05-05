using PdfGlown.Documents;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Entities;
using PdfGlown.Documents.Contents.Fonts;
using SkiaSharp;
using System.IO;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to embed an image object within a PDF content
    /// stream.</summary>
    /// <remarks>
    ///   <para>Inline objects should be used sparingly, as they easily clutter content
    ///   streams.</para>
    ///   <para>The alternative (and preferred) way to insert an image object is via external
    ///   object (XObject); its main advantage is to allow content reuse.</para>
    /// </remarks>
    public class InlineObjectSample : Sample
    {
        private const float Margin = 36;

        public override void Run()
        {
            // 1. PDF file instantiation.
            var document = new PdfDocument();

            // 2. Content creation.
            Populate(document);

            // 3. Serialize the PDF file!
            Serialize(document, "Inline image", "embedding an image within a content stream", "inline image");
        }

        private void Populate(PdfDocument document)
        {
            var page = new PdfPage(document);
            document.Pages.Add(page);
            SKSize pageSize = page.Size;

            var composer = new PrimitiveComposer(page);
            {
                var blockComposer = new BlockComposer(composer);
                blockComposer.Hyphenation = true;
                blockComposer.Begin(
                  SKRect.Create(
                    Margin,
                    Margin,
                    (float)pageSize.Width - Margin * 2,
                    (float)pageSize.Height - Margin * 2),
                  XAlignmentEnum.Justify,
                  YAlignmentEnum.Top);
                var bodyFont = PdfType1Font.Load(document, FontName.CourierBold);
                composer.SetFont(bodyFont, 32);
                blockComposer.ShowText("Inline image sample"); blockComposer.ShowBreak();
                composer.SetFont(bodyFont, 16);
                blockComposer.ShowText("Showing the GNU logo as an inline image within the page content stream.");
                blockComposer.End();
            }
            // Showing the 'GNU' image...
            {
                // Instantiate a jpeg image object!
                var image = Image.Get(GetResourcePath("images" + Path.DirectorySeparatorChar + "gnu.jpg")); // Abstract image (entity).
                                                                                                                      // Set the position of the image in the page!
                composer.ApplyMatrix(200, 0, 0, 200, (pageSize.Width - 200) / 2, (pageSize.Height - 200) / 2);
                // Show the image!
                image.ToInlineObject(composer); // Transforms the image entity into an inline image within the page.
            }
            composer.Flush();
        }
    }
}
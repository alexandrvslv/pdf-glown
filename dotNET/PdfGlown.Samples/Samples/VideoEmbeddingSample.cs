using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Annotations;
using SkiaSharp;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to insert screen annotations to display media clips inside
    ///  a PDF document.</summary>
    public class VideoEmbeddingSample : Sample
    {
        public override void Run()
        {
            // 1. Instantiate the PDF file!
            var document = new PdfDocument();

            // 2. Insert a new page!
            var page = new PdfPage(document);
            document.Pages.Add(page);

            // 3. Insert a video into the page!
            new Screen(
              page,
              SKRect.Create(10, 10, 320, 180),
              "PJ Harvey - Dress (part)",
              GetResourcePath("video" + System.IO.Path.DirectorySeparatorChar + "pj_clip.mp4"),
              "video/mp4");

            // 4. Serialize the PDF file!
            Serialize(document, "Video embedding", "inserting screen annotations to display media clips inside a PDF document", "video embedding");
        }
    }
}
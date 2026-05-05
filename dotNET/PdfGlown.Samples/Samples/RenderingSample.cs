using PdfGlown.Tools;
using SkiaSharp;

namespace PdfGlown.Samples.CLI
{
    /**
      <summary>This sample demonstrates how to render a PDF page as a raster image.<summary>
      <remarks>Note: rendering is currently in pre-alpha stage; therefore this sample is
      nothing but an initial stub (no assumption to work!).</remarks>
    */
    public class RenderingSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                var pages = document.Pages;

                // 2. Page rasterization.
                int pageIndex = PromptPageChoice("Select the page to render", pages.Count);
                var page = pages[pageIndex];
                var imageSize = new SKSize(page.RotatedBox.Width * 2.5F, page.RotatedBox.Height * 2.5F);
                var renderer = new Renderer();
                var image = renderer.Render(page, imageSize);

                // 3. Save the page image!

                using (var stream = new SKFileWStream(GetOutputPath("ContentRenderingSample.png")))
                {
                    image.Encode(stream, SKEncodedImageFormat.Png, 100);
                };
            }
        }
    }
}
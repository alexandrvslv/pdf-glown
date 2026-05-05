using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Navigation;
using SkiaSharp;
using System;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to apply links to a PDF document.</summary>
    public class LinkCreationSample : Sample
    {
        public override void Run()
        {
            // 1. Creating the document...
            var document = new PdfDocument();

            // 2. Applying links...
            BuildLinks(document);

            // 3. Serialize the PDF file!
            Serialize(document, "Link annotations", "applying link annotations", "links, creation");
        }

        private void BuildLinks(PdfDocument document)
        {
            var pages = document.Pages;
            var page = new PdfPage(document);
            pages.Add(page);

            var composer = new PrimitiveComposer(page);
            var blockComposer = new BlockComposer(composer);

            var font = PdfType1Font.Load(document, FontName.CourierBold);

            // 2.1. Goto-URI link.
            {
                blockComposer.Begin(SKRect.Create(30, 100, 200, 50), XAlignmentEnum.Left, YAlignmentEnum.Middle);
                composer.SetFont(font, 12);
                blockComposer.ShowText("Go-to-URI link");
                composer.SetFont(font, 8);
                blockComposer.ShowText("\nIt allows you to navigate to a network resource.");
                composer.SetFont(font, 5);
                blockComposer.ShowText("\n\nClick on the box to go to the project's SourceForge.net repository.");
                blockComposer.End();

                // NOTE: This statement instructs the PDF viewer to navigate to the given URI when the link is clicked.
                page.Annotations.Add(new Link(page, SKRect.Create(240, 100, 100, 50), "Link annotation",
                  new GoToURI(document, new Uri("http://www.sourceforge.net/projects/clown")))
                { Border = new Border(3, BorderStyleType.Beveled) });
            }

            // 2.2. Embedded-goto link.
            {
                var filePath = PromptFileChoice("Please select a PDF file to attach");

                // NOTE: These statements instruct PDF Glown to attach a PDF file to the current document.
                // This is necessary in order to test the embedded-goto functionality,
                // as you can see in the following link creation (see below).
                int fileAttachmentPageIndex = page.Index;
                var fileAttachmentName = "attachedSamplePDF";
                var fileName = System.IO.Path.GetFileName(filePath);
                page.Annotations.Add(new FileAttachment(page, SKRect.Create(0, -20, 10, 10), "File attachment annotation",
                    IFileSpecification.Get(EmbeddedFile.Get(document, filePath), fileName))
                {
                    Name = fileAttachmentName,
                    AttachmentName = FileAttachmentImageType.PaperClip
                });

                blockComposer.Begin(SKRect.Create(30, 170, 200, 50), XAlignmentEnum.Left, YAlignmentEnum.Middle);
                composer.SetFont(font, 12);
                blockComposer.ShowText("Go-to-embedded link");
                composer.SetFont(font, 8);
                blockComposer.ShowText("\nIt allows you to navigate to a destination within an embedded PDF file.");
                composer.SetFont(font, 5);
                blockComposer.ShowText("\n\nClick on the button to go to the 2nd page of the attached PDF file (" + fileName + ").");
                blockComposer.End();

                // NOTE: This statement instructs the PDF viewer to navigate to the page 2 of a PDF file
                // attached inside the current document as described by the FileAttachment annotation on page 1 of the current document.
                page.Annotations.Add(new Link(page, SKRect.Create(240, 170, 100, 50), "Link annotation",
                  new GoToEmbedded(
                    document,
                    new GoToEmbedded.PathElement(
                      document,
                      // Page of the current document containing the file attachment annotation of the target document.
                      fileAttachmentPageIndex,
                      // Name of the file attachment annotation corresponding to the target document.
                      fileAttachmentName,
                      // No sub-target.
                      null), // Target represents the document to go to.
                    new RemoteDestination(
                      document,
                      1, // Show the page 2 of the target document.
                      Destination.ModeEnum.Fit, // Show the target document page entirely on the screen.
                      null,
                      null
                      )))// The destination must be within the target document.
                { Border = new Border(1, new LineDash(new float[] { 8, 5, 2, 5 })) });
            }

            // 2.3. Textual link.
            {
                blockComposer.Begin(SKRect.Create(30, 240, 200, 50), XAlignmentEnum.Left, YAlignmentEnum.Middle);
                composer.SetFont(font, 12);
                blockComposer.ShowText("Textual link");
                composer.SetFont(font, 8);
                blockComposer.ShowText("\nIt allows you to expose any kind of link (including the above-mentioned types) as text.");
                composer.SetFont(font, 5);
                blockComposer.ShowText("\n\nClick on the text links to go either to the project's SourceForge.net repository or to the project's home page.");
                blockComposer.End();

                composer.BeginLocalState();
                composer.SetFont(font, 10);
                composer.SetFillColor(RGBColor.Get(SKColors.Blue));
                composer.ShowText(
                  "PDF Glown Project's repository at SourceForge.net",
                  new SKPoint(240, 265),
                  XAlignmentEnum.Left,
                  YAlignmentEnum.Middle,
                  0,
                  new GoToURI(
                    document,
                    new Uri("http://www.sourceforge.net/projects/clown")));
                composer.ShowText(
                  "PDF Glown Project's home page",
                  new SKPoint(240, 285),
                  XAlignmentEnum.Left,
                  YAlignmentEnum.Bottom,
                  -90,
                  new GoToURI(
                    document,
                    new Uri("http://www.pdfclown.org")));
                composer.End();
            }

            composer.Flush();
        }
    }
}
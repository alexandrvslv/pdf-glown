using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Annotations.styles;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to insert annotations into a PDF document.</summary>
    public class AnnotationSample : Sample
    {
        public override void Run()
        {
            // 1. PDF file instantiation.
            var document = new PdfDocument();

            // 2. Content creation.
            Populate(document);

            // 3. Serialize the PDF file!
            Serialize(document, "Annotations", "inserting annotations", "annotations, creation, attachment, sticky notes, callout notes, rubber stamps, markup, highlighting");
        }

        private void Populate(PdfDocument document)
        {
            var page = new PdfPage(document);
            document.Pages.Add(page);

            var composer = new PrimitiveComposer(page);
            var font = PdfType1Font.Load(document, FontName.CourierBold);
            composer.SetFont(font, 12);

            // Sticky note.
            composer.ShowText("Sticky note annotation:", new SKPoint(35, 35));
            page.Annotations.Add(new StickyNote(page, new SKPoint(50, 50), "Text of the Sticky note annotation")
            {
                ImageName = NoteImageEnum.Note,
                Color = RGBColor.Get(SKColors.Yellow),
                Popup = new Popup(
                    page,
                    SKRect.Create(200, 25, 200, 75),
                    "Text of the Popup annotation (this text won't be visible as associating popups to markup annotations overrides the former's properties with the latter's)"
                ),
                Author = "Stefano",
                Subject = "Sticky note",
                IsOpen = true
            });
            page.Annotations.Add(new StickyNote(page, new SKPoint(80, 50), "Text of the Help sticky note annotation")
            {
                ImageName = NoteImageEnum.Help,
                Color = RGBColor.Get(SKColors.Pink),
                Author = "Stefano",
                Subject = "Sticky note",
                Popup = new Popup(
                    page,
                    SKRect.Create(400, 25, 200, 75),
                    "Text of the Popup annotation (this text won't be visible as associating popups to markup annotations overrides the former's properties with the latter's)"
                )
            });
            page.Annotations.Add(new StickyNote(page, new SKPoint(110, 50), "Text of the Comment sticky note annotation")
            {
                ImageName = NoteImageEnum.Comment,
                Color = RGBColor.Get(SKColors.Green),
                Author = "Stefano",
                Subject = "Sticky note"
            });
            page.Annotations.Add(new StickyNote(page, new SKPoint(140, 50), "Text of the Key sticky note annotation")
            {
                ImageName = NoteImageEnum.Key,
                Color = RGBColor.Get(SKColors.Blue),
                Author = "Stefano",
                Subject = "Sticky note"
            });

            // Callout.
            composer.ShowText("Callout note annotation:", new SKPoint(35, 85));
            page.Annotations.Add(new FreeText(page, SKRect.Create(250, 90, 150, 70), "Text of the Callout note annotation")
            {
                Line = new FreeText.CalloutLine(page,
                    new SKPoint(100, 100),
                    new SKPoint(150, 125),
                    new SKPoint(250, 125)),
                Intent = MarkupIntent.FreeTextCallout,
                LineEndStyle = LineEndStyleEnum.OpenArrow,
                Border = new Border(1),
                Color = RGBColor.Get(SKColors.Yellow)
            });

            // File attachment.
            composer.ShowText("File attachment annotation:", new SKPoint(35, 135));
            page.Annotations.Add(new FileAttachment(page,
              SKRect.Create(50, 150, 15, 20),
              "Text of the File attachment annotation",
              IFileSpecification.Get(
                EmbeddedFile.Get(document, GetResourcePath("images" + Path.DirectorySeparatorChar + "gnu.jpg")),
                "happyGNU.jpg"))
            {
                AttachmentName = FileAttachmentImageType.PaperClip,
                Author = "Stefano",
                Subject = "File attachment"
            });

            composer.ShowText("Line annotation:", new SKPoint(35, 185));
            {
                composer.BeginLocalState();
                composer.SetFont(font, 10);

                // Arrow line.
                composer.ShowText("Arrow:", new SKPoint(50, 200));
                page.Annotations.Add(new Line(
                    page,
                    new SKPoint(50, 260),
                    new SKPoint(200, 210),
                    "Text of the Arrow line annotation",
                    RGBColor.Get(SKColors.Black))
                {
                    StartStyle = LineEndStyleEnum.Circle,
                    EndStyle = LineEndStyleEnum.ClosedArrow,
                    CaptionVisible = true,
                    InteriorColor = RGBColor.Get(SKColors.Green),
                    Author = "Stefano",
                    Subject = "Arrow line"
                });

                // Dimension line.
                composer.ShowText("Dimension:", new SKPoint(300, 200));
                page.Annotations.Add(new Line(
                  page,
                  new SKPoint(300, 220),
                  new SKPoint(500, 220),
                  "Text of the Dimension line annotation",
                  RGBColor.Get(SKColors.Blue))
                {
                    LeaderLineLength = -20,
                    LeaderLineExtension = 10,
                    StartStyle = LineEndStyleEnum.OpenArrow,
                    EndStyle = LineEndStyleEnum.OpenArrow,
                    Border = new Border(1),
                    CaptionVisible = true,
                    Author = "Stefano",
                    Subject = "Dimension line"
                });

                composer.End();
            }

            var path = new SKPath();
            path.MoveTo(new SKPoint(50, 320));
            path.LineTo(new SKPoint(70, 305));
            path.LineTo(new SKPoint(110, 335));
            path.LineTo(new SKPoint(130, 320));
            path.LineTo(new SKPoint(110, 305));
            path.LineTo(new SKPoint(70, 335));
            path.LineTo(new SKPoint(50, 320));
            // Scribble.
            composer.ShowText("Scribble annotation:", new SKPoint(35, 285));
            page.Annotations.Add(new Scribble(
              page,
              new List<SKPath> { path },
              "Text of the Scribble annotation",
              RGBColor.Get(SKColors.Orange))
            {
                Border = new Border(1, new LineDash(new float[] { 5, 2, 2, 2 })),
                Author = "Stefano",
                Subject = "Scribble"
            });

            // Rectangle.
            composer.ShowText("Rectangle annotation:", new SKPoint(35, 350));
            page.Annotations.Add(new Rectangle(
              page,
              SKRect.Create(50, 370, 100, 30),
              "Text of the Rectangle annotation")
            {
                Color = RGBColor.Get(SKColors.Red),
                Border = new Border(1, new LineDash(new float[] { 5 })),
                Author = "Stefano",
                Subject = "Rectangle",
                Popup = new Popup(
                 page,
                 SKRect.Create(200, 325, 200, 75),
                 "Text of the Popup annotation (this text won't be visible as associating popups to markup annotations overrides the former's properties with the latter's)")
            });

            // Ellipse.
            composer.ShowText("Ellipse annotation:", new SKPoint(35, 415));
            page.Annotations.Add(new Ellipse(
              page,
              SKRect.Create(50, 440, 100, 30),
              "Text of the Ellipse annotation")
            {
                BorderEffect = new BorderEffect(BorderEffectType.Cloudy, 1),
                InteriorColor = RGBColor.Get(SKColors.Cyan),
                Color = RGBColor.Get(SKColors.Black),
                Author = "Stefano",
                Subject = "Ellipse"
            });

            // Rubber stamp.
            composer.ShowText("Rubber stamp annotations:", new SKPoint(35, 505));
            {
                var stampFont = PdfType0Font.Load(document, GetResourcePath("fonts" + Path.DirectorySeparatorChar + "TravelingTypewriter.otf"));
                page.Annotations.Add(new Stamp(
                  page,
                  new SKPoint(75, 570),
                  "This is a round custom stamp",
                  new StampAppearanceBuilder(document, StampAppearanceBuilder.TypeEnum.Round, "Done", 50, stampFont).Build())
                {
                    Rotation = -10,
                    Author = "Stefano",
                    Subject = "Custom stamp"
                });

                page.Annotations.Add(new Stamp(
                  page,
                  new SKPoint(210, 570),
                  "This is a squared (and round-cornered) custom stamp",
                  new StampAppearanceBuilder(document, StampAppearanceBuilder.TypeEnum.Squared, "Classified", 150, stampFont)
                  { Color = RGBColor.Get(SKColors.Orange) }.Build())
                {
                    Rotation = 15,
                    Author = "Stefano",
                    Subject = "Custom stamp"
                });

                var stampFont2 = PdfType0Font.Load(document, GetResourcePath("fonts" + Path.DirectorySeparatorChar + "MgOpenCanonicaRegular.ttf"));
                page.Annotations.Add(new Stamp(
                  page,
                  new SKPoint(350, 570),
                  "This is a striped custom stamp",
                  new StampAppearanceBuilder(document, StampAppearanceBuilder.TypeEnum.Striped, "Out of stock", 100, stampFont2)
                  { Color = RGBColor.Get(SKColors.Gray) }.Build())
                {
                    Rotation = 90,
                    Author = "Stefano",
                    Subject = "Custom stamp"
                });

                // Define the standard stamps template path!
                /*
                  NOTE: The PDF specification defines several stamps (aka "standard stamps") whose rendering
                  depends on the support of viewer applications. As such support isn't guaranteed, PDF Glown
                  offers smooth, ready-to-use embedding of these stamps through the StampPath property of the
                  document configuration: you can decide to point to the stamps directory of your Acrobat
                  installation (e.g., in my GNU/Linux system it's located in
                  "/opt/Adobe/Reader9/Reader/intellinux/plug_ins/Annotations/Stamps/ENU") or to the
                  collection included in this distribution (std-stamps.pdf).
                */
                document.CatalogConfiguration.StampPath = GetResourcePath("../../pkg/templates/std-stamps.pdf");

                // Add a standard stamp, rotating it 15 degrees counterclockwise!
                page.Annotations.Add(new Stamp(
                  page,
                  new SKPoint(485, 515),
                  null, // Default size is natural size.
                  "This is 'Confidential', a standard stamp",
                  StandardStampEnum.Confidential)
                {
                    Rotation = 15,
                    Author = "Stefano",
                    Subject = "Standard stamp"
                });

                // Add a standard stamp, without rotation!
                page.Annotations.Add(new Stamp(
                  page,
                  new SKPoint(485, 580),
                  null, // Default size is natural size.
                  "This is 'SBApproved', a standard stamp",
                  StandardStampEnum.BusinessApproved)
                {
                    Author = "Stefano",
                    Subject = "Standard stamp"
                });

                // Add a standard stamp, rotating it 10 degrees clockwise!
                page.Annotations.Add(new Stamp(
                  page,
                  new SKPoint(485, 635),
                  new SKSize(0, 40), // This scales the width proportionally to the 40-unit height (you can obviously do also the opposite, defining only the width).
                  "This is 'SHSignHere', a standard stamp",
                  StandardStampEnum.SignHere)
                {
                    Rotation = -10,
                    Author = "Stefano",
                    Subject = "Standard stamp"
                });
            }

            composer.ShowText("Text markup annotations:", new SKPoint(35, 650));
            {
                composer.BeginLocalState();
                composer.SetFont(font, 8);
                page.Annotations.Add(new TextMarkup(page,
                  composer.ShowText("Highlight annotation", new SKPoint(35, 680)),
                  "Text of the Highlight annotation",
                  TextMarkupType.Highlight)
                {
                    Author = "Stefano",
                    Subject = "An highlight text markup!"
                });
                page.Annotations.Add(new TextMarkup(page,
                  composer.ShowText("Highlight annotation 2", new SKPoint(35, 695)).Inflate(0, 1),
                  "Text of the Highlight annotation 2",
                  TextMarkupType.Highlight)
                {
                    Color = RGBColor.Get(SKColors.Magenta)
                });

                page.Annotations.Add(new TextMarkup(page,
                  composer.ShowText("Highlight annotation 3", new SKPoint(35, 710)).Inflate(0, 2),
                  "Text of the Highlight annotation 3",
                  TextMarkupType.Highlight)
                {
                    Color = RGBColor.Get(SKColors.Red)
                });

                page.Annotations.Add(new TextMarkup(page,
                  composer.ShowText("Squiggly annotation", new SKPoint(180, 680)),
                  "Text of the Squiggly annotation",
                  TextMarkupType.Squiggly));

                page.Annotations.Add(new TextMarkup(page,
                  composer.ShowText("Squiggly annotation 2", new SKPoint(180, 695)).Inflate(0, 2.5f),
                  "Text of the Squiggly annotation 2",
                  TextMarkupType.Squiggly)
                {
                    Color = RGBColor.Get(SKColors.Orange)
                });

                page.Annotations.Add(new TextMarkup(page,
                  composer.ShowText("Squiggly annotation 3", new SKPoint(180, 710)).Inflate(0, 3),
                  "Text of the Squiggly annotation 3",
                  TextMarkupType.Squiggly)
                {
                    Color = RGBColor.Get(SKColors.Pink)
                });

                page.Annotations.Add(new TextMarkup(page,
                    composer.ShowText("Underline annotation", new SKPoint(320, 680)),
                    "Text of the Underline annotation",
                    TextMarkupType.Underline));
                page.Annotations.Add(new TextMarkup(page,
                  composer.ShowText("Underline annotation 2", new SKPoint(320, 695)).Inflate(0, 2.5f),
                  "Text of the Underline annotation 2",
                  TextMarkupType.Underline)
                {
                    Color = RGBColor.Get(SKColors.Orange)
                });

                page.Annotations.Add(new TextMarkup(page,
                  composer.ShowText("Underline annotation 3", new SKPoint(320, 710)).Inflate(0, 3),
                  "Text of the Underline annotation 3",
                  TextMarkupType.Underline)
                {
                    Color = RGBColor.Get(SKColors.Green)
                });

                page.Annotations.Add(new TextMarkup(page,
                    composer.ShowText("StrikeOut annotation", new SKPoint(455, 680)),
                    "Text of the StrikeOut annotation",
                    TextMarkupType.StrikeOut));

                page.Annotations.Add(new TextMarkup(page,
                    composer.ShowText("StrikeOut annotation 2", new SKPoint(455, 695)).Inflate(0, 2.5f),
                    "Text of the StrikeOut annotation 2",
                    TextMarkupType.StrikeOut)
                {
                    Color = RGBColor.Get(SKColors.Orange)
                });

                page.Annotations.Add(new TextMarkup(page,
                    composer.ShowText("StrikeOut annotation 3", new SKPoint(455, 710)).Inflate(0, 3),
                    "Text of the StrikeOut annotation 3",
                    TextMarkupType.StrikeOut)
                {
                    Color = RGBColor.Get(SKColors.Green)
                });

                composer.End();
            }
            composer.Flush();
        }
    }
}
using PdfGlown.Documents;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Objects;
using PdfGlown.Tools;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to inspect the links of a PDF document, retrieving
    /// their associated text along with its graphic attributes (font, font size, text color,
    /// text rendering mode, text bounding box...).</summary>
    /// <remarks>According to PDF spec, page text and links have no mutual relation (contrary to, for
    /// example, HTML links), so retrieving the text associated to a link is somewhat tricky
    /// as we have to infer the overlapping areas between links and their corresponding text.</remarks>
    public class LinkParsingSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // 2. Link extraction from the document pages.
                var extractor = new TextExtractor();
                extractor.AreaTolerance = 2; // 2 pt tolerance on area boundary detection.
                bool linkFound = false;
                foreach (var page in document.Pages)
                {
                    if (!PromptNextPage(page, !linkFound))
                    {
                        Quit();
                        break;
                    }

                    IDictionary<SKRect?, IList<ITextString>> textStrings = null;
                    linkFound = false;

                    // Get the page annotations!
                    PageAnnotations annotations = page.Annotations;
                    if (annotations.Virtual)
                    {
                        Console.WriteLine("No annotations here.");
                        continue;
                    }

                    // Iterating through the page annotations looking for links...
                    foreach (Annotation annotation in annotations)
                    {
                        if (annotation is Link link)
                        {
                            linkFound = true;

                            if (textStrings == null)
                            { textStrings = extractor.Extract(page); }

                            SKRect linkBox = link.GetViewBounds();

                            // Text.
                            /*
                              Extracting text superimposed by the link...
                              NOTE: As links have no strong relation to page text but a weak location correspondence,
                              we have to filter extracted text by link area.
                            */
                            var linkTextBuilder = new StringBuilder();
                            foreach (ITextString linkTextString in extractor.Filter(textStrings, linkBox))
                            {
                                linkTextBuilder.Append(linkTextString.Text);
                            }
                            Console.WriteLine("Link '" + linkTextBuilder + "' ");

                            // Position.
                            Console.WriteLine(
                              "    Position: "
                                + "x:" + Math.Round(linkBox.Left) + ","
                                + "y:" + Math.Round(linkBox.Top) + ","
                                + "w:" + Math.Round(linkBox.Width) + ","
                                + "h:" + Math.Round(linkBox.Height));

                            // Target.
                            Console.Write("    Target: ");
                            var target = link.Target;
                            if (target is Destination destination)
                            {
                                PrintDestination(destination);
                            }
                            else if (target is PdfAction action)
                            {
                                PrintAction(action);
                            }
                            else if (target == null)
                            {
                                Console.WriteLine("[not available]");
                            }
                            else
                            {
                                Console.WriteLine("[unknown type: " + target.GetType().Name + "]");
                            }
                        }
                    }
                    if (!linkFound)
                    {
                        Console.WriteLine("No links here.");
                        continue;
                    }
                }
            }
        }

        private void PrintAction(PdfAction action)
        {
            /*
              NOTE: Here we have to deal with reflection as a workaround
              to the lack of type covariance support in C# (so bad -- any better solution?).
            */
            Console.WriteLine("Action [" + action.GetType().Name + "] " + action.RefOrSelf);
            if (action.Is(typeof(GoToDestination<>)))
            {
                if (action.Is(typeof(GotoNonLocal<>)))
                {
                    var destinationFile = (FileSpecification)action.Get("DestinationFile");
                    if (destinationFile != null)
                    { Console.WriteLine("      Filename: " + destinationFile.FilePath); }

                    if (action is GoToEmbedded)
                    {
                        GoToEmbedded.PathElement target = ((GoToEmbedded)action).DestinationPath;
                        Console.WriteLine("      EmbeddedFilename: " + target.EmbeddedFileName + " Relation: " + target.Relation);
                    }
                }
                Console.Write("      ");
                PrintDestination((Destination)action.Get("Destination"));
            }
            else if (action is GoToURI)
            { Console.WriteLine("      URI: " + ((GoToURI)action).URI); }
        }

        private void PrintDestination(Destination destination)
        {
            Console.WriteLine(destination.GetType().Name + " " + destination.RefOrSelf);
            Console.Write("        Page ");
            object pageRef = destination.Page;
            if (pageRef is PdfPage page)
            {
                Console.WriteLine(page.Number + " [ID: " + page.RefOrSelf + "]");
            }
            else
            { Console.WriteLine((int)pageRef + 1); }

            object location = destination.Location;
            if (location != null)
            { Console.WriteLine("        Location {0}", location); }

            double? zoom = destination.Zoom;
            if (zoom.HasValue)
            { Console.WriteLine("        Zoom {0}", zoom.Value); }
        }
    }
}
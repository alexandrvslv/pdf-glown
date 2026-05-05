using PdfGlown.Documents;
using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Objects;
using System;
using System.Collections.Generic;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to inspect the bookmarks of a PDF document.</summary>
    public class BookmarksParsingSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                PdfCatalog catalog = document.Catalog;
                // 2. Get the bookmarks collection!
                Bookmarks bookmarks = catalog.Bookmarks;
                if ((bookmarks.Status & PdfObjectStatus.Virtual) == PdfObjectStatus.Virtual)
                {
                    Console.WriteLine("\nNo bookmark available (Outline dictionary not found).");
                }
                else
                {
                    Console.WriteLine("\nIterating through the bookmarks collection (please wait)...\n");
                    // 3. Show the bookmarks!
                    PrintBookmarks(bookmarks);
                }
            }
        }

        private void PrintBookmarks(IEnumerable<Bookmark> bookmarks)
        {
            if (bookmarks == null)
                return;

            foreach (Bookmark bookmark in bookmarks)
            {
                // Show current bookmark!
                Console.WriteLine("Bookmark '" + bookmark.Title + "'");
                Console.Write("    Target: ");
                var target = bookmark.Target;
                if (target is Destination)
                { PrintDestination((Destination)target); }
                else if (target is PdfAction)
                { PrintAction((PdfAction)target); }
                else if (target == null)
                { Console.WriteLine("[not available]"); }
                else
                { Console.WriteLine("[unknown type: " + target.GetType().Name + "]"); }

                // Show child bookmarks!
                PrintBookmarks(bookmark);
            }
        }

        private void PrintAction(PdfAction action)
        {
            /*
              NOTE: Here we have to deal with reflection as a workaround
              to the lack of type covariance support in C# (so bad -- any better solution?).
            */
            Console.WriteLine("Action [" + action.GetType().Name + "] " + action.Reference);
            if (action.Is(typeof(GoToDestination<>)))
            {
                if (action.Is(typeof(GotoNonLocal<>)))
                {
                    FileSpecification destinationFile = (FileSpecification)action.Get("DestinationFile");
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
                Console.WriteLine(page.Number + " [ID: " + page.Reference + "]");
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
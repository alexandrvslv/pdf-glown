using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Documents.Names;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to inspect the object names within a PDF document.</summary>
    public class NamesParsingSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // 2. Named objects extraction.
                NamedResources names = document.Catalog.Names;
                if (names.Virtual)
                { Console.WriteLine("\nNo names dictionary."); }
                else
                {
                    Console.WriteLine("\nNames dictionary found (" + names.DataContainer.Reference + ")");

                    NamedDestinations namedDestinations = names.Destinations;
                    if (namedDestinations.Virtual)
                    { Console.WriteLine("\nNo named destinations."); }
                    else
                    {
                        Console.WriteLine("\nNamed destinations found (" + namedDestinations.DataContainer.Reference + ")");

                        // Parsing the named destinations...
                        foreach (KeyValuePair<PdfString, Destination> namedDestination in namedDestinations)
                        {
                            PdfString key = namedDestination.Key;
                            Destination value = namedDestination.Value;

                            Console.WriteLine("  Destination '" + key + "' (" + value.DataContainer.Reference + ")");

                            Console.Write("    Target Page: number = ");
                            object pageRef = value.Page;
                            if (pageRef is Int32) // NOTE: numeric page refs are typical of remote destinations.
                            { Console.WriteLine(((int)pageRef) + 1); }
                            else // NOTE: explicit page refs are typical of local destinations.
                            {
                                var page = (PdfPage)pageRef;
                                Console.WriteLine($"{page.Number}; ID = {page.Reference.Number} {page.Reference.Generation}");
                            }
                        }

                        Console.WriteLine("Named destinations count = " + namedDestinations.Count);
                    }
                }
            }
        }
    }
}
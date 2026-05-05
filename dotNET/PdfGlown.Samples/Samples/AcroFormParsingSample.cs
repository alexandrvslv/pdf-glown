using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Forms;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to inspect the AcroForm fields of a PDF document.</summary>
    public class AcroFormParsingSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                var catalog = document.Catalog;

                // 2. Get the acroform!
                AcroForm form = catalog.Form;
                if (form.Virtual)
                { Console.WriteLine("\nNo acroform available (AcroForm dictionary not found)."); }
                else
                {
                    Console.WriteLine("\nIterating through the fields collection...\n");

                    // 3. Showing the acroform fields...
                    Dictionary<string, int> objCounters = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (Field field in form.Fields.Values)
                    {
                        Console.WriteLine("* Field '" + field.FullName + "' (" + field.RefOrSelf + ")");

                        string typeName = field.GetType().Name;
                        Console.WriteLine("    Type: " + typeName);
                        Console.WriteLine("    Value: " + field.Value);
                        Console.WriteLine("    Data: " + field.DataObject.ToString());

                        int widgetIndex = 0;
                        foreach (Widget widget in field.Widgets)
                        {
                            Console.WriteLine("    Widget " + (++widgetIndex) + ":");
                            var widgetPage = widget.Page;
                            Console.WriteLine("      Page: " + (widgetPage == null ? "undefined" : widgetPage.Number + " (" + widgetPage.RefOrSelf + ")"));

                            SKRect widgetBox = widget.GetViewBounds();
                            Console.WriteLine("      Coordinates: {x:" + Math.Round(widgetBox.Left) + "; y:" + Math.Round(widgetBox.Top) + "; width:" + Math.Round(widgetBox.Width) + "; height:" + Math.Round(widgetBox.Height) + "}");
                        }

                        objCounters[typeName] = (objCounters.ContainsKey(typeName) ? objCounters[typeName] : 0) + 1;
                    }

                    int fieldCount = form.Fields.Count;
                    if (fieldCount == 0)
                    { Console.WriteLine("No field available."); }
                    else
                    {
                        Console.WriteLine("\nFields partial counts (grouped by type):");
                        foreach (KeyValuePair<string, int> entry in objCounters)
                        { Console.WriteLine(" " + entry.Key + ": " + entry.Value); }
                        Console.WriteLine("Fields total count: " + fieldCount);
                    }
                }
            }
        }
    }
}
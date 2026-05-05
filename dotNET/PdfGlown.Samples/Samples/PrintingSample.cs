using PdfGlown.Tools;

using System;

namespace PdfGlown.Samples.CLI
{
    /**
      <summary>This sample demonstrates how to print a PDF document.<summary>
      <remarks>Note: printing is currently in pre-alpha stage; therefore this sample is
      nothing but an initial stub (no assumption to work!).</remarks>
    */
    public class PrintingSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // 2. Printing the document...
                Renderer renderer = new Renderer();
                bool silent = false;
                if (renderer.Print(document, silent))
                { Console.WriteLine("Print fulfilled."); }
                else
                { Console.WriteLine("Print discarded."); }
            }
        }
    }
}
using PdfGlown.Tools;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to flatten the AcroForm fields of a PDF document.</summary>
    public class AcroFormFlatteningSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // 2. Flatten the form!
                var formFlattener = new FormFlattener();
                formFlattener.Flatten(document);

                // 3. Serialize the PDF file!
                Serialize(document);
            }
        }
    }
}
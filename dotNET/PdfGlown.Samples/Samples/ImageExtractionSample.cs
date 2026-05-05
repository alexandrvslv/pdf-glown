using PdfGlown.Bytes;
using PdfGlown.Objects;

using System;
using System.IO;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to extract XObject images from a PDF document.</summary>
    /// <remarks>
    ///   <para>Inline images are ignored.</para>
    ///   <para>XObject images other than JPEG aren't currently supported for handling.</para>
    /// </remarks>
    public class ImageExtractionSample : Sample
    {
        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using var document = new PdfDocument(filePath);
            // 2. Iterating through the indirect object collection...
            int index = 0;
            foreach (PdfIndirectObject indirectObject in document.IndirectObjects)
            {
                // Get the data object associated to the indirect object!
                var dataObject = indirectObject.DataObject;
                // Is this data object a stream?
                if (dataObject is PdfStream stream)
                {
                    // Is this stream an image?
                    if (stream.ContainsKey(PdfName.Type)
                      && PdfName.XObject.Equals(stream.Get<PdfName>(PdfName.Type))
                      && PdfName.Image.Equals(stream.Get<PdfName>(PdfName.Subtype)))
                    {
                        // Which kind of image?
                        if (PdfName.DCTDecode.Equals(stream.Get<PdfName>(PdfName.Filter))) // JPEG image.
                        {
                            // Get the image data (keeping it encoded)!
                            var body = ((PdfStream)dataObject).GetInputStreamNoDecode();
                            // Export the image!
                            ExportImage(body, "ImageExtractionSample_" + (index++) + ".jpg");
                        }
                        else // Unsupported image.
                        {
                            Console.WriteLine("Image XObject " + indirectObject.Reference + " couldn't be extracted (filter: " + stream.Get(PdfName.Filter) + ")");
                        }
                    }
                }
            }
        }

        private void ExportImage(IInputStream data, string filename)
        {
            string outputPath = GetOutputPath(filename);
            FileStream outputStream;
            try
            { outputStream = new FileStream(outputPath, FileMode.CreateNew); }
            catch (Exception e)
            { throw new Exception(outputPath + " file couldn't be created.", e); }

            try
            {
                data.CopyTo(outputStream);
                outputStream.Close();
            }
            catch (Exception e)
            { throw new Exception(outputPath + " file writing has failed.", e); }

            Console.WriteLine("Output: " + outputPath);
        }
    }
}
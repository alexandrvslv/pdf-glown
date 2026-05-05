using PdfGlown.Documents.Files;
using PdfGlown.Files;
using PdfGlown.Objects;

using System.IO;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to move stream data outside PDF files and keep external
    /// references to them; it demonstrates also the inverse process (reimporting stream data from
    /// external files).</summary>
    /// <remarks>Note that, due to security concerns, external streams are a discouraged feature which
    /// is often unsupported on third-party viewers and disabled by default on recent Adobe Acrobat
    /// versions; in the latter case, in order to bypass restrictions and allow access to external
    /// streams, users have to enable Enhanced Security from the Preferences dialog, specifying
    /// privileged locations.</remarks>
    public class StreamExternalizationSample : Sample
    {
        public override void Run()
        {
            // 1. Externalizing the streams...
            string externalizedFilePath;
            {
                // 1.1. Opening the PDF file...
                string filePath = PromptFileChoice("Please select a PDF file");
                using var document = new PdfDocument(filePath);
                /*
                  NOTE: As we are going to export streams using paths relative to the output path, it's
                  necessary to ensure they are properly resolved (otherwise they will be written relative to
                  the current user directory).
                */
                document.Path = OutputPath;

                // 1.2. Iterating through the indirect objects to externalize streams...
                int filenameIndex = 0;
                foreach (PdfIndirectObject indirectObject in document.IndirectObjects)
                {
                    var dataObject = indirectObject.DataObject;
                    if (dataObject is PdfStream stream)
                    {
                        if (stream.DataFile == null) // Internal stream to externalize.
                        {
                            stream.SetDataFile(
                              IFileSpecification.Get(
                                document,
                                GetType().Name + "-external" + filenameIndex++),
                              true);// Forces the stream data to be transferred to the external location.

                        }
                    }
                }

                // 1.3. Serialize the PDF file!
                externalizedFilePath = Serialize(document, SerializationModeEnum.Standard);
            }

            // 2. Reimporting the externalized streams...
            {
                // 2.1. Opening the PDF file...
                using var document = new PdfDocument(externalizedFilePath);
                // 2.2. Iterating through the indirect objects to internalize streams...
                foreach (PdfIndirectObject indirectObject in document.IndirectObjects)
                {
                    var dataObject = indirectObject.DataObject;
                    if (dataObject is PdfStream stream)
                    {
                        if (stream.DataFile != null) // External stream to internalize.
                        {
                            // Forces the stream data to be transferred to the internal location.
                            stream.SetDataFile(null, true);
                        }
                    }
                }

                // 2.3. Serialize the PDF file!
                string externalizedFileName = Path.GetFileNameWithoutExtension(externalizedFilePath);
                string internalizedFilePath = externalizedFileName + "-reimported.pdf";
                Serialize(document, internalizedFilePath, SerializationModeEnum.Standard);
            }
        }
    }
}
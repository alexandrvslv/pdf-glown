using PdfGlown.Documents;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Files;

using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Samples.CLI
{
    /// <summary>Abstract sample.</summary>
    public abstract class Sample
    {
        private string inputPath;
        private string outputPath;

        private bool quit;

        /// <summary>Gets whether the sample was exited before its completion.</summary>
        public bool IsQuit() => quit;

        /// <summary>Executes the sample.</summary>
        /// <returns>Whether the sample has been completed.</returns>
        public abstract void Run();

        protected string GetIndentation(int level) => new string(' ', level);

        /// <summary>Gets the path used to serialize output files.</summary>
        /// <param name="fileName">Relative output file path.</param>
        protected string GetOutputPath(string fileName) => fileName != null ? Path.Combine(outputPath, fileName) : outputPath;

        /// <summary>Gets the path to a sample resource.</summary>
        /// <param name="resourceName">Relative resource path.</param>
        protected string GetResourcePath(string resourceName) => Path.Combine(inputPath, resourceName);

        /// <summary>Gets the path used to serialize output files.</summary>
        protected string OutputPath => GetOutputPath(null);

        /// <summary>Prompts a message to the user.</summary>
        /// <param name="message">Text to show.</param>
        protected void Prompt(string message) => Utils.Prompt(message);

        /// <summary>Gets the user's choice from the given request.</summary>
        /// <param name="message">Description of the request to show to the user.</param>
        /// <returns>User choice.</returns>
        protected string PromptChoice(string message)
        {
            Console.Write(message);
            try
            { return Console.ReadLine(); }
            catch
            { return null; }
        }

        /// <summary>Gets the user's choice from the given options.</summary>
        /// <param name="options">Available options to show to the user.</param>
        /// <returns>Chosen option key.</returns>
        protected string PromptChoice(Dictionary<string, string> options)
        {
            Console.WriteLine();
            foreach (KeyValuePair<string, string> option in options)
            {
                Console.WriteLine(
                    (option.Key.Equals("") ? "ENTER" : "[" + option.Key + "]")
                      + " " + option.Value);
            }
            Console.Write("Please select: ");
            return Console.ReadLine();
        }

        protected string PromptFileChoice(string inputDescription)
        {
            string resourcePath = Path.Combine(Path.GetFullPath(inputPath), "pdf");
            Console.WriteLine("\nAvailable PDF files (" + resourcePath + "):");

            // Get the list of available PDF files!
            string[] filePaths = Directory.GetFiles(resourcePath + Path.DirectorySeparatorChar, "*.pdf");

            // Display files!
            for (int index = 0; index < filePaths.Length; index++)
            { Console.WriteLine("[{0}] {1}", index, Path.GetFileName(filePaths[index])); }

            while (true)
            {
                // Get the user's choice!
                Console.Write(inputDescription + ": ");
                try
                { return filePaths[int.Parse(Console.ReadLine())]; }
                catch
                {/* NOOP */}
            }
        }

        /// <summary>Prompts the user for advancing to the next page.</summary>
        /// <param name="page">Next page.</param>
        /// <param name="skip">Whether the prompt has to be skipped.</param>
        /// <returns>Whether to advance.</returns>
        protected bool PromptNextPage(PdfPage page, bool skip)
        {
            int pageIndex = page.Index;
            if (pageIndex > 0 && !skip)
            {
                var options = new Dictionary<string, string>(StringComparer.Ordinal);
                options[""] = "Scan next page";
                options["Q"] = "End scanning";
                if (!PromptChoice(options).Equals(""))
                    return false;
            }

            Console.WriteLine("\nScanning page " + (pageIndex + 1) + "...\n");
            return true;
        }

        /// <summary>Prompts the user for a page index to select.</summary>
        /// <param name="inputDescription">Message prompted to the user.</param>
        /// <param name="pageCount">Page count.</param>
        /// <returns>Selected page index.</returns>
        protected int PromptPageChoice(string inputDescription, int pageCount)
        {
            return PromptPageChoice(inputDescription, 0, pageCount);
        }

        /// <summary>Prompts the user for a page index to select.</summary>
        /// <param name="inputDescription">Message prompted to the user.</param>
        /// <param name="startIndex">First page index, inclusive.</param>
        /// <param name="endIndex">Last page index, exclusive.</param>
        /// <returns>Selected page index.</returns>
        protected int PromptPageChoice(string inputDescription, int startIndex, int endIndex)
        {
            int pageIndex;
            try
            { pageIndex = int.Parse(PromptChoice(inputDescription + " [" + (startIndex + 1) + "-" + endIndex + "]: ")) - 1; }
            catch
            { pageIndex = startIndex; }
            if (pageIndex < startIndex)
            { pageIndex = startIndex; }
            else if (pageIndex >= endIndex)
            { pageIndex = endIndex - 1; }

            return pageIndex;
        }

        /// <summary>Indicates that the sample was exited before its completion.</summary>
        protected void Quit()
        {
            quit = true;
        }

        /// <summary>Serializes the given PDF Glown file object.</summary>
        /// <param name="document">PDF file to serialize.</param>
        /// <returns>Serialization path.</returns>
        protected string Serialize(PdfDocument document) => Serialize(document, null, null, null);

        /// <summary>Serializes the given PDF Glown file object.</summary>
        /// <param name="document">PDF file to serialize.</param>
        /// <param name="serializationMode">Serialization mode.</param>
        /// <returns>Serialization path.</returns>
        protected string Serialize(PdfDocument document, SerializationModeEnum? serializationMode) 
            => Serialize(document, serializationMode, null, null, null);

        /// <summary>Serializes the given PDF Glown file object.</summary>
        /// <param name="document">PDF file to serialize.</param>
        /// <param name="fileName">Output file name.</param>
        /// <returns>Serialization path.</returns>
        protected string Serialize(PdfDocument document, string fileName) 
            => Serialize(document, fileName, null, null);

        /// <summary>Serializes the given PDF Glown file object.</summary>
        /// <param name="document">PDF file to serialize.</param>
        /// <param name="fileName">Output file name.</param>
        /// <param name="serializationMode">Serialization mode.</param>
        /// <returns>Serialization path.</returns>
        protected string Serialize(PdfDocument document, string fileName, SerializationModeEnum? serializationMode)
            => Serialize(document, fileName, serializationMode, null, null, null);

        /// <summary>Serializes the given PDF Glown file object.</summary>
        /// <param name="document">PDF file to serialize.</param>
        /// <param name="title">Document title.</param>
        /// <param name="subject">Document subject.</param>
        /// <param name="keywords">Document keywords.</param>
        /// <returns>Serialization path.</returns>
        protected string Serialize(PdfDocument document, string title, string subject, string keywords) 
            => Serialize(document, null, title, subject, keywords);

        /// <summary>Serializes the given PDF Glown file object.</summary>
        /// <param name="document">PDF file to serialize.</param>
        /// <param name="serializationMode">Serialization mode.</param>
        /// <param name="title">Document title.</param>
        /// <param name="subject">Document subject.</param>
        /// <param name="keywords">Document keywords.</param>
        /// <returns>Serialization path.</returns>
        protected string Serialize(PdfDocument document, SerializationModeEnum? serializationMode, string title, string subject, string keywords) 
            => Serialize(document, GetType().Name, serializationMode, title, subject, keywords);

        /// <summary>serializes the given pdf clown file object.</summary>
        /// <param name="document">pdf file to serialize.</param>
        /// <param name="filename">output file name.</param>
        /// <param name="serializationmode">serialization mode.</param>
        /// <param name="title">document title.</param>
        /// <param name="subject">document subject.</param>
        /// <param name="keywords">document keywords.</param>
        /// <returns>serialization path.</returns>
        protected string Serialize(PdfDocument document, string fileName, SerializationModeEnum? serializationMode, string title, string subject, string keywords)
        {
            ApplyDocumentSettings(document, title, subject, keywords);

            Console.WriteLine();

            if (!serializationMode.HasValue)
            {
                if (document.Reader == null) // New file.
                { serializationMode = SerializationModeEnum.Standard; }
                else // Existing file.
                {
                    Console.WriteLine("[0] Standard serialization");
                    Console.WriteLine("[1] Incremental update");
                    Console.Write("Please select a serialization mode: ");
                    serializationMode = (SerializationModeEnum)(int.TryParse(Console.ReadLine(), out var sInt) ? sInt : 0);
                }
            }

            string outputFilePath = outputPath + fileName + "." + serializationMode + ".pdf";

            // Save the file!
            // NOTE: You can also save to a generic target stream (see Save() method overloads).
            try
            { document.Save(outputFilePath, serializationMode.Value); }
            catch (Exception e)
            {
                Console.WriteLine("File writing failed: " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Console.WriteLine("\nOutput: " + outputFilePath);

            return outputFilePath;
        }

        internal void Initialize(string inputPath, string outputPath)
        {
            this.inputPath = inputPath;
            this.outputPath = outputPath;
        }

        private void ApplyDocumentSettings(PdfDocument document, string title, string subject, string keywords)
        {
            if (title == null)
                return;

            // Viewer preferences.
            var view = document.Catalog.ViewerPreferences;
            view.DocTitleDisplayed = true;

            // Document metadata.
            Information info = document.Information;
            info.Author = "Stefano";
            info.CreationDate = DateTime.Now;
            info.Creator = GetType().FullName;
            info.Title = "PDF Glown - " + title + " sample";
            info.Subject = "Sample about " + subject + " using PDF Glown";
            info.Keywords = keywords;
        }
    }
}
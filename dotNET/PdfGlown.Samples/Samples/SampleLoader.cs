using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace PdfGlown.Samples.CLI
{
    /**
      <summary>Command-line sample loader.</summary>
    */
    public static class SampleLoader
    {
        private class TypeComparer : IComparer<Type>
        {
            public int Compare(Type type1, Type type2)
            {
                return type1.Name.CompareTo(type2.Name);
            }
        }

        private static readonly string ClassName = (typeof(SampleLoader)).FullName;

        private static readonly string Properties_InputPath = ClassName + ".InputPath";
        private static readonly string Properties_OutputPath = ClassName + ".OutputPath";

        private static readonly string QuitChoiceSymbol = "Q";

        public static void Main(string[] args)
        {
            Console.WriteLine("\nSampleLoader running...");

            {
                Assembly pdfGlownAssembly = Assembly.GetAssembly(typeof(Engine));
                Console.WriteLine("\n" + ((AssemblyTitleAttribute)pdfGlownAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title + " version " + pdfGlownAssembly.GetName().Version);
            }

            Run(ConfigurationManager.AppSettings.Get(Properties_InputPath),
              ConfigurationManager.AppSettings.Get(Properties_OutputPath));

            Console.WriteLine("\nSampleLoader finished.\n");
        }

        private static void Run(string inputPath, string outputPath)
        {
            if (!Directory.Exists(outputPath))
            { Directory.CreateDirectory(outputPath); }

            while (true)
            {
                // Get the current assembly!
                Assembly assembly = Assembly.GetExecutingAssembly();
                // Get all the types inside the current assembly!
                List<Type> types = new List<Type>(assembly.GetTypes());
                types.Sort(new TypeComparer());

                Console.WriteLine("\nAvailable samples:");
                // Instantiate the list of available samples!
                List<Type> sampleTypes = new List<Type>();
                // Picking available samples...
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Sample)))
                    {
                        sampleTypes.Add(type);
                        Console.WriteLine("[{0}] {1}", sampleTypes.IndexOf(type), type.Name);
                    }
                }
                Console.WriteLine("[" + QuitChoiceSymbol + "] (Quit)");

                // Getting the user's choice...
                Type sampleType = null;
                do
                {
                    Console.Write("Please select a sample: ");
                    try
                    {
                        string choice = Console.ReadLine();
                        if (choice.ToUpper().Equals(QuitChoiceSymbol)) // Quit.
                            return;

                        sampleType = sampleTypes[Int32.Parse(choice)];
                    }
                    catch
                    {/* NOOP */}
                } while (sampleType == null);

                Console.WriteLine("\n{0} running...", sampleType.Name);

                // Instantiate the sample!
                Sample sample = (Sample)Activator.CreateInstance(sampleType);
                sample.Initialize(inputPath, outputPath);

                // Run the sample!
                try
                {
                    sample.Run();
                    if (!sample.IsQuit())
                    { Utils.Prompt("Sample finished."); }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An exception happened while running the sample:");
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
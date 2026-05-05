using PdfGlown.Documents;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Tools;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PdfGlown.Samples.CLI
{
    /// <summary>This sample demonstrates how to highlight text matching arbitrary patterns.</summary>
    /// <remarks>Highlighting is defined through text markup annotations.</remarks>
    public class TextHighlightSample : Sample
    {
        private class TextHighlighter : TextExtractor.IIntervalFilter
        {
            private IEnumerator matchEnumerator;
            private PdfPage page;

            public TextHighlighter(PdfPage page, MatchCollection matches)
            {
                this.page = page;
                this.matchEnumerator = matches.GetEnumerator();
            }

            public Interval<int> Current
            {
                get
                {
                    Match current = (Match)matchEnumerator.Current;
                    return new Interval<int>(current.Index, current.Index + current.Length);
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {/* NOOP */}

            public bool MoveNext() => matchEnumerator.MoveNext();

            public void Process(Interval<int> interval, ITextString match)
            {
                //var matrix = page.RotateMatrix;
                // Defining the highlight box of the text pattern match...
                var highlightQuads = new List<Quad>();
                {
                    // NOTE: A text pattern match may be split across multiple contiguous lines,
                    // so we have to define a distinct highlight box for each text chunk.
                    Quad? textQuad = null;
                    foreach (TextChar textChar in match.Chars)
                    {
                        var textCharQuad = textChar.Quad;
                        if (!textQuad.HasValue)
                        { textQuad = textCharQuad; }
                        else
                        {
                            if (textCharQuad.MinY > textQuad.Value.MaxY)
                            {
                                highlightQuads.Add(textQuad.Value);
                                textQuad = textCharQuad;
                            }
                            else
                            { textQuad = Quad.Union(textQuad.Value, textCharQuad); }
                        }
                    }
                    highlightQuads.Add(textQuad.Value);
                }
                // Highlight the text pattern match!
                page.Annotations.Add(new TextMarkup(page, highlightQuads, null, TextMarkupType.Highlight));
            }

            public void Reset() => throw new NotSupportedException();
        }

        public override void Run()
        {
            // 1. Opening the PDF file...
            string filePath = PromptFileChoice("Please select a PDF file");
            using (var document = new PdfDocument(filePath))
            {
                // Define the text pattern to look for!
                var textRegEx = PromptChoice("Please enter the pattern to look for: ");
                var pattern = new Regex(textRegEx, RegexOptions.IgnoreCase);

                // 2. Iterating through the document pages...
                var textExtractor = new TextExtractor(true, true);
                foreach (var page in document.Pages)
                {
                    Console.WriteLine("\nScanning page " + page.Number + "...\n");

                    // 2.1. Extract the page text!
                    var textStrings = textExtractor.Extract(page);

                    // 2.2. Find the text pattern matches!
                    var matches = pattern.Matches(TextExtractor.ToString(textStrings));

                    // 2.3. Highlight the text pattern matches!
                    textExtractor.Filter(
                      textStrings,
                      new TextHighlighter(page, matches));
                }

                // 3. Highlighted file serialization.
                Serialize(document);
            }
        }
    }
}
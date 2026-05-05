using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.UI.Text
{
    public class PageTextSelection : IDisposable
    {
        private SKPath? path;

        public List<TextChar> Chars { get; set; } = new List<TextChar>();

        public void Clear()
        {
            Chars.Clear();
            path?.Reset();
        }

        public void Dispose()
        {
            Clear();
            path?.Dispose();
        }

        public SKPath GetPath()
        {
            path ??= new SKPath();
            if (!path.IsEmpty)
                return path;
            foreach (var textChar in Chars.ToArray())
            {
                path.AddPoly(textChar.Quad.GetPoints());
            }
            return path;
        }

        public void AddRange(IEnumerable<TextChar> chars)
        {
            Chars.AddRange(chars);
        }

        public List<Quad> GetHighlightQuads()
        {
            var highlightQuads = new List<Quad>();
            Quad textQuad = Quad.Empty;
            foreach (var textChar in Chars)
            {
                if (textChar.IsEmpty)
                    continue;
                var textCharQuad = textChar.Quad;
                if (textQuad.IsEmpty)
                {
                    textQuad = textCharQuad;
                }
                else
                {
                    if (textCharQuad.MinY > textQuad.MaxY)
                    {
                        highlightQuads.Add(textQuad);
                        textQuad = textCharQuad;
                    }
                    else
                    {
                        textQuad.Add(textCharQuad);
                    }
                }
            }
            highlightQuads.Add(textQuad);
            return highlightQuads;
        }
    }
}
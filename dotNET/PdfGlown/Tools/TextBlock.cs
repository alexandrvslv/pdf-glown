/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Util.Math;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Tools
{
    public class TextBlock : ITextBlock
    {
        public static ITextBlock Transform(ITextBlock textBlock, SKMatrix ctm)
        {
            var buffer = new TextBlock();
            foreach (var textString in textBlock.Strings)
            {
                buffer.Add(TextString.Transform(textString, ctm));
            }
            return buffer;
        }

        private List<ITextString> strings;
        private SKRect? quad;
        private string text;

        public SKRect Box
        {
            get
            {
                if (quad == null)
                {
                    var result = new SKRect();
                    foreach (var textString in Strings)
                    {
                        if (textString.Quad.IsEmpty)
                            continue;
                        if (result.IsEmpty)
                        { result = textString.Quad.GetBounds(); }
                        else
                        { result.Add(textString.Quad); }
                    }
                    quad = result;
                }
                return quad.Value;
            }
        }

        public string Text
        {
            get
            {
                if (text == null)
                {
                    var textBuilder = new System.Text.StringBuilder();
                    foreach (var textString in Strings)
                    {
                        textBuilder.Append(textString.Text);
                    }
                    text = textBuilder.ToString();
                }
                return text;
            }
        }

        public List<ITextString> Strings => strings ??= new();

        public void Add(ITextString textString)
        {
            Strings.Add(textString);
        }
    }
}

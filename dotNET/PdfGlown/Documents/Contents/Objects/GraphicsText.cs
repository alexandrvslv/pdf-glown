/*
  Copyright 2007-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Bytes;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Tokens;
using PdfGlown.Util.Math;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Text object [PDF:1.6:5.3].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class GraphicsText : CompositeObject, IBoxed, ITextBlock
    {
        public static readonly string BeginOperatorKeyword = BeginText.OperatorKeyword;
        public static readonly string EndOperatorKeyword = EndText.OperatorKeyword;

        private static readonly byte[] BeginChunk = BaseEncoding.Pdf.Encode(BeginOperatorKeyword + Symbol.LineFeed);
        private static readonly byte[] EndChunk = BaseEncoding.Pdf.Encode(EndOperatorKeyword + Symbol.LineFeed);
        private List<ITextString> textStrings;
        private string text;
        private SKRect? quad;

        public GraphicsText()
        { }

        public GraphicsText(IList<ContentObject> objects) : base(objects)
        { }

        public SKRect Box
        {
            get
            {
                if (quad == null)
                {
                    var result = new SKRect();
                    foreach (var textString in textStrings)
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
                    foreach (var textString in textStrings)
                    {
                        textBuilder.Append(textString.Text);
                    }
                    text = textBuilder.ToString();
                }
                return text;
            }
        }

        /// <summary>Gets the text strings.</summary>
        public List<ITextString> Strings => textStrings;

        public void Add(ITextString textString) => textStrings.Add(textString);

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            stream.Write(BeginChunk);
            base.WriteTo(stream, context);
            stream.Write(EndChunk);
        }

        public override void OnScanning(GraphicsState state)
        {
            if (textStrings == null)
            {
                textStrings = new List<ITextString>();
                state.Scanner.Context?.TextBlocks?.Add(this);
            }
            var newState = state.PushTextState();
            newState.TextBlock = this;
        }

        public override void OnScanned(GraphicsState state)
        {
            state.PopTextState();
        }

        public SKRect GetBox(GraphicsState state)
        {
            if (textStrings == null)
            {
                Scan(state);
            }
            return Box;
        }

    }
}
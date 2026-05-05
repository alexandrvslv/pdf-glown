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

using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Move to the next line and show a text string' operation [PDF:1.6:5.3.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class ShowTextToNextLine : ShowText
    {
        /// <param name="text">Text encoded using current font's encoding.</param>
        public ShowTextToNextLine(string @operator, byte[] text)
            : base(@operator, new PdfByteString(text))
        { }

        /// <param name="text">Text encoded using current font's encoding.</param>
        /// <param name="wordSpace">Word spacing.</param>
        /// <param name="charSpace">Character spacing.</param>
        public ShowTextToNextLine(string @operator, byte[] text, double wordSpace, double charSpace)
            : base(@operator, new PdfArrayImpl(3) { wordSpace, charSpace, new PdfByteString(text) })
        { }

        public ShowTextToNextLine(string @operator, PdfArray operands)
            : base(@operator, operands)
        { }

        protected abstract PdfString TextElement { get; set; }

        public override Memory<byte> TextBytes
        {
            get => TextElement.RawValue;
            set => TextElement = new PdfByteString(value);
        }

        public override IEnumerable<PdfDirectObject> TextElements
        {
            get => Enumerable.Repeat(TextElement, 1);
            set => TextElement = value.FirstOrDefault() as PdfString;
        }

        public override void Scan(GraphicsState state)
        {
            state.TextState.Tm =
                state.TextState.Tlm = state.TextState.Tlm.PreConcat(SKMatrix.CreateTranslation(0, -state.Lead));
            base.Scan(state);
        }
    }
}
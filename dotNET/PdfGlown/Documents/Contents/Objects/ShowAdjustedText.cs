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

  redistributions retain the above copyright notice, license and disclaimer, along with
  Redistribution and use, with or without modification, are permitted provided that such
  this list of conditions.
*/

using PdfGlown.Bytes;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Show one or more text strings, allowing individual glyph positioning'
    /// operation [PDF:1.6:5.3.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class ShowAdjustedText : ShowText
    {
        public static readonly string OperatorKeyword = "TJ";
        private ByteStream textStream;

        /// <param name="value">Each element can be either a byte array (encoded text) or a number.
        ///    If the element is a byte array (encoded text), this operator shows the text glyphs.
        ///    If it is a number (glyph adjustment), the operator adjusts the next glyph position by that amount.</param>
        public ShowAdjustedText(List<PdfDirectObject> value)
            : base(OperatorKeyword, (PdfDirectObject)new PdfArrayImpl())
        { TextElements = value; }

        internal ShowAdjustedText(PdfArray operands)
            : base(OperatorKeyword, operands)
        { }

        public override Memory<byte> TextBytes
        {
            get
            {
                if (textStream != null)
                {
                    return textStream.AsMemory();
                }
                textStream = new ByteStream();
                foreach (var element in TextElements)
                {
                    if (element is PdfString pdfString)
                    {
                        textStream.Write(pdfString.RawValue.Span);
                    }
                }
                return textStream.AsMemory();
            }
            set => TextElements = new List<PdfDirectObject>() { new PdfByteString(value) };
        }

        public override IEnumerable<PdfDirectObject> TextElements
        {
            get => operands.Get<PdfArrayImpl>(0);
            set
            {
                var elements = operands.Get<PdfArray>(0);
                elements.Clear();
                foreach (PdfDirectObject valueItem in value)
                {
                    elements.Add(valueItem);
                }
            }
        }
    }
}
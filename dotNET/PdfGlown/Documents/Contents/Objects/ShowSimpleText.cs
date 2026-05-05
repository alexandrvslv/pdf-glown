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
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Show a text string' operation [PDF:1.6:5.3.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class ShowSimpleText : ShowText
    {
        public static readonly string OperatorKeyword = "Tj";

        /// <param name="text">Text encoded using current font's encoding.</param>
        public ShowSimpleText(Memory<byte> text) : base(OperatorKeyword, new PdfByteString(text))
        { }

        public ShowSimpleText(PdfArray operands) : base(OperatorKeyword, operands)
        { }

        public override Memory<byte> TextBytes
        {
            get => operands.Get<PdfString>(0).RawValue;
            set => operands.SetSimple(0, new PdfByteString(value));
        }

        public override IEnumerable<PdfDirectObject> TextElements
        {
            get => (PdfArrayImpl)operands;
            set => operands.SetSimple(0, value.FirstOrDefault());
        }
    }
}
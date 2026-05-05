/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Move to the start of the next line, offset from the start of the current line' operation
    /// [PDF:1.6:5.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class TranslateTextRelative : Operation
    {
        public TranslateTextRelative(string @operator, double offsetX, double offsetY)
            : base(@operator, new PdfArrayImpl(2) { offsetX, offsetY })
        { }

        public TranslateTextRelative(string @operator, PdfArray operands)
            : base(@operator, operands)
        { }

        public float OffsetX
        {
            get => operands.GetFloat(0);
            set => operands.Set(0, value);
        }

        public float OffsetY
        {
            get => operands.GetFloat(1);
            set => operands.Set(1, value);
        }

        public override void Scan(GraphicsState state)
        {
            state.TextState.Tlm =
                state.TextState.Tm = state.TextState.Tlm.PreConcat(SKMatrix.CreateTranslation(OffsetX, OffsetY));
        }
    }
}
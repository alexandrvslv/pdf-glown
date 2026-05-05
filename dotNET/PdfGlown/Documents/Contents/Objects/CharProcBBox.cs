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

using PdfGlown.Objects;
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Font Type3 Char Proc Information' operation
    /// [PDF:1.6:4.4.1].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class CharProcBBox : Operation
    {
        public static readonly string OperatorKeyword = "d1";

        public CharProcBBox(double wx, double wy, double x, double y, double width, double height)
            : base(OperatorKeyword,
                  new PdfArrayImpl(6) { wx, wy, x, y, width, height })
        { }

        public CharProcBBox(PdfArray operands) : base(OperatorKeyword, operands)
        { }

        public override void Scan(GraphicsState state)
        {
        }

        public double WX
        {
            get => operands.GetFloat(0);
            set => operands.Set(0, value);
        }

        public double WY
        {
            get => operands.GetFloat(1);
            set => operands.Set(1, value);
        }

        public double Left
        {
            get => operands.GetFloat(2);
            set => operands.Set(2, value);
        }

        public double Top
        {
            get => operands.GetFloat(3);
            set => operands.Set(3, value);
        }

        public double Right
        {
            get => operands.GetFloat(4);
            set => operands.Set(4, value);
        }

        public double Bottom
        {
            get => operands.GetFloat(5);
            set => operands.Set(5, value);
        }

        public SKRect BBox
        {
            get => new SKRect((float)Left, (float)Top, (float)Right, (float)Bottom);
        }
    }
}
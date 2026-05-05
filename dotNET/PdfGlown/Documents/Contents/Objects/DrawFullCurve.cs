/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Util.Math;
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Objects
{
    public sealed class DrawFullCurve : DrawCurve
    {
        /// <summary>Creates a fully-explicit curve.</summary>
        /// <param name="point">Final endpoint.</param>
        /// <param name="control1">First control point.</param>
        /// <param name="control2">Second control point.</param>
        public DrawFullCurve(SKPoint point, SKPoint control1, SKPoint control2)
            : this(point.X, point.Y, control1.X, control1.Y, control2.X, control2.Y)
        { }

        /// <summary>Creates a fully-explicit curve.</summary>
        public DrawFullCurve(double pointX, double pointY, double control1X, double control1Y, double control2X, double control2Y)
            : base(FullOperatorKeyword, new PdfArrayImpl(6)
              {
                  control1X, control1Y,
                  control2X, control2Y,
                  pointX, pointY
              })
        { }

        public DrawFullCurve(PdfArray operands) : base(FullOperatorKeyword, operands)
        { }

        public override SKPoint Control1
        {
            get => operands.ToSKPoint();
            set
            {
                operands.Set(0, value.X);
                operands.Set(1, value.Y);
            }
        }

        public override SKPoint Control2
        {
            get => new SKPoint(operands.GetFloat(2), operands.GetFloat(3));
            set
            {
                operands.Set(2, value.X);
                operands.Set(3, value.Y);
            }
        }

        public override SKPoint Point
        {
            get => new SKPoint(operands.GetFloat(4), operands.GetFloat(5));
            set
            {
                operands.Set(4, value.X);
                operands.Set(5, value.Y);
            }
        }

        public override void Scan(GraphicsState state) => state.Scanner.Path?.CubicTo(Control1, Control2, Point);
    }
}
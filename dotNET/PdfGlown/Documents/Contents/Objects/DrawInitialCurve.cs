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
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Objects
{
    public sealed class DrawInitialCurve : DrawCurve
    {
        /// <summary>Creates a partially-explicit curve.</summary>
        /// <param name="point">Final endpoint.</param>
        /// <param name="control">Explicit control point.</param>
        /// <param name="operator">Operator (either <code>InitialOperator</code> or <code>FinalOperator</code>).
        /// It defines how to interpret the <code>control</code> parameter.</param>
        public DrawInitialCurve(SKPoint point, SKPoint control)
            : base(InitialOperatorKeyword, point, control)
        { }

        public DrawInitialCurve(PdfArray operands) : base(InitialOperatorKeyword, operands)
        { }

        public override SKPoint Control1
        {
            get => SKPoint.Empty;
            set { }
        }

        public override SKPoint Control2
        {
            get => new SKPoint(operands.GetFloat(0), operands.GetFloat(1));
            set
            {
                operands.Set(0, value.X);
                operands.Set(1, value.Y);
            }
        }

        public override SKPoint Point
        {
            get => new SKPoint(operands.GetFloat(2), operands.GetFloat(3));
            set
            {
                operands.Set(2, value.X);
                operands.Set(3, value.Y);
            }
        }

        public override void Scan(GraphicsState state) => state.Scanner.Path?.CubicTo(state.Scanner.Path.LastPoint, Control2, Point);
    }
}
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
using PdfGlown.Util.Math;
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Modify the current transformation matrix (CTM) by concatenating the specified SKMatrix'
    /// operation [PDF:1.6:4.3.3].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class ModifyCTM : Operation
    {
        public static readonly string OperatorKeyword = "cm";
        private SKMatrix? matrix;

        public static ModifyCTM GetResetCTM(GraphicsState state)
        {
            var initialMatrix = state.GetInitialCtm();
            var temp = initialMatrix.PreConcat(state.Ctm);
            return new ModifyCTM(
              temp.Invert()
              // TODO: inverseCtm is a simplification which assumes an identity initial ctm!
              //        SquareMatrix.get(state.Ctm).solve(
              //          SquareMatrix.get(state.GetInitialCtm())
              //          ).toTransform()
              );
        }

        public ModifyCTM(SKMatrix value)
            : this(value.ToPdfArray())
        {
            matrix = value;
        }

        public ModifyCTM(double a, double b, double c, double d, double e, double f)
            : this(new PdfArrayImpl(6) { a, b, c, d, e, f })
        { }

        public ModifyCTM(PdfArray operands)
            : base(OperatorKeyword, operands)
        { }

        public SKMatrix Value => matrix ??= operands.ToSkMatrix();

        public override void Scan(GraphicsState state) => state.Ltm = Value;

    }
}
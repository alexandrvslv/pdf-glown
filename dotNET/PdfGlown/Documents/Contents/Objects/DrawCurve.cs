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
    /// <summary>'Append a cubic Bezier curve to the current path' operation [PDF:1.6:4.4.1].</summary>
    /// <remarks>Such curves are defined by four points:
    /// the two endpoints (the current point and the final point)
    /// and two control points (the first control point, associated to the current point,
    /// and the second control point, associated to the final point).</remarks>
    [Pdf(VersionEnum.PDF10)]
    public abstract class DrawCurve : Operation
    {
        /// <summary>Specifies only the second control point
        /// (the first control point coincides with the initial point of the curve).</summary>
        public static readonly string InitialOperatorKeyword = "v";
        /// <summary>Specifies both control points explicitly.</summary>
        public static readonly string FullOperatorKeyword = "c";
        /// <summary>Specifies only the first control point
        /// (the second control point coincides with the final point of the curve).</summary>
        public static readonly string FinalOperatorKeyword = "y";

        /// <summary>Creates a partially-explicit curve.</summary>
        /// <param name="point">Final endpoint.</param>
        /// <param name="control">Explicit control point.</param>
        /// <param name="operator">Operator (either <code>InitialOperator</code> or <code>FinalOperator</code>).
        /// It defines how to interpret the <code>control</code> parameter.</param>

        public DrawCurve(string @operator, SKPoint point, SKPoint control)
            : base(@operator, new PdfArrayImpl(4)
              {
                  control.X, control.Y,
                  point.X, point.Y
              })
        { }

        public DrawCurve(string @operator, PdfArray operands) : base(@operator, operands)
        { }

        /// <summary>Gets/Sets the first control point.</summary>
        public abstract SKPoint Control1 { get; set; }

        /// <summary>Gets/Sets the second control point.</summary>
        public abstract SKPoint Control2 { get; set; }

        /// <summary>Gets/Sets the final endpoint.</summary>
        public abstract SKPoint Point { get; set; }

    }
}

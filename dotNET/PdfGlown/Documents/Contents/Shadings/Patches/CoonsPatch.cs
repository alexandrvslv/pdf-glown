/*
  Copyright 2010-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using SkiaSharp;
using System;

namespace PdfGlown.Documents.Contents.Shadings.Patches
{
    class CoonsPatch : Patch
    {
        /// <summary>Constructor of a patch for type 6 shading.</summary>
        /// <param name="points">12 control points</param>
        /// <param name="color">4 corner colors</param>
        public CoonsPatch(SKPoint[] points, SKColor[] color)
                : base(color)
        {
            controlPoints = points;
        }

        public override void GetFlag1Edge(Span<SKPoint> points)
        {
            controlPoints.AsSpan(3, 4).CopyTo(points);
        }

        public override void GetFlag2Edge(Span<SKPoint> points)
        {
            controlPoints.AsSpan(6, 4).CopyTo(points);
        }

        public override void GetFlag3Edge(Span<SKPoint> points)
        {
            points[0] = controlPoints[9];
            points[1] = controlPoints[10];
            points[2] = controlPoints[11];
            points[3] = controlPoints[0];
        }

    }
}

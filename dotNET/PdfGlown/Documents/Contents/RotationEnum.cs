/*
  Copyright 2010-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using SkiaSharp;

namespace PdfGlown.Documents.Contents
{
    /// <summary>Rotation (clockwise) [PDF:1.6:3.6.2].</summary>
    public enum RotationEnum
    {
        /// <summary>Downward (0° clockwise).</summary>
        Downward = 0,

        /// <summary>Leftward (90° clockwise).</summary>
        Leftward = 90,
        
        /// <summary>Upward (180° clockwise).</summary>
        Upward = 180,

        /// <summary>Rightward (270° clockwise).</summary>
        Rightward = 270
    }

    internal static class RotationEnumExtension
    {
        /// <summary>Gets the direction corresponding to the given value.</summary>
        public static RotationEnum Get(IPdfNumber value)
        {
            if (value == null)
                return RotationEnum.Downward;

            int normalizedValue = (int)(Math.Round(value.DoubleValue / 90) % 4) * 90;
            if (normalizedValue < 0)
            { normalizedValue += 360 * (int)Math.Ceiling(-normalizedValue / 360d); }
            return (RotationEnum)normalizedValue;
        }

        public static SKSize Transform(this RotationEnum rotation, SKSize size)
        {
            if ((int)rotation % 180 == 0)
                return new SKSize(size.Width, size.Height);
            else
                return new SKSize(size.Height, size.Width);
        }

        public static SKRect RotateRect(this SKRect rect, float rotate)
        {
            var size = SKMatrix.CreateRotationDegrees(rotate).MapVector(rect.Width, rect.Height);
            return SKRect.Create(rect.Left, rect.Top, (float)Math.Abs(Math.Round(size.X, 2)), (float)Math.Abs(Math.Round(size.Y, 2)));
        }

    }
}
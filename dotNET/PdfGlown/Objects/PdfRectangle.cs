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

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Objects
{
    /// <summary>PDF rectangle object [PDF:1.6:3.8.4].</summary>
    /// <remarks>
    ///   <para>Rectangles are described by two diagonally-opposite corners. Corner pairs which don't
    ///   respect the canonical form (lower-left and upper-right) are automatically normalized to
    ///   provide a consistent representation.</para>
    ///   <para>Coordinates are expressed within the PDF coordinate space (lower-left origin and
    ///   positively-oriented axes).</para>
    /// </remarks>
    public sealed class PdfRectangle : PdfArray, IEquatable<PdfRectangle>
    {
        private static readonly HashSet<PdfName> rectTypeName = new()
        {
            PdfName.ArtBox,
            PdfName.BBox,
            PdfName.BleedBox,
            PdfName.CropBox,
            PdfName.FontBBox,
            PdfName.MediaBox,
            PdfName.TrimBox,
            PdfName.Rect
        };

        internal static bool IsMatch(PdfName currentDictionaryKey)
        {
            return currentDictionaryKey != null && rectTypeName.Contains(currentDictionaryKey);
        }

        public static List<PdfDirectObject> Normalize(List<PdfDirectObject> array)
        {
            if (array.Count == 0)
            {
                array.AddRange(Enumerable.Repeat(PdfReal.Zero, 4));
            }
            else if (array.Count > 3)
            {
                if (array.GetDouble(0).CompareTo(array.GetDouble(2)) > 0)
                {
                    var leftCoordinate = array.GetNumber(2);
                    array[2] = (PdfDirectObject)array.GetNumber(0);
                    array[0] = (PdfDirectObject)leftCoordinate;
                }
                if (array.GetDouble(1).CompareTo(array.GetDouble(3)) > 0)
                {
                    var bottomCoordinate = array.GetNumber(3);
                    array[3] = (PdfDirectObject)array.GetNumber(1);
                    array[1] = (PdfDirectObject)bottomCoordinate;
                }
            }
            return array;
        }
        
        public PdfRectangle()
            : this(new List<PdfDirectObject>(4))
        { }

        public PdfRectangle(SKRect rectangle)
            : this(rectangle.Left, rectangle.Bottom, rectangle.Width, rectangle.Height)
        { }

        public PdfRectangle(SKPoint lowerLeft, SKPoint upperRight)
            : this(lowerLeft.X, upperRight.Y, upperRight.X - lowerLeft.X, upperRight.Y - lowerLeft.Y)
        { }

        public PdfRectangle(double left, double top, double width, double height)
            : this(new List<PdfDirectObject>(4)
              {
                  PdfReal.Get(left), // Left (X).
                  PdfReal.Get(top - height), // Bottom (Y).
                  PdfReal.Get(left + width), // Right.
                  PdfReal.Get(top) // Top.
              })
        { }


        internal PdfRectangle(List<PdfDirectObject> baseObject)
            : base(Normalize(baseObject))
        { }

        public double Left
        {
            get => GetDouble(0);
            set => Set(0, value);
        }

        public double Bottom
        {
            get => GetDouble(1);
            set => Set(1, value);
        }

        public double Right
        {
            get => GetDouble(2);
            set => Set(2, value);
        }

        public double Top
        {
            get => GetDouble(3);
            set => Set(3, value);
        }

        public bool Equals(PdfRectangle other)
        {
            return Left.Equals(other.Left)
                && Bottom.Equals(other.Bottom)
                && Right.Equals(other.Right)
                && Top.Equals(other.Top);
        }

        public PdfRectangle Round() => Round(Document?.Configuration?.RealPrecision ?? 5);

        public PdfRectangle Round(int precision)
        {
            Left = Math.Round(Left, precision);
            Bottom = Math.Round(Bottom, precision);
            Right = Math.Round(Right, precision);
            Top = Math.Round(Top, precision);
            return this;
        }

        public double Width
        {
            get => Right - Left;
            set => Right = Left + value;
        }

        public double Height
        {
            get => Top - Bottom;
            set => Bottom = Top - value;
        }

        public double X
        {
            get => Left;
            set => Left = value;
        }

        public double Y
        {
            get => Bottom;
            set => Bottom = value;
        }

        public override bool Equals(object @object)
        {
            return base.Equals(@object);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    
}
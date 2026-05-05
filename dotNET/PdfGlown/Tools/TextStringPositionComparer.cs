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

using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Util.Math;
using System;
using System.Collections.Generic;

namespace PdfGlown.Tools
{
    /// <summary>Text string position comparer.</summary>
    public class TextStringPositionComparer<T> : IComparer<T>
      where T : ITextString
    {
        public static readonly TextStringPositionComparer<T> Default = new();
        /// <summary>Gets whether the specified boxes lay on the same text line.</summary>
        public static bool IsOnTheSameLine(Quad box1, Quad box2)
        {
            // NOTE: In order to consider the two boxes being on the same line,
            // we apply a simple rule of thumb: at least 25% of a box's height MUST
            // lay on the horizontal projection of the other one.
            double minHeight = Math.Min(box1.Height, box2.Height);
            double yThreshold = minHeight * .75;
            return ((box1.MinY > box2.MinY - yThreshold
                && box1.MinY < box2.MaxY + yThreshold - minHeight)
              || (box2.MinY > box1.MinY - yThreshold
                && box2.MinY < box1.MaxY + yThreshold - minHeight));
        }

        public int Compare(T textString1, T textString2)
        {
            var quad1 = textString1.Quad;
            var quad2 = textString2.Quad;
            if (IsOnTheSameLine(quad1, quad2))
            {
                // [FIX:55:0.1.3] In order not to violate the transitive condition, equivalence on x-axis
                // MUST fall back on y-axis comparison.
                int xCompare = quad1.MinX.CompareTo(quad2.MinX);
                if (xCompare != 0)
                    return xCompare;
            }
            return quad1.MinY.CompareTo(quad2.MinY);
        }
    }
}

/*
  Copyright 2006-2010 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using SkiaSharp;

namespace PdfGlown.Documents.Contents
{
    /**
      <summary>Shape to be used at the corners of stroked paths
      [PDF:1.6:4.3.2].</summary>
    */
    [Pdf(VersionEnum.PDF10)]
    public enum LineJoinEnum
    {
        /**
          <summary>Sharp line join.</summary>
        */
        Miter = 0,
        /**
          <summary>Rounded line join.</summary>
        */
        Round = 1,
        /**
          <summary>Squared-off line join.</summary>
        */
        Bevel = 2
    };

    internal static class LineJoinEnumExtension
    {
        public static SKStrokeJoin ToSkia(this LineJoinEnum lineJoin)
        {
            switch (lineJoin)
            {
                case LineJoinEnum.Bevel:
                    return SKStrokeJoin.Bevel;
                case LineJoinEnum.Miter:
                    return SKStrokeJoin.Miter;
                case LineJoinEnum.Round:
                    return SKStrokeJoin.Round;
                default:
                    throw new NotSupportedException(lineJoin + " convertion not supported.");
            }
        }
    }
}
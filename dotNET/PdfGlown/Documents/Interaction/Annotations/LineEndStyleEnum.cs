/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Util;

using System;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Line ending style [PDF:1.6:8.4.5].</summary>
    public enum LineEndStyleEnum
    {
        // Square.
        Square,
        
        // Circle.
        Circle,

        // Diamond.
        Diamond,
        
        // Open arrow.
        OpenArrow,

        // Closed arrow.
        ClosedArrow,
        
        // None.
        None,
        
        // Butt.
        Butt,
        
        // Reverse open arrow.
        ReverseOpenArrow,
        
        // Reverse closed arrow.
        ReverseClosedArrow,
        
        // Slash.
        Slash
    }

    internal static class LineEndStyleEnumExtension
    {
        private static readonly BiDictionary<LineEndStyleEnum, string> codes = new()
        {
            [LineEndStyleEnum.Square] = PdfName.Square.StringValue,
            [LineEndStyleEnum.Circle] = PdfName.Circle.StringValue,
            [LineEndStyleEnum.Diamond] = PdfName.Diamond.StringValue,
            [LineEndStyleEnum.OpenArrow] = PdfName.OpenArrow.StringValue,
            [LineEndStyleEnum.ClosedArrow] = PdfName.ClosedArrow.StringValue,
            [LineEndStyleEnum.None] = PdfName.None.StringValue,
            [LineEndStyleEnum.Butt] = PdfName.Butt.StringValue,
            [LineEndStyleEnum.ReverseOpenArrow] = PdfName.ROpenArrow.StringValue,
            [LineEndStyleEnum.ReverseClosedArrow] = PdfName.RClosedArrow.StringValue,
            [LineEndStyleEnum.Slash] = PdfName.Slash.StringValue
        };

        public static LineEndStyleEnum Get(string name)
        {
            if (name == null)
                return LineEndStyleEnum.None;

            LineEndStyleEnum? lineEndStyle = codes.GetKey(name);
            if (!lineEndStyle.HasValue)
                throw new NotSupportedException("Line end style unknown: " + name);

            return lineEndStyle.Value;
        }

        public static string GetName(this LineEndStyleEnum lineEndStyle) => codes[lineEndStyle];
    }
}
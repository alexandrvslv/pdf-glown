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

using PdfGlown.Util.Math;
using System;
using System.Diagnostics.CodeAnalysis;

namespace PdfGlown.Documents.Contents.Scanner
{
    /// <summary>Text character.</summary>
    /// <remarks>It describes a text element extracted from content streams.</remarks>
    public readonly struct TextChar : IEquatable<TextChar>
    {
        public static readonly TextChar Empty = new TextChar(char.MinValue, Quad.Empty);
        private readonly Quad quad;
        private readonly char value;

        public TextChar(char value, Quad box)
        {
            this.value = value;
            quad = box;
        }

        public readonly Quad Quad => quad;

        public readonly char Value => value;

        public bool IsEmpty => value == char.MinValue;

        public bool Contains(char value) => this.value == value;

        public override string ToString() => Value.ToString();

        public override int GetHashCode()
        {
            return HashCode.Combine(value, quad);
        }

        public override bool Equals([NotNullWhen(true)] object obj) 
            => obj is TextChar textChar
                ? Equals(textChar)
                : false;

        public bool Equals(TextChar other) 
            => value.Equals(other.Value)
                && quad.Equals(other.quad);

    }
}
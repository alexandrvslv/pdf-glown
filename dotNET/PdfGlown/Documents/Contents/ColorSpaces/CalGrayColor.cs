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

using PdfGlown.Objects;

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>Single-component CIE-based color value [PDF:1.6:4.5.4].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class CalGrayColor : LeveledColor
    {
        public CalGrayColor(CalGrayColorSpace colorSpace, double g)
            : this(null, new PdfArrayImpl(1) { NormalizeComponent(g) })
        { }

        internal CalGrayColor(CalGrayColorSpace colorSpace, PdfArray components)
            : base(colorSpace, components)
        { }

        /// <summary>Gets/Sets the gray component.</summary>
        public float G
        {
            get => this[0];
            set => this[0] = value;
        }
    }
}
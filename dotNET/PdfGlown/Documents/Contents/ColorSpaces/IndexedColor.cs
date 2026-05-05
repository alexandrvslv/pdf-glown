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
    /// <summary>Indexed color value [PDF:1.6:4.5.5].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class IndexedColor : Color
    {
        public IndexedColor(ColorSpace colorSpace, int index)
            : this(colorSpace, new PdfArrayImpl(1) { index })
        { }

        internal IndexedColor(ColorSpace colorSpace, PdfArray components)//TODO:consider color space reference!
            : base(colorSpace, components)
        { }

        public override PdfArray Components => (PdfArray)DataObject;

        /// <summary>Gets the color index.</summary>
        public int Index
        {
            get => Components.GetInt(0);
            set => Components.Set(0, value);
        }        
    }
}
/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections.Generic;

namespace PdfGlown.Documents.Multimedia
{
    /// <summary>Selector rendition [PDF:1.7:9.1.2].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class SelectorRendition : Rendition
    {
        private Renditions renditions;

        public SelectorRendition(PdfDocument context)
            : base(context, PdfName.SR)
        { }

        internal SelectorRendition(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets an ordered collection of renditions. The first viable media rendition found
        /// in the array, or nested within a selector rendition in the array, should be used.</summary>
        public Renditions Renditions
        {
            get => renditions ??= new(GetOrCreate<PdfArrayImpl>(PdfName.R));
            set => Set(PdfName.R, renditions = value);
        }
    }
}
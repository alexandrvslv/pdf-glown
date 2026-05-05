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

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Multimedia
{
    /// <summary>Media clip object [PDF:1.7:9.1.3].</summary>
    [Pdf(VersionEnum.PDF15)]
    public abstract class MediaClip : PdfDictionary
    {
        /// <summary>Wraps a clip base object into a clip object.</summary>
        internal static MediaClip Create(Dictionary<PdfName, PdfDirectObject> baseObject)
        {
            var subtype = baseObject.Get<PdfName>(PdfName.S);
            if (PdfName.MCD.Equals(subtype))
                return new MediaClipData(baseObject);
            else if (PdfName.MCS.Equals(subtype))
                return new MediaClipSection(baseObject);
            else
                throw new ArgumentException("It doesn't represent a valid clip object.", "baseObject");
        }

        protected MediaClip(PdfDocument context, PdfName subtype)
            : base(context, new Dictionary<PdfName, PdfDirectObject>(2)
            {
                { PdfName.Type, PdfName.MediaClip},
                { PdfName.S, subtype},
            })
        { }

        protected MediaClip(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the actual media data.</summary>
        /// <returns>Either a <see cref="FullFileSpecification"/> or a <see cref="FormXObject"/>.</returns>
        public abstract IPdfDataObject Data { get; set; }
    }
}
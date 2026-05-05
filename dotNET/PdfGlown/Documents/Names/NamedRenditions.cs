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

using PdfGlown.Documents.Multimedia;
using PdfGlown.Objects;

namespace PdfGlown.Documents.Names
{
    /// <summary>Named renditions [PDF:1.6:3.6.3].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class NamedRenditions : NameTree<Rendition>
    {
        public NamedRenditions(PdfDocument context)
            : base(context)
        { }

        public NamedRenditions(PdfDirectObject baseObject)
            : base(baseObject)
        { }

        public override bool Remove(PdfString key)
        {
            Rendition oldValue = this[key];
            bool removed = base.Remove(key);
            UpdateName(oldValue, null);
            return removed;
        }

        public override Rendition this[PdfString key]
        {
            get => base[key];
            set
            {
                Rendition oldValue = base[key];
                base[key] = value;
                UpdateName(oldValue, null);
                UpdateName(value, key);
            }
        }

        protected override Rendition WrapValue(PdfDirectObject baseObject) => (Rendition)baseObject?.Resolve(PdfName.Rendition);

        /// <summary>Ensures name reference synchronization for the specified rendition [PDF:1.7:9.1.2].
        /// </summary>
        private void UpdateName(Rendition rendition, PdfString name)
        {
            if (rendition == null)
                return;

            rendition[PdfName.N] = name;
        }
    }
}
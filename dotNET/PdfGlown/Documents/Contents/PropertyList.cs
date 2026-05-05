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

using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents
{
    /// <summary>Private information meaningful to the program (application or plugin extension)
    /// creating the marked content [PDF:1.6:10.5.1].</summary>
    [Pdf(VersionEnum.PDF12)]
    public class PropertyList : PdfDictionary
    {
        /// <summary>Wraps the specified base object into a property list object.</summary>
        /// <param name="dictionary">Base object of a property list object.</param>
        /// <returns>Property list object corresponding to the base object.</returns>
        internal static PropertyList Create(Dictionary<PdfName, PdfDirectObject> dictionary)
        {
            var type = dictionary.Get<PdfName>(PdfName.Type);
            if (Layer.TypeName.Equals(type))
                return new Layer(dictionary);
            else if (LayerMembership.TypeName.Equals(type))
                return new LayerMembership(dictionary);
            else
                return new PropertyList(dictionary);
        }

        public PropertyList(PdfDocument context, Dictionary<PdfName, PdfDirectObject> baseDataObject)
            : base(context, baseDataObject)
        { }

        public PropertyList(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public int Id
        {
            get => GetInt(PdfName.MCID);
            set => Set(PdfName.MCID, value);
        }
    }
}
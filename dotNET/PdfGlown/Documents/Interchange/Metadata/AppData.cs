/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library"
  (the Program): see the accompanying README files for more info.

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

using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interchange.Metadata
{
    /// <summary>Private application data dictionary [PDF:1.7:10.4].</summary>
    [Pdf(VersionEnum.PDF13)]
    public class AppData : PdfDictionary
    {
        public AppData(PdfDocument context) 
            : base(context, new ())
        { }

        internal AppData(Dictionary<PdfName, PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the private data associated to the application.</summary>
        /// <remarks>It can be any type, although dictionary is its typical form.</remarks>
        public PdfDirectObject Data
        {
            get => Get(PdfName.Private);
            set => Set(PdfName.Private, value?.Unresolve());
        }

        /// <summary>Gets the date when the contents of the holder (<see cref="PdfCatalog">document</see>,
        /// <see cref="PdfPage">page</see>, or <see cref="FormXObject">form</see>) were most recently
        /// modified by this application.</summary>
        /// <remarks>To update it, use the <see cref="IAppDataHolder.Touch(PdfName)"/> method of the
        /// holder.</remarks>
        public DateTime ModificationDate
        {
            get => GetDate(PdfName.LastModified) ?? DateTime.MinValue;
            internal set => Set(PdfName.LastModified, value);
        }
    }
}

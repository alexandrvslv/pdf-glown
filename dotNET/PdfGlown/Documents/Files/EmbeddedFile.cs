/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Bytes;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Documents.Files
{
    /// <summary>Embedded file [PDF:1.6:3.10.3].</summary>
    [Pdf(VersionEnum.PDF13)]
    public sealed class EmbeddedFile : PdfStream
    {
        /// <summary>Creates a new embedded file inside the document.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="path">Path of the file to embed.</param>
        public static EmbeddedFile Get(PdfDocument context, string path)
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new EmbeddedFile(context, new ByteStream(fileStream));
        }

        /// <summary>Creates a new embedded file inside the document.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="stream">File stream to embed.</param>
        public EmbeddedFile(PdfDocument context, IInputStream stream)
            : base(context, new(1) { 
                { PdfName.Type, PdfName.EmbeddedFile },
                { PdfName.Length, PdfInteger.Get(stream.Length) }
            }, stream)
        { }

        internal EmbeddedFile(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        internal EmbeddedFile(Dictionary<PdfName, PdfDirectObject> baseObject, IInputStream stream)
            : base(baseObject, stream)
        { }

        /// <summary>Gets/Sets the creation date of this file.</summary>
        public DateTime? CreationDate
        {
            get => Params.GetNDate(PdfName.CreationDate);
            set => Params.Set(PdfName.CreationDate, value);
        }

        /// <summary>Gets the data contained within this file.</summary>
        public IInputStream Data => GetInputStream();

        /// <summary>Gets/Sets the MIME media type name of this file [RFC 2046].</summary>
        public string MimeType
        {
            get => GetString(PdfName.Subtype);
            set => SetName(PdfName.Subtype, value);
        }

        /// <summary>Gets/Sets the modification date of this file.</summary>
        public DateTime? ModificationDate
        {
            get => Params.GetNDate(PdfName.ModDate);
            set => Params.Set(PdfName.ModDate, value);
        }

        /// <summary>Gets/Sets the size of this file, in bytes.</summary>
        public int Size
        {
            get => Params.GetInt(PdfName.Size);
            set => Params.Set(PdfName.Size, value);
        }

        /// <summary>Gets the file parameters.</summary>
        private PdfDictionary Params => GetOrCreate<PdfDictionary>(PdfName.Params);

    }
}
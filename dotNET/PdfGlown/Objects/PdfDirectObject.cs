/*
  Copyright 2006-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Tokens;
using PdfGlown.Util;
using System;

namespace PdfGlown.Objects
{
    /// <summary>Abstract PDF direct object.</summary>
    public abstract class PdfDirectObject : PdfObject, IComparable<PdfDirectObject>, IPdfDataObject
    {
        private static readonly byte[] NullChunk = BaseEncoding.Pdf.Encode(Keyword.Null);

        /// <summary>Ensures that the given direct object is properly represented as string.</summary>
        /// <remarks>This method is useful to force null pointers to be expressed as PDF null objects.</remarks>
        internal static string ToString(PdfDirectObject obj) => obj?.ToString() ?? Keyword.Null;

        /// <summary>Ensures that the given direct object is properly serialized.</summary>
        /// <remarks>This method is useful to force null pointers to be expressed as PDF null objects.</remarks>
        internal static void WriteTo(IOutputStream stream, PdfDocument context, PdfDirectObject obj)
        {
            if (obj == null)
            { stream.Write(NullChunk); }
            else
            { obj.WriteTo(stream, context); }
        }

        protected PdfDirectObject()
        { }

        protected PdfDirectObject(PdfObjectStatus status)
            : base(status)
        { }

        public virtual PdfDirectObject RefOrSelf => Reference ?? this;

        /// <summary>Retrieves the name possibly associated to this object, walking through the document's
        /// name dictionary.</summary>
        protected virtual PdfString RetrieveName()
        {
            return Document.Catalog.Names.Get(GetType()) is IBiDictionary biDictionary
                ? biDictionary.GetKey(this) as PdfString
                : null;
        }

        /// <summary>Retrieves the object name, if available; otherwise, behaves like
        /// <see cref="PdfDirectObject.RefOrSelf"/>.</summary>
        protected PdfDirectObject RetrieveNamedBaseObject()
        {
            return RetrieveName() ?? RefOrSelf;
        }

        public abstract int CompareTo(PdfDirectObject obj);

        internal virtual void AfterParse() { }

    }
}
/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Objects;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.XObjects
{
    /// <summary>External graphics object whose contents are defined by a self-contained content stream,
    /// separate from the content stream in which it is used [PDF:1.6:4.7].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class XObject : PdfStream, ILayerable
    {
        internal static PdfDictionary Create(Dictionary<PdfName, PdfDirectObject> dictionary)
        {
            var subtype = dictionary.Get<PdfName>(PdfName.Subtype);
            if (PdfName.Image.Equals(subtype))
                return new ImageXObject(dictionary);
            else if (subtype == null && dictionary.ContainsKey(PdfName.FormType))
            {
                //NOTE: Sometimes the form stream's header misses the mandatory Subtype entry; therefore, here
                //we force integrity for convenience (otherwise, content resource allocation may fail, for
                //example in case of Acroform flattening).
                dictionary[PdfName.Subtype] = PdfName.Form;
            }
            return new FormXObject(dictionary);
        }

        /// <summary>Creates a new external object inside the document.</summary>
        protected XObject(PdfDocument context)
            : this(context, new(), new ByteStream())
        { }

        /// <summary>Creates a new external object inside the document.</summary>
        protected XObject(PdfDocument context, Dictionary<PdfName, PdfDirectObject> baseDataObject, IInputStream inputStream)
            : base(context, baseDataObject, inputStream)
        {
            baseDataObject[PdfName.Type] = PdfName.XObject;
        }

        protected XObject(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        protected XObject(Dictionary<PdfName, PdfDirectObject> baseObject, IInputStream inputStream)
            : base(baseObject, inputStream)
        { }

        /// <summary>Gets/Sets the mapping from external-object space to user space.</summary>
        public abstract SKMatrix Matrix { get; set; }

        /// <summary>Gets/Sets the external object size.</summary>
        public abstract SKSize Size { get; set; }

        public LayerEntity Layer
        {
            get => Get<LayerEntity>(PdfName.OC);
            set => Set(PdfName.OC, value?.Membership);
        }
    }
}
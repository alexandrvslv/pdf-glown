/*
  Copyright 2012-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Tokens;
using System.Collections.Generic;

namespace PdfGlown.Objects
{
    /// <summary>Visitor object.</summary>
    public class Visitor : IVisitor
    {
        public virtual PdfObject Visit(ObjectStream obj, PdfName parentKey, object data)
        {
            foreach (PdfDirectObject value in obj.Values)
            { value.Accept(this, parentKey, data); }
            return obj;
        }

        public virtual PdfObject Visit(PdfArray obj, PdfName parentKey, object data)
        {
            foreach (var item in obj.GetItems())
            {
                item?.Accept(this, parentKey, data);
            }
            return obj;
        }

        public virtual PdfObject Visit(PdfBoolean obj, PdfName parentKey, object data) => obj;

        public PdfObject Visit(PdfDirectObject obj, PdfName parentKey, object data) => obj.Accept(this, parentKey, data);

        public virtual PdfObject Visit(PdfDate obj, PdfName parentKey, object data) => obj;

        public virtual PdfObject Visit(PdfDictionary obj, PdfName parentKey, object data)
        {
            foreach (KeyValuePair<PdfName, PdfDirectObject> entry in obj)
            {
                entry.Value?.Accept(this, entry.Key, data);
            }
            return obj;
        }

        public virtual PdfObject Visit(PdfIndirectObject obj, PdfName parentKey, object data)
        {
            obj.GetDataObject(parentKey).Accept(this, parentKey, data);
            return obj;
        }

        public virtual PdfObject Visit(PdfInteger obj, PdfName parentKey, object data) => obj;

        public virtual PdfObject Visit(PdfName obj, PdfName parentKey, object data) => obj;

        public virtual PdfObject Visit(PdfReal obj, PdfName parentKey, object data) => obj;

        public virtual PdfObject Visit(PdfReference obj, PdfName parentKey, object data)
        {
            obj.IndirectObject.Accept(this, parentKey, data);
            return obj;
        }

        public virtual PdfObject Visit(PdfStream obj, PdfName parentKey, object data)
        {
            obj.Accept(this, parentKey, data);
            return obj;
        }

        public virtual PdfObject Visit(PdfString obj, PdfName parentKey, object data) => obj;

        public virtual PdfObject Visit(PdfTextString obj, PdfName parentKey, object data) => obj;

        public virtual PdfObject Visit(XRefStream obj, PdfName parentKey, object data) => obj;
    }
}
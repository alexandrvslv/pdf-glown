/*
  Copyright 2011-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace PdfGlown.Documents.Interchange.Metadata
{
    /// <summary>Metadata stream [PDF:1.6:10.2.2].</summary>
    [Pdf(VersionEnum.PDF14)]
    public sealed class PdfMetadata : PdfStream
    {
        private XmlDocument content;

        public PdfMetadata(PdfDocument context)
            : base(context, new Dictionary<PdfName, PdfDirectObject>(2)
                      {
                          { PdfName.Type, PdfName.Metadata },
                          { PdfName.Subtype, PdfName.XML }
                      })
        { }

        internal PdfMetadata(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        internal PdfMetadata(Dictionary<PdfName, PdfDirectObject> baseObject, IInputStream inputStream) 
            : base(baseObject, inputStream)
        { }

        ///<summary> Gets/Sets the metadata contents.</summary>
        public XmlDocument Content
        {
            get
            {
                if (content == null)
                {
                    var body = GetInputStream();
                    if (body.Length > 0)
                    {
                        content = new XmlDocument();
                        content.Load((Stream)body);
                    }
                    else
                    { content = null; }

                }
                return content;
            }
            set
            {
                var body = GetOutputStream();
                body.SetLength(0);
                value.Save((Stream)body);
                content = value;
            }
        }
    }
}
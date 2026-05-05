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
using PdfGlown.Tokens;

using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Marked-content sequence [PDF:1.6:10.5].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class GraphicsMarkedContent : CompositeObject
    {
        private static readonly byte[] EndChunk = BaseEncoding.Pdf.Encode(EndMarkedContent.OperatorKeyword + Symbol.LineFeed);

        private BeginMarkedContent header;

        public GraphicsMarkedContent(BeginMarkedContent header)
            : this(header, new List<ContentObject> { header })
        { }

        public GraphicsMarkedContent(BeginMarkedContent header, IList<ContentObject> objects)
            : base(objects)
        { this.header = header; }

        /// <summary>Gets/Sets information about this marked-content sequence.</summary>
        public override Operation Header
        {
            get => header;
            set => header = (BeginMarkedContent)value;
        }

        public string Type => header?.Operands.Count > 0 ? (header.Operands.Get<PdfName>(0)?.RawValue) : null;

        public ContentMarker MarkerHeader { get => header; }

        public override void Scan(GraphicsState state)
        {
            //if (header.Properties is Layer layer
            //    && !layer.Visible)
            //    return;
            base.Scan(state);
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            base.WriteTo(stream, context);
            stream.Write(EndChunk);
        }
    }
}
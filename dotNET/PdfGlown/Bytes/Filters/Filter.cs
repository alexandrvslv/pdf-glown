/*
  Copyright 2006-2010 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Bytes.Filters
{
    /// <summary>Abstract filter [PDF:1.6:3.3].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class Filter
    {
        private static readonly Filter ASCII85Filter = new ASCII85Filter();
        private static readonly Filter ASCIIHexFilter = new ASCIIHexFilter();
        private static readonly Filter FlateDecode = new FlateFilter();
        private static readonly Filter CCITTFaxDecode = new CCITTFaxFilter();
        private static readonly Filter JBIG2Decode = new JBIG2Filter();
        private static readonly Filter JPXDecode = new JPXFilter();
        private static readonly Filter DCTFilter = new DCTFilter();
        private static readonly Filter LZWFilter = new LZWFilter();

        private static readonly Dictionary<PdfName, Filter> cache = new(16)
        {
            { PdfName.FlateDecode, FlateDecode },
            { PdfName.Fl, FlateDecode },
            { PdfName.LZWDecode, LZWFilter },
            { PdfName.LZW, LZWFilter },
            { PdfName.ASCIIHexDecode, ASCIIHexFilter },
            { PdfName.AHx, ASCIIHexFilter },
            { PdfName.ASCII85Decode, ASCII85Filter },
            { PdfName.A85, ASCII85Filter },
            { PdfName.CCITTFaxDecode, CCITTFaxDecode },
            { PdfName.CCF, CCITTFaxDecode },
            { PdfName.DCTDecode, DCTFilter },
            { PdfName.DCT, DCTFilter },
            { PdfName.JBIG2Decode, JBIG2Decode },
            { PdfName.JPXDecode, JPXDecode },
        };

        ///<summary>Gets a specific filter object.</summary>
        ///<param name="name">Name of the requested filter.</param>
        ///<returns>Filter object associated to the name.</returns>
        public static Filter Get(PdfName name)
        {
            //NOTE: This is a factory singleton method for any filter-derived object.
            if (name == null)
                return null;
            return cache.TryGetValue(name, out var filter) ? filter
                : name.Equals(PdfName.RunLengthDecode)
              || name.Equals(PdfName.RL)
              || name.Equals(PdfName.Crypt)
                ? throw new NotImplementedException(name.StringValue)
                : null;
        }

        protected Filter()
        { }

        public abstract Memory<byte> Decode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header);

        public abstract Memory<byte> Encode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header);
    }
}
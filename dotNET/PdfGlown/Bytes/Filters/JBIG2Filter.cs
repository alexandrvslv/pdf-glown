/*
  Copyright 2006-2013 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    
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

using PdfGlown.Bytes.Filters.JBig;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Bytes.Filters
{
    public sealed class JBIG2Filter : Filter
    {
        internal JBIG2Filter()
        { }

        public override Memory<byte> Decode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            var jbig2Image = new Jbig2Image();

            var chunks = new List<ImageChunk>();
            if (parameters is PdfDictionary parametersDict)
            {
                if (parametersDict.Get<PdfStream>(PdfName.JBIG2Globals) is PdfStream pdfStream)
                {
                    var globals = pdfStream.GetInputStream();
                    chunks.Add(new ImageChunk(data: globals));
                }
            }
            chunks.Add(new ImageChunk(data));
            var imageData = jbig2Image.ParseChunks(chunks);
            var dataLength = imageData.Length;

            // JBIG2 had black as 1 and white as 0, inverting the colors
            for (var i = 0; i < dataLength; i++)
            {
                imageData[i] ^= 0xff;
            }
            return imageData;
        }

        public override Memory<byte> Encode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            return data.AsMemory();
        }
    }


}
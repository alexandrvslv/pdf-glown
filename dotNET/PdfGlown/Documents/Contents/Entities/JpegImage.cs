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
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Documents.Contents.Entities
{
    /// <summary>JPEG image object [ISO 10918-1;JFIF:1.02].</summary>
    public sealed class JpegImage : Image
    {
        public JpegImage(Stream stream) : base(stream)
        { Load(); }

        public override ContentObject ToInlineObject(PrimitiveComposer composer)
        {
            return composer.Add(
              new GraphicsInlineImage(
                new InlineImageHeader(
                  new PdfArrayImpl
                  {
                      PdfName.W, Width,
                      PdfName.H, Height,
                      PdfName.CS, PdfName.RGB,
                      PdfName.BPC, BitsPerComponent,
                      PdfName.F, PdfName.DCT
                  }),
                new InlineImageBody(new ByteStream(Stream))));
        }

        public override XObject ToXObject(PdfDocument context)
        {
            return new ImageXObject(
              context,
              new Dictionary<PdfName, PdfDirectObject>(5)
              {
                  { PdfName.Width, PdfInteger.Get(Width) },
                  { PdfName.Height, PdfInteger.Get(Height) },
                  { PdfName.BitsPerComponent, PdfInteger.Get(BitsPerComponent) },
                  { PdfName.ColorSpace, PdfName.DeviceRGB },
                  { PdfName.Filter, PdfName.DCTDecode }
              },
              new ByteStream(Stream));
        }

        private void Load()
        {
            // NOTE: Big-endian data expected.
            Stream stream = Stream;
            var streamReader = new StreamContainer(stream);

            int index = 4;
            stream.Seek(index, SeekOrigin.Begin);
            byte[] markerBytes = new byte[2];
            while (true)
            {
                index += streamReader.ReadUInt16();
                stream.Seek(index, SeekOrigin.Begin);

                stream.ReadExactly(markerBytes, 0, 2);
                index += 2;

                // Frame header?
                if (markerBytes[0] == 0xFF
                  && markerBytes[1] == 0xC0)
                {
                    stream.Seek(2, SeekOrigin.Current);
                    // Get the image bits per color component (sample precision)!
                    BitsPerComponent = stream.ReadByte();
                    // Get the image size!
                    Height = streamReader.ReadUInt16();
                    Width = streamReader.ReadUInt16();

                    break;
                }
            }
        }
    }
}
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
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents.XObjects
{
    /// <summary>Image external object [PDF:1.6:4.8.4].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class ImageXObject : XObject, IImageObject
    {
        private ColorSpace colorSpace;
        private int? bitsPerComponent;
        private SKSize? size;
        private float[] decode;

        public ImageXObject(PdfDocument context, Dictionary<PdfName, PdfDirectObject> baseDataObject, IInputStream stream)
            : base(context, baseDataObject, stream)
        {
            //NOTE: It's caller responsability to adequately populate the stream
            //header and body in order to instantiate a valid object; header entries like
            //'Width', 'Height', 'ColorSpace', 'BitsPerComponent' MUST be defined
            //appropriately.
            baseDataObject[PdfName.Subtype] = PdfName.Image;
        }

        internal ImageXObject(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        internal ImageXObject(Dictionary<PdfName, PdfDirectObject> baseObject, IInputStream inputStream)
            : base(baseObject, inputStream)
        { }

        /// <summary>Gets the number of bits per color component.</summary>
        public int BitsPerComponent => bitsPerComponent ??= GetInt(PdfName.BitsPerComponent, 8);

        /// <summary>Gets the color space in which samples are specified.</summary>
        public ColorSpace ColorSpace => colorSpace ??= ColorSpace.Wrap(Get(PdfName.ColorSpace));

        public IInputStream Data => GetInputStreamNoDecode();

        public override SKMatrix Matrix
        {
            get
            {
                var size = Size;
                //NOTE: Image-space-to-user-space matrix is [1/w 0 0 1/h 0 0],
                //where w and h are the width and height of the image in samples [PDF:1.6:4.8.3].
                return new SKMatrix
                {
                    Values = new float[] { 1f / size.Width, 0, 0, 0, 1f / size.Height, 0, 0, 0, 1 }
                };
            }
            set
            {/* NOOP. */}
        }

        IDictionary<PdfName, PdfDirectObject> IImageObject.Header => this;

        /// <summary>Gets the size of the image (in samples).</summary>
        public override SKSize Size
        {
            get => size ??= new SKSize(
                  GetInt(PdfName.Width),
                  GetInt(PdfName.Height));
            set => throw new NotSupportedException();
        }

        public SKImage Load(GraphicsState state)
        {
            if (Document.Cache.TryGetValue(Reference, out var existingBitmap))
            {
                return (SKImage)existingBitmap;
            }

            var image = BitmapLoader.Load(this, state);
            Document.Cache[Reference] = image;
            return image;
        }

        public IImageObject SMask
        {
            get => Get<ImageXObject>(PdfName.SMask);
            set => Set(PdfName.SMask, (PdfDictionary)value);
        }

        public PdfDirectObject Mask
        {
            get => Get(PdfName.Mask);
            set => Set(PdfName.Mask, value);
        }

        public PdfArray Matte => Get<PdfArray>(PdfName.Matte);

        public float[] Decode
        {
            get => decode ??= Get<PdfArray>(PdfName.Decode)?.ToFloatArray();
            set => Set(PdfName.Decode, new PdfArrayImpl(value));
        }

        public bool ImageMask
        {
            get => GetBool(PdfName.ImageMask);
            set => Set(PdfName.ImageMask, value);
        }

    }
}
/*
  Copyright 2007-2011 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Inline image object [PDF:1.6:4.8.6].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class GraphicsInlineImage : CompositeObject, IImageObject, IBoxed
    {
        public static readonly string BeginOperatorKeyword = BeginInlineImage.OperatorKeyword;
        public static readonly string EndOperatorKeyword = EndInlineImage.OperatorKeyword;

        private static readonly string DataOperatorKeyword = "ID";
        private SKImage image;
        private IContentContext Context;

        public GraphicsInlineImage(InlineImageHeader header, InlineImageBody body)
        {
            objects.Add(header);
            objects.Add(body);
        }

        /// <summary>Gets the image body.</summary>
        public Operation Body => (Operation)Contents[1];

        /// <summary>Gets the image header.</summary>
        public override Operation Header => (Operation)Contents[0];

        IDictionary<PdfName, PdfDirectObject> IImageObject.Header => ImageHeader;

        public InlineImageHeader ImageHeader => (InlineImageHeader)Header;

        public InlineImageBody ImageBody => (InlineImageBody)Body;

        /// <summary>Gets the image size.</summary>
        public SKSize Size => new SKSize(ImageHeader.Width, ImageHeader.Height);

        public SKMatrix Matrix => SKMatrix.CreateScale(1F / ImageHeader.Width, -1F / ImageHeader.Height);

        public IImageObject SMask => null;

        public PdfDirectObject Mask => null;

        public bool ImageMask => bool.TryParse(ImageHeader.ImageMask, out var isMask) ? isMask : false;

        public IInputStream Data => ImageBody.Value;

        public float[] Decode => ImageHeader.Decode;

        public PdfDirectObject Filter => ImageHeader.Filter;

        public PdfDirectObject Parameters => ImageHeader.DecodeParms;

        public int BitsPerComponent => ImageHeader.BitsPerComponent;

        public ColorSpace ColorSpace
        {
            get => ImageHeader.ColorSpace ?? Context.Resources.ColorSpaces[(PdfName)ImageHeader.ColorSpaceObject];
        }

        public PdfArray Matte => null;

        public SKRect GetBox(GraphicsState state)
        {
            var size = Size;
            var ctm = state.Ctm;
            return SKRect.Create(
              ctm.TransX,
              size.Height - ctm.TransY,
              size.Width * ctm.ScaleX,
              size.Height * Math.Abs(ctm.ScaleY));
        }

        public override void Scan(GraphicsState state)
        {
            Context = state.Scanner.Context;
            var size = Size;            
            if (state.Scanner?.Canvas is SKCanvas canvas)
            {
                var image = Load(state);
                if (image != null)
                {
                    //canvas.Save();
                    var matrix = canvas.TotalMatrix;

                    matrix = matrix.PreConcat(Matrix);
                    matrix = matrix.PreConcat(SKMatrix.CreateTranslation(0, -size.Height));
                    canvas.SetMatrix(matrix);

                    if (ImageMask)
                    {
                        using (var paint = new SKPaint())
                        {
                            paint.Color = state.GetFillColor() ?? SKColors.Black;
                            canvas.DrawImage(image, 0, 0, paint);
                        }
                    }
                    else
                    {
                        using (var paint = state.CreateFillPaint())
                        {
                            canvas.DrawImage(image, 0, 0, paint);
                        }
                    }
                    //canvas.Restore();
                }
            }
        }

        public SKImage Load(GraphicsState state)
        {
            if (image != null)
                return image;
            
            image = BitmapLoader.Load(this, state);
            state.Scanner.Contents.Document.Cache[Header.Operands] = image;
            return image;
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            stream.Write(BeginOperatorKeyword); stream.Write("\n");
            Header.WriteTo(stream, context);
            stream.Write(DataOperatorKeyword); stream.Write("\n");
            Body.WriteTo(stream, context); stream.Write("\n");
            stream.Write(EndOperatorKeyword);
        }

    }
}
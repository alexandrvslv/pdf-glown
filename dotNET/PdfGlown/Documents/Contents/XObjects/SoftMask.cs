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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Functions;
using PdfGlown.Objects;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.XObjects
{
    [Pdf(VersionEnum.PDF14)]
    public class SoftMask : PdfDictionary
    {
        private Function function;

        public static SoftMask WrapSoftMask(PdfDirectObject baseObject)
        {
            if (baseObject is PdfName name)
            {
                if(name.Equals(PdfName.None))
                    return null;
            }
            return (SoftMask)baseObject.Resolve(PdfName.SMask);
        }

        public SoftMask(PdfDocument context) 
            : base(context, new Dictionary<PdfName, PdfDirectObject>
            {
                { PdfName.Type, PdfName.Mask }
            })
        { }

        internal SoftMask(Dictionary<PdfName, PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        public PdfName SubType
        {
            get => Get<PdfName>(PdfName.S);
            set => SetSimple(PdfName.S, value);
        }

        public FormXObject Group
        {
            get => Get<FormXObject>(PdfName.G);
            set => Set(PdfName.G, value);
        }

        public PdfArray BackColor
        {
            get => Get<PdfArray>(PdfName.BC);
            set => Set(PdfName.BC, value);
        }

        public Function Function
        {
            get => function ??= Function.Wrap(Get(PdfName.TR));
            set => Set(PdfName.TR, function = value);
        }

        public SKMatrix InitialMatrix { get; internal set; }

        public void Apply(SKPaint paint, ContentScanner scanner)// , SKCanvas canvas, SKPicture picture)
        {
            var mObject = Group;
            var tGroup = mObject.Group;
            
            var temp = scanner.State.SMask;
            scanner.State.SMask = null;

            if (PdfName.Luminosity.Equals(SubType))
            {
                var softMaskPicture = mObject.Render(scanner, GetBackgroundColor());
                var pictureShader = SKShader.CreatePicture(softMaskPicture);
                paint.Shader = paint.Shader == null
                        ? pictureShader
                        : SKShader.CreateCompose(paint.Shader, pictureShader);
                paint.BlendMode = tGroup.Knockout ? SKBlendMode.SrcOver : tGroup.Isolated ? SKBlendMode.SrcATop : SKBlendMode.Luminosity;

            }
            else // alpha
            {
                var softMaskPicture = mObject.Render(scanner, SKColors.Transparent);//
                //var box = softMaskPicture.CullRect;
                //using var bitmap = new SKBitmap((int)box.Width, (int)box.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                //using var bitmapCanvas = new SKCanvas(bitmap);
                //bitmapCanvas.DrawPicture(softMaskPicture);
                var pictureShader = SKShader.CreatePicture(softMaskPicture);
                paint.Shader = paint.Shader == null
                        ? pictureShader
                        : SKShader.CreateCompose(paint.Shader, pictureShader);
                paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
                    0, 0, 0, 1, 0,
                    0, 0, 0, 1, 0,
                    0, 0, 0, 1, 0,
                    0, 0, 0, 1, 0
                });
                paint.BlendMode = tGroup.Knockout ? SKBlendMode.SrcOver : tGroup.Isolated ? SKBlendMode.Overlay : SKBlendMode.Overlay;
            }
            scanner.State.SMask = temp;
        }

        private SKColor? GetBackgroundColor()
        {
            SKColor? skColor = null;
            if (BackColor is PdfArray array
                && Group?.Group?.ColorSpace is ColorSpace colorSpace
                && colorSpace.ComponentCount <= array.Count
                && colorSpace.GetColor(array, Group) is Color color)
            {
                skColor = colorSpace.GetSKColor(color);
            }

            return skColor;
        }
    }
}
/*
  Copyright 2006-2011 Stefano Chizzolini. http://www.pdfclown.org

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
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>ICC-based color space [PDF:1.6:4.5.4].</summary>
    // TODO:IMPL improve profile support (see ICC.1:2003-09 spec)!!!
    [Pdf(VersionEnum.PDF13)]
    public sealed class ICCBasedColorSpace : ColorSpace
    {
        private SKColorSpace skColorSpace;
        private SKMatrix44 xyzD50 = SKMatrix44.CreateIdentity();
        private SKColorSpaceTransferFn transfer;
        private ColorSpace alternate;
        //TODO:IMPL new element constructor!

        internal ICCBasedColorSpace(List<PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override int ComponentCount => N;

        public override IColor DefaultColor => AlternateColorSpace.DefaultColor;

        public override IColor GetColor(PdfArray components, IContentContext context) => AlternateColorSpace.GetColor(components, context);

        public override bool IsSpaceColor(IColor color) => AlternateColorSpace.IsSpaceColor(color);

        public override SKColor GetSKColor(IColor color, float? alpha = null) => AlternateColorSpace.GetSKColor(color, alpha);

        public override SKColor GetSKColor(ReadOnlySpan<float> components, float? alpha = null) => AlternateColorSpace.GetSKColor(components, alpha);

        public PdfStream Profile => Get<PdfStream>(1);

        public PdfName Alternate => Profile?.Get<PdfName>(PdfName.Alternate);

        public ColorSpace AlternateColorSpace
        {
            get
            {
                if (alternate == null)
                {
                    var obj = Alternate;
                    if (obj == null)
                    {
                        switch (N)
                        {
                            case 1: obj = PdfName.DeviceGray; break;
                            case 3: obj = PdfName.DeviceRGB; break;
                            case 4: obj = PdfName.DeviceCMYK; break;
                            default: obj = PdfName.DeviceN; break;
                        }
                    }
                    alternate = ColorSpace.Wrap(obj);
                }
                return alternate;
            }
        }
        public int N => Profile?.GetInt(PdfName.N) ?? 0;

        public SKColorSpace GetSKColorSpace()
        {
            if (skColorSpace == null)
            {
                skColorSpace = SKColorSpace.CreateIcc(Profile.GetInputStream().AsSpan());
                if (skColorSpace != null)
                {
                    skColorSpace.GetNumericalTransferFunction(out var spaceTransfer);
                    transfer = spaceTransfer.Invert();
                }
            }
            return skColorSpace;
        }

        private SKPoint3 XYZtoRGB(float x, float y, float z)
        {
            return new SKPoint3(x, y, z);
            //var result = xyzD50.MapScalars(x, y, z, 1);
            ////float r = result[0], g = result[1], b = result[2];
            //float r = transfer.Transform(Math.Min(1F, Math.Max(0F, result[0]))), 
            //    g = transfer.Transform(Math.Min(1F, Math.Max(0F, result[1]))), 
            //    b = transfer.Transform(Math.Min(1F, Math.Max(0F, result[2])));

            //return new SKPoint3(r, g, b);
        }
    }
}
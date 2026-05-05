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

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>Device Gray color space [PDF:1.6:4.5.3].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class GrayColorSpace : DeviceColorSpace
    {
        // NOTE: It may be specified directly (i.e. without being defined in the ColorSpace subdictionary
        // of the contextual resource dictionary) [PDF:1.6:4.5.7].
        public static readonly GrayColorSpace Default = new(PdfName.DeviceGray);

        public static readonly GrayColorSpace DefaultShort = new(PdfName.G);

        public GrayColorSpace(PdfName baseObject)
            : base(baseObject)
        { }

        public override int ComponentCount => 1;

        public override IColor DefaultColor => GrayColor.Default;

        public override IColor GetColor(PdfArray components, IContentContext context = null)
            => components == null ? DefaultColor : new GrayColor(this, components);

        public override bool IsSpaceColor(IColor color) => color is GrayColor;

        public override SKColor GetSKColor(IColor color, float? alpha = null)
        {
            var spaceColor = (GrayColor)color;
            var g = (byte)Math.Round(spaceColor.G * 255);
            var skColor = new SKColor(g, g, g);
            if (alpha != null)
            {
                skColor = skColor.WithAlpha((byte)(alpha.Value * 255));
            }
            return skColor;
        }

        public override SKColor GetSKColor(ReadOnlySpan<float> components, float? alpha = null)
        {
            var g = (byte)Math.Round(components[0] * 255);
            var skColor = new SKColor(g, g, g);
            if (alpha != null)
            {
                skColor = skColor.WithAlpha((byte)(alpha.Value * 255));
            }
            return skColor;
        }
    }
}
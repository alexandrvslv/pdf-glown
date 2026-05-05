/*
  Copyright 2010-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>Indexed color space [PDF:1.6:4.5.5].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class IndexedColorSpace : SpecialColorSpace
    {
        private Dictionary<int, IColor> baseColors = new();
        private Dictionary<int, SKColor> baseSKColors = new();
        private byte[] baseComponentValues;
        private ColorSpace baseSpace;
        private int? componentCount;
        private IndexedColor defaultColor;

        //TODO:IMPL new element constructor!

        internal IndexedColorSpace(List<PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the base color space in which the values in the color table
        /// are to be interpreted.</summary>
        public ColorSpace BaseSpace => baseSpace ??= ColorSpace.Wrap(Get(1));

        public int BaseSpaceComponentCount => componentCount ??= BaseSpace.ComponentCount;

        public override int ComponentCount => 1;

        public override IColor DefaultColor => defaultColor ??= new IndexedColor(this, 0);

        /// <summary>Gets the color corresponding to the specified table index resolved according to
        /// the<see cref="BaseSpace"> base space</see>.<summary>
        public IColor GetBaseColor(IndexedColor color)
        {
            int colorIndex = color.Index;
            if (!baseColors.TryGetValue(colorIndex, out var baseColor))
            {
                ColorSpace baseSpace = BaseSpace;
                int componentCount = BaseSpaceComponentCount;
                var components = new PdfArrayImpl(componentCount);
                {
                    int componentValueIndex = colorIndex * componentCount;
                    byte[] baseComponentValues = BaseComponentValues;
                    for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
                    {
                        var byteValue = componentValueIndex < baseComponentValues.Length
                            ? baseComponentValues[componentValueIndex]
                            : 0;
                        var value = ((int)byteValue & 0xff) / 255d;
                        components.Set(componentIndex, value);
                        componentValueIndex++;
                    }
                }
                baseColor = baseColors[colorIndex] = baseSpace.GetColor(components, null);
            }
            return baseColor;
        }

        public SKColor GetBaseSKColor(ReadOnlySpan<float> color)
        {
            int colorIndex = (int)color[0];
            if (!baseSKColors.TryGetValue(colorIndex, out var baseColor))
            {
                ColorSpace baseSpace = BaseSpace;
                int componentCount = BaseSpaceComponentCount;
                Span<float> components = stackalloc float[componentCount];
                {
                    int componentValueIndex = colorIndex * componentCount;
                    byte[] baseComponentValues = BaseComponentValues;
                    for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
                    {
                        var byteValue = componentValueIndex < baseComponentValues.Length
                            ? baseComponentValues[componentValueIndex]
                            : 0;
                        var value = (byteValue & 0xff) / 255F;
                        components[componentIndex] = value;
                        componentValueIndex++;
                    }
                }
                baseColor = baseSKColors[colorIndex] = baseSpace.GetSKColor(components, null);
            }
            return baseColor;
        }

        public override IColor GetColor(PdfArray components, IContentContext context)
            => components == null ? DefaultColor : new IndexedColor(this, components);

        public override bool IsSpaceColor(IColor color) => color is IndexedColor;

        public override SKColor GetSKColor(IColor color, float? alpha = null) => BaseSpace.GetSKColor(GetBaseColor((IndexedColor)color), alpha);

        public override SKColor GetSKColor(ReadOnlySpan<float> components, float? alpha = null)
        {
            var color = GetBaseSKColor(components);
            if (alpha != null)
            {
                color = color.WithAlpha((byte)(alpha * 255));
            }
            return color;
        }

        public override SKPaint GetPaint(IColor color, SKPaintStyle paintStyle, float? alpha = null, GraphicsState state = null)
        {
            return BaseSpace.GetPaint(GetBaseColor((IndexedColor)color), paintStyle, alpha, state);
        }

        /// <summary>Gets the color table.</summary>
        private byte[] BaseComponentValues
        {
            get
            {
                if (baseComponentValues == null)
                {
                    var value = Get<PdfDirectObject>(3);
                    if (value is IDataWrapper wrapper)
                    {
                        baseComponentValues = wrapper.GetArrayBuffer();
                    }
                    else if (value is PdfStream stream)
                    {
                        baseComponentValues = stream.GetInputStream().GetArrayBuffer();
                    }
                }
                return baseComponentValues;
            }
        }
    }
}
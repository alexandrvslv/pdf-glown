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

using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>Color space [PDF:1.6:4.5].</summary>
    public abstract class ColorSpace : PdfArray
    {
        private static readonly Dictionary<PdfName, Func<List<PdfDirectObject>, ColorSpace>> factory = new()
        {
            { PdfName.DeviceRGB, static (dict) => RGBColorSpace.Default },
            { PdfName.RGB, static (dict) => RGBColorSpace.DefaultShort },
            { PdfName.DeviceCMYK, static (dict) => CMYKColorSpace.Default },
            { PdfName.CMYK, static (dict) => CMYKColorSpace.DefaultShort },
            { PdfName.DeviceGray, static (dict) => GrayColorSpace.Default },
            { PdfName.G, static (dict) => GrayColorSpace.DefaultShort },
            { PdfName.CalRGB, static (dict) => new CalRGBColorSpace(dict) },
            { PdfName.CalGray, static (dict) => new CalGrayColorSpace(dict) },
            { PdfName.ICCBased, static (dict) => new ICCBasedColorSpace(dict) },
            { PdfName.Lab, static (dict) => new LabColorSpace(dict) },
            { PdfName.DeviceN, static (dict) => new NColorSpace(dict) },
            { PdfName.Indexed, static (dict) => new IndexedColorSpace(dict) },
            { PdfName.I, static (dict) => new IndexedColorSpace(dict) },
            { PdfName.Pattern, static (dict) => new PatternColorSpace(dict) },
            { PdfName.Separation, static (dict) => new SeparationColorSpace(dict) },
        };

        private static readonly Dictionary<PdfName, ColorSpace> defaultSpaces = new()
        {
            { PdfName.DeviceRGB, RGBColorSpace.Default },
            { PdfName.RGB, RGBColorSpace.DefaultShort },
            { PdfName.DeviceCMYK, CMYKColorSpace.Default },
            { PdfName.CMYK, CMYKColorSpace.DefaultShort },
            { PdfName.DeviceGray, GrayColorSpace.Default },
            { PdfName.G, GrayColorSpace.DefaultShort },
            { PdfName.Pattern, PatternColorSpace.Default },
        };

        /// <summary>Wraps the specified color space base object into a color space object.</summary>
        /// <param name="baseObject">Base object of a color space object.</param>
        /// <returns>Color space object corresponding to the base object.</returns>
        public static ColorSpace Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            // Get the data object corresponding to the color space!
            return baseObject.Resolve(PdfName.ColorSpace) is PdfDirectObject resolved
                    ? resolved is PdfName relovedName 
                        ? GetDefault(relovedName)
                        : resolved as ColorSpace
                    : null;
        }

        public static ColorSpace GetDefault(PdfName name)
        {
            return defaultSpaces.TryGetValue(name, out var deviceSpace) ? deviceSpace : null;
        }

        internal static bool IsMatch(List<PdfDirectObject> list)
        {
            return list.Count > 0
                && list[0] is PdfName name
                && factory.ContainsKey(name);
        }

        internal static ColorSpace Create(List<PdfDirectObject> array)
        {
            // NOTE: A color space is defined by an array object whose first element
            // is a name object identifying the color space family [PDF:1.6:4.5.2].
            // For families that do not require parameters, the color space CAN be
            // specified simply by the family name itself instead of an array.
            var name = array.Get<PdfName>(0);
            if (factory.TryGetValue(name, out var func))
                return func(array);
            return null;
        }

        protected ColorSpace(PdfDocument context, List<PdfDirectObject> baseDataObject)
            : base(context, baseDataObject)
        { }

        public ColorSpace(List<PdfDirectObject> dictionary)
            : base(dictionary)
        { }

        /// <summary>Gets the number of components used to represent a color value.</summary>
        public abstract int ComponentCount
        {
            get;
        }

        /// <summary>Gets the initial color value within this color space.</summary>
        public abstract IColor DefaultColor
        {
            get;
        }

        /// <summary>Gets the rendering representation of the specified color value.</summary>
        /// <param name="color">Color value to convert into an equivalent rendering representation.</param>
        public virtual SKPaint GetPaint(IColor color, SKPaintStyle paintStyle, float? alpha = null, GraphicsState graphicsState = null)
        {
            var skColor = GetSKColor(color, alpha);
            return new SKPaint
            {
                Color = skColor,
                Style = paintStyle,
                IsAntialias = true,
                BlendMode = SKBlendMode.SrcOver
            };
        }

        /// <summary>Gets the color value corresponding to the specified components
        /// interpreted according to this color space [PDF:1.6:4.5.1].</summary>
        /// <param name="components">Color components.</param>
        /// <param name="context">Content context.</param>
        public abstract IColor GetColor(PdfArray components, IContentContext context);

        public SKColor GetSKColor(PdfArray components, IContentContext context, float? alpha = null) => GetSKColor(GetColor(components, context), alpha);

        public abstract SKColor GetSKColor(IColor color, float? alpha = null);

        public abstract SKColor GetSKColor(ReadOnlySpan<float> components, float? alpha = null);

        public abstract bool IsSpaceColor(IColor value);

        protected static byte ToByte(double v)
        {
            return v > 255 ? (byte)255 : v < 0 ? (byte)0 : (byte)v;
        }
    }
}
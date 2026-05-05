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
    /// <summary>Device Cyan-Magenta-Yellow-Key color space [PDF:1.6:4.5.3].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class CMYKColorSpace : DeviceColorSpace
    {
        // NOTE: It may be specified directly (i.e. without being defined in the ColorSpace subdictionary
        // of the contextual resource dictionary) [PDF:1.6:4.5.7].
        public static readonly CMYKColorSpace Default = new(PdfName.DeviceCMYK);

        public static readonly CMYKColorSpace DefaultShort = new(PdfName.CMYK);

        internal CMYKColorSpace(PdfName baseObject)
            : base(baseObject)
        { }

        public override int ComponentCount => 4;

        public override Color DefaultColor => CMYKColor.Default;

        public override IColor GetColor(PdfArray components, IContentContext context = null)
            => components == null ? DefaultColor : new CMYKColor(this, components);

        public override bool IsSpaceColor(IColor color) => color is CMYKColor;

        public override SKColor GetSKColor(IColor color, float? alpha = null)
        {
            var cmykColor = (CMYKColor)color;
            return Calculate(cmykColor.C, cmykColor.M, cmykColor.Y, cmykColor.K, alpha);
        }

        public override SKColor GetSKColor(ReadOnlySpan<float> components, float? alpha = null)
        {
            return Calculate(components[0], components[1], components[2], components[3], alpha);
        }

        // Mozilla Pdf.js
        // The coefficients below was found using numerical analysis: the method of
        // steepest descent for the sum((f_i - color_value_i)^2) for r/g/b colors,
        // where color_value is the tabular value from the table of sampled RGB colors
        // from CMYK US Web Coated (SWOP) colorspace, and f_i is the corresponding
        // CMYK color conversion using the estimation below:
        //   f(A, B,.. N) = Acc+Bcm+Ccy+Dck+c+Fmm+Gmy+Hmk+Im+Jyy+Kyk+Ly+Mkk+Nk+255
        SKColor Calculate(double c, double m, double y, double k, float? alpha = null)
        {
            var r =
              255 +
              c *
                (-4.387332384609988 * c +
                  54.48615194189176 * m +
                  18.82290502165302 * y +
                  212.25662451639585 * k +
                  -285.2331026137004) +
              m *
                (1.7149763477362134 * m -
                  5.6096736904047315 * y +
                  -17.873870861415444 * k -
                  5.497006427196366) +
              y *
                (-2.5217340131683033 * y - 21.248923337353073 * k + 17.5119270841813) +
              k * (-21.86122147463605 * k - 189.48180835922747);

            var g =
              255 +
              c *
                (8.841041422036149 * c +
                  60.118027045597366 * m +
                  6.871425592049007 * y +
                  31.159100130055922 * k +
                  -79.2970844816548) +
              m *
                (-15.310361306967817 * m +
                  17.575251261109482 * y +
                  131.35250912493976 * k -
                  190.9453302588951) +
              y * (4.444339102852739 * y + 9.8632861493405 * k - 24.86741582555878) +
              k * (-20.737325471181034 * k - 187.80453709719578);

            var b =
              255 +
              c *
                (0.8842522430003296 * c +
                  8.078677503112928 * m +
                  30.89978309703729 * y -
                  0.23883238689178934 * k +
                  -14.183576799673286) +
              m *
                (10.49593273432072 * m +
                  63.02378494754052 * y +
                  50.606957656360734 * k -
                  112.23884253719248) +
              y *
                (0.03296041114873217 * y +
                  115.60384449646641 * k +
                  -193.58209356861505) +
              k * (-22.33816807309886 * k - 180.12613974708367);

            var skColor = new SKColor(ToByte(r), ToByte(g), ToByte(b));
            if (alpha != null)
            {
                skColor = skColor.WithAlpha((byte)(alpha.Value * 255));
            }
            return skColor;
        }

    }
}
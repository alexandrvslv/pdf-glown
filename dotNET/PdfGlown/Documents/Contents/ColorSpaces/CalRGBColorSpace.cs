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
/**
 * Mozilla Pdf.js
 * CalRGBCS: Based on "PDF Reference, Sixth Ed", p.247
 *
 * The default color is `new Float32Array([0, 0, 0])`.
 */

using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>CIE-based ABC single-transformation-stage color space, where A, B, and C represent
    /// calibrated red, green and blue color values[PDF:1.6:4.5.4].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class CalRGBColorSpace : CalColorSpace
    {
        // See http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html for these
        // matrices.
        // prettier-ignore
        public static readonly float[] BRADFORD_SCALE_MATRIX = new float[]{
          0.8951F, 0.2664F, -0.1614F,
          -0.7502F, 1.7135F, 0.0367F,
          0.0389F, -0.0685F, 1.0296F };

        // prettier-ignore
        public static readonly float[] BRADFORD_SCALE_INVERSE_MATRIX = new float[]{
          0.9869929F, -0.1470543F, 0.1599627F,
          0.4323053F, 0.5183603F, 0.0492912F,
          -0.0085287F, 0.0400428F, 0.9684867F };

        // See http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html.
        // prettier-ignore
        public static readonly float[] SRGB_D65_XYZ_TO_RGB_MATRIX = new float[]{
          3.2404542F, -1.5371385F, -0.4985314F,
          -0.9692660F, 1.8760108F, 0.0415560F,
          0.0556434F, -0.2040259F, 1.0572252F };

        public static readonly float[] FLAT_WHITEPOINT_MATRIX = new float[] { 1, 1, 1 };

        public static readonly float DECODE_L_CONSTANT = (float)(Math.Pow((8 + 16) / 116, 3) / 8.0F);

        static void MatrixProduct(float[] a, float[] b, float[] result)
        {
            result[0] = a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
            result[1] = a[3] * b[0] + a[4] * b[1] + a[5] * b[2];
            result[2] = a[6] * b[0] + a[7] * b[1] + a[8] * b[2];
        }

        static void ConvertToFlat(float[] sourceWhitePoint, float[] LMS, float[] result)
        {
            result[0] = (LMS[0] * 1) / sourceWhitePoint[0];
            result[1] = (LMS[1] * 1) / sourceWhitePoint[1];
            result[2] = (LMS[2] * 1) / sourceWhitePoint[2];
        }

        static void ConvertToD65(float[] sourceWhitePoint, float[] LMS, float[] result)
        {
            const float D65X = 0.95047F;
            const float D65Y = 1F;
            const float D65Z = 1.08883F;

            result[0] = (LMS[0] * D65X) / sourceWhitePoint[0];
            result[1] = (LMS[1] * D65Y) / sourceWhitePoint[1];
            result[2] = (LMS[2] * D65Z) / sourceWhitePoint[2];
        }

        static float SRGBTransferFunction(float color)
        {
            // See http://en.wikipedia.org/wiki/SRGB.
            if (color <= 0.0031308)
            {
                return AdjustToRange(0, 1, 12.92F * color);
            }
            return AdjustToRange(0, 1, (float)(Math.Pow((1 + 0.055) * color, (1 / 2.4)) - 0.055F));
        }

        static float AdjustToRange(float min, float max, float value)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        static float DecodeL(float L)
        {
            if (L < 0)
            {
                return -DecodeL(-L);
            }
            if (L > 8.0)
            {
                return (float)Math.Pow((L + 16) / 116, 3);
            }
            return L * DECODE_L_CONSTANT;
        }

        static void CompensateBlackPoint(float[] sourceBlackPoint, float[] XYZ_Flat, float[] result)
        {
            // In case the blackPoint is already the default blackPoint then there is
            // no need to do compensation.
            if (
              sourceBlackPoint[0] == 0 &&
              sourceBlackPoint[1] == 0 &&
              sourceBlackPoint[2] == 0
            )
            {
                result[0] = XYZ_Flat[0];
                result[1] = XYZ_Flat[1];
                result[2] = XYZ_Flat[2];
                return;
            }

            // For the blackPoint calculation details, please see
            // http://www.adobe.com/content/dam/Adobe/en/devnet/photoshop/sdk/
            // AdobeBPC.pdf.
            // The destination blackPoint is the default blackPoint [0, 0, 0].
            var zeroDecodeL = DecodeL(0);

            var X_DST = zeroDecodeL;
            var X_SRC = DecodeL(sourceBlackPoint[0]);

            var Y_DST = zeroDecodeL;
            var Y_SRC = DecodeL(sourceBlackPoint[1]);

            var Z_DST = zeroDecodeL;
            var Z_SRC = DecodeL(sourceBlackPoint[2]);

            var X_Scale = (1 - X_DST) / (1 - X_SRC);
            var X_Offset = 1 - X_Scale;

            var Y_Scale = (1 - Y_DST) / (1 - Y_SRC);
            var Y_Offset = 1 - Y_Scale;

            var Z_Scale = (1 - Z_DST) / (1 - Z_SRC);
            var Z_Offset = 1 - Z_Scale;

            result[0] = XYZ_Flat[0] * X_Scale + X_Offset;
            result[1] = XYZ_Flat[1] * Y_Scale + Y_Offset;
            result[2] = XYZ_Flat[2] * Z_Scale + Z_Offset;
        }

        static void NormalizeWhitePointToFlat(float[] sourceWhitePoint, float[] XYZ_In, float[] result)
        {
            // In case the whitePoint is already flat then there is no need to do
            // normalization.
            if (sourceWhitePoint[0] == 1 && sourceWhitePoint[2] == 1)
            {
                result[0] = XYZ_In[0];
                result[1] = XYZ_In[1];
                result[2] = XYZ_In[2];
                return;
            }

            var LMS = result;
            MatrixProduct(BRADFORD_SCALE_MATRIX, XYZ_In, LMS);

            var LMS_Flat = new float[3];
            ConvertToFlat(sourceWhitePoint, LMS, LMS_Flat);

            MatrixProduct(BRADFORD_SCALE_INVERSE_MATRIX, LMS_Flat, result);
        }

        static void NormalizeWhitePointToD65(float[] sourceWhitePoint, float[] XYZ_In, float[] result)
        {
            var LMS = result;
            MatrixProduct(BRADFORD_SCALE_MATRIX, XYZ_In, LMS);

            var LMS_D65 = new float[3];
            ConvertToD65(sourceWhitePoint, LMS, LMS_D65);

            MatrixProduct(BRADFORD_SCALE_INVERSE_MATRIX, LMS_D65, result);
        }

        private float[] gamma;
        private SKMatrix? matrix;
        private float GR;
        private float GG;
        private float GB;
        private float MXA;
        private float MYA;
        private float MZA;
        private float MXB;
        private float MYB;
        private float MZB;
        private float MXC;
        private float MYC;
        private float MZC;
        private CalRGBColor defaultColor;

        //TODO:IMPL new element constructor!

        internal CalRGBColorSpace(List<PdfDirectObject> baseObject)
            : base(baseObject)
        {
            var gamma = Gamma;
            var matrix = Matrix;
            // Translate arguments to spec variables.
            this.GR = gamma[0];
            this.GG = gamma[1];
            this.GB = gamma[2];

            this.MXA = matrix.ScaleX;
            this.MYA = matrix.SkewY;
            this.MZA = matrix.SkewX;
            this.MXB = matrix.ScaleY;
            this.MYB = matrix.TransX;
            this.MZB = matrix.TransY;
            this.MXC = matrix.Persp0;
            this.MYC = matrix.Persp1;
            this.MZC = matrix.Persp2;
        }

        public override int ComponentCount => 3;

        public override Color DefaultColor => defaultColor ??= new CalRGBColor(this, 0, 0, 0);

        public override float[] Gamma
        {
            get
            {
                return gamma ??= Dictionary.Get<PdfArray>(PdfName.Gamma) is PdfArray array
                  ? new float[] { array.GetFloat(0), array.GetFloat(1), array.GetFloat(2) }
                  : new float[] { 1, 1, 1 };
            }
        }

        public SKMatrix Matrix
        {
            get
            {
                return matrix ??= Dictionary.Get<PdfArray>(PdfName.Matrix) is PdfArray array
                    ? new SKMatrix
                    {
                        ScaleX = array.GetFloat(0),
                        SkewY = array.GetFloat(1),
                        SkewX = array.GetFloat(2),
                        ScaleY = array.GetFloat(3),
                        TransX = array.GetFloat(4),
                        TransY = array.GetFloat(5),
                        Persp0 = array.GetFloat(6),
                        Persp1 = array.GetFloat(7),
                        Persp2 = array.GetFloat(8, 1F)
                    }
                    : SKMatrix.Identity;
            }
            set => Dictionary[PdfName.Matrix] =
                 new PdfArrayImpl(9)
                 {
                    value.ScaleX,
                    value.SkewY,
                    value.SkewX,
                    value.ScaleY,
                    value.TransX,
                    value.TransY,
                    value.Persp0,
                    value.Persp1,
                    value.Persp2
                 };
        }

        public override IColor GetColor(PdfArray components, IContentContext context)
            => components == null ? DefaultColor : new CalRGBColor(this, components);

        public override bool IsSpaceColor(IColor color) => color is CalRGBColor;

        public override SKColor GetSKColor(IColor color, float? alpha = null)
        {
            var calColor = (CalRGBColor)color;
            // FIXME: temporary hack
            return Calculate(calColor.R, calColor.G, calColor.B, alpha);
        }

        public override SKColor GetSKColor(ReadOnlySpan<float> components, float? alpha = null)
        {
            return Calculate(components[0], components[1], components[2], alpha);
        }

        public SKColor Calculate(float a, float b, float c, float? alpha = null)
        {
            // A, B and C represent a red, green and blue components of a calibrated
            // rgb space.
            var A = AdjustToRange(0, 1, a);
            var B = AdjustToRange(0, 1, b);
            var C = AdjustToRange(0, 1, c);

            // A <---> AGR in the spec
            // B <---> BGG in the spec
            // C <---> CGB in the spec
            var AGR = Math.Pow(A, GR);
            var BGG = Math.Pow(B, GG);
            var CGB = Math.Pow(C, GB);

            // Computes intermediate variables L, M, N as per spec.
            // To decode X, Y, Z values map L, M, N directly to them.
            var X = MXA * AGR + MXB * BGG + MXC * CGB;
            var Y = MYA * AGR + MYB * BGG + MYC * CGB;
            var Z = MZA * AGR + MZB * BGG + MZC * CGB;

            // The following calculations are based on this document:
            // http://www.adobe.com/content/dam/Adobe/en/devnet/photoshop/sdk/
            // AdobeBPC.pdf.
            var XYZ = new float[3];
            XYZ[0] = (float)X;
            XYZ[1] = (float)Y;
            XYZ[2] = (float)Z;
            var XYZ_Flat = new float[3];

            NormalizeWhitePointToFlat(WhitePoint, XYZ, XYZ_Flat);

            var XYZ_Black = new float[3];
            CompensateBlackPoint(BlackPoint, XYZ_Flat, XYZ_Black);

            var XYZ_D65 = new float[3];
            NormalizeWhitePointToD65(FLAT_WHITEPOINT_MATRIX, XYZ_Black, XYZ_D65);

            var SRGB = new float[3];
            MatrixProduct(SRGB_D65_XYZ_TO_RGB_MATRIX, XYZ_D65, SRGB);

            // Convert the values to rgb range [0, 255].
            var skColor = new SKColor(ToByte(SRGBTransferFunction(SRGB[0]) * 255),
                ToByte(SRGBTransferFunction(SRGB[1]) * 255),
                ToByte(SRGBTransferFunction(SRGB[2]) * 255));
            if (alpha != null)
            {
                skColor = skColor.WithAlpha((byte)(alpha * 255));
            }
            return skColor;
        }
    }
}
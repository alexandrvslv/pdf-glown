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

using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Functions
{
    /// <summary>Exponential interpolation of one input value and <code>n</code> output values
    /// [PDF:1.6:3.9.2].</summary>
    /// <remarks>Each input value <code>x</code> will return <code>n</code> values, given by <code>
    /// y[j] = C0[j] + x^N × (C1[j] − C0[j])</code>, for <code>0 ≤ j < n</code>, where <code>C0</code>
    /// and <code>C1</code> are the {@link #getBoundOutputValues() function results} when, respectively,
    /// <code>x = 0</code> and <code>x = 1</code>, and <code>N</code> is the {@link #getExponent()
    /// interpolation exponent}.</remarks>
    [Pdf(VersionEnum.PDF13)]
    public sealed class Type2Function : Function
    {
        private float[] _c0;
        private float[] _c1;
        //TODO:implement function creation!

        internal Type2Function(Dictionary<PdfName, PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        public override ReadOnlySpan<float> Calculate(ReadOnlySpan<float> inputs)
        {
            var c0 = C0;
            var c1 = C1;
            var outCount = Math.Min(c1.Length, c0.Length);
            var result = new float[outCount];
            var inputN = (float)Math.Pow(inputs[0], Exponent);
            for (int i = 0; i < outCount; i++)
            {
                var range = Ranges?[i];
                var exponenta = Exponential(c0[i], c1[i], inputN);
                result[i] = ClipToRange(exponenta, range?.Low ?? 0F, range?.High ?? 1F);
            }
            return result;
        }


        /// <summary>Gets the interpolation exponent.</summary>
        public float Exponent => GetFloat(PdfName.N);

        public float[] C0
        {
            get => _c0 ??= Get<PdfArray>(PdfName.C0)?.ToFloatArray() ?? new float[] { 0, 0 };
        }

        public float[] C1
        {
            get => _c1 ??= Get<PdfArray>(PdfName.C1)?.ToFloatArray() ?? new float[] { 1, 0 };
        }
    }
}
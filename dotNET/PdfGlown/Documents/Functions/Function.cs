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
using PdfGlown.Util.Collections;
using PdfGlown.Util.Math;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Functions
{
    /// <summary>Function [PDF:1.6:3.9].</summary>
    [Pdf(VersionEnum.PDF12)]
    public abstract class Function : PdfStream
    {
        /// <summary>Default intervals callback.</summary>

        private const int FunctionType0 = 0;
        private const int FunctionType2 = 2;
        private const int FunctionType3 = 3;
        private const int FunctionType4 = 4;
        private IList<Interval<float>> domains;
        private IList<Interval<float>> ranges;

        /// <summary>Wraps a function base object into a function object.</summary>
        /// <param name="baseObject">Function base object.</param>
        /// <returns>Function object associated to the base object.</returns>
        public static Function Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;
            if (baseObject is PdfName dataName
                && (PdfName.Identity.Equals(dataName)
                || PdfName.Default.Equals(dataName)))
            {
                return TypeIdentityFunction.Instance;
            }
            return (Function)baseObject.Resolve(PdfName.Function);
        }

        internal static Function Create(Dictionary<PdfName, PdfDirectObject> dictionary)
        {
            int functionType = dictionary.GetInt(PdfName.FunctionType);
            return functionType switch
            {
                FunctionType0 => new Type0Function(dictionary),
                FunctionType2 => new Type2Function(dictionary),
                FunctionType3 => new Type3Function(dictionary),
                FunctionType4 => new Type4Function(dictionary),
                _ => throw new NotSupportedException("Function type " + functionType + " unknown."),
            };
        }

        protected Function(PdfDocument context, Dictionary<PdfName, PdfDirectObject> baseDataObject)
            : base(context, baseDataObject)
        { }

        protected Function(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the result of the calculation applied by this function
        /// to the specified input values.</summary>
        /// <param name="inputs">Input values.</param>
        public abstract ReadOnlySpan<float> Calculate(ReadOnlySpan<float> inputs);

        /// <summary>Gets the result of the calculation applied by this function
        /// to the specified input values.</summary>
        /// <param name="inputs">Input values.</param>
        public IList<PdfDirectObject> Calculate(IList<PdfDirectObject> inputs)
        {
            var outputs = new List<PdfDirectObject>();
            {
                float[] inputValues = new float[inputs.Count];
                for (int index = 0, length = inputValues.Length; index < length; index++)
                { inputValues[index] = ((IPdfNumber)inputs[index]).FloatValue; }
                var outputValues = Calculate(inputValues);
                for (int index = 0, length = outputValues.Length; index < length; index++)
                { outputs.Add(PdfReal.Get(outputValues[index])); }
            }
            return outputs;
        }

        /// <summary>Gets the (inclusive) domains of the input values.</summary>
        /// <remarks>Input values outside the declared domains are clipped to the nearest boundary value.</remarks>
        public IList<Interval<float>> Domains => domains ??= GetIntervals<float>(PdfName.Domain, null);

        /// <summary>Gets the number of input values (parameters) of this function.</summary>
        public int InputCount => Domains?.Count ?? 1;

        /// <summary>Gets the number of output values (results) of this function.</summary>
        public int OutputCount => Ranges?.Count ?? 1;

        /// <summary>Gets the (inclusive) ranges of the output values.</summary>
        /// <remarks>Output values outside the declared ranges are clipped to the nearest boundary value;
        /// if this entry is absent, no clipping is done.</remarks>
        /// <returns><code>null</code> in case of unbounded ranges.</returns>
        public IList<Interval<float>> Ranges => ranges ??= GetIntervals<float>(PdfName.Range, null);

        /// <summary>Gets the intervals corresponding to the specified key.</summary>
        protected IList<Interval<T>> GetIntervals<T>(PdfName key, DefaultIntervalsCallback<T> defaultIntervalsCallback)
            where T : struct, IComparable<T>
        {
            return Get<PdfArray>(key).GetIntervals(defaultIntervalsCallback);
        }

        //https://stackoverflow.com/questions/12838007/c-sharp-linear-interpolation
        static public float Linear(float x, float x0, float x1, float y0, float y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * ((y1 - y0) / (x1 - x0));
        }

        public static float Exponential(float c0, float c1, float inputN) => c0 + inputN * (c1 - c0);

        /// <summary>Clip the given input value to the given range.</summary>
        /// <param name="x">the input value</param>
        /// <param name="rangeMin">the min value of the range</param>
        /// <param name="rangeMax">max value of the range</param>
        /// <returns>the clipped value</returns>
        protected static float ClipToRange(float x, float rangeMin, float rangeMax) => Math.Min(Math.Max(x, rangeMin), rangeMax);

        /// <summary>Clip the given input values to the ranges.</summary>
        /// <param name="inputValues">the input values</param>
        protected void ClipToRange(Span<float> inputValues) => ClipToRange(inputValues, Ranges);

        protected void ClipToDomain(Span<float> inputValues) => ClipToRange(inputValues, Domains);

        private static void ClipToRange(Span<float> inputValues, IList<Interval<float>> rangesArray)
        {
            if (rangesArray != null && rangesArray.Count > 0)
            {
                for (int i = 0; i < rangesArray.Count; i++)
                {
                    var range = rangesArray[i];
                    inputValues[i] = ClipToRange(inputValues[i], range.Low, range.High);
                }
            }
        }
    }
}
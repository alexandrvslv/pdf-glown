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
using PdfGlown.Util.Math;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Functions
{
    /// <summary>Sampled function using a sequence of sample values to provide an approximation for
    /// functions whose domains and ranges are bounded[PDF:1.6:3.9.1].</summary>
    /// <remarks>The samples are organized as an m-dimensional table in which each entry has n components.
    /// </remarks>
    [Pdf(VersionEnum.PDF12)]
    public sealed class Type0Function : Function
    {
        private IList<Interval<float>> decodes;
        private IList<Interval<int>> encodes;
        private int[] sizes;
        private int[,] samples;
        public enum InterpolationOrderEnum
        {
            /// Linear spline interpolation.
            Linear = 1,
            /// Cubic spline interpolation.
            Cubic = 3
        }

        //TODO:implement function creation!

        internal Type0Function(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override ReadOnlySpan<float> Calculate(ReadOnlySpan<float> inputs)
        {
            //Mozilla Pdf.js
            var domains = Domains;
            var ranges = Ranges;
            var decodes = Decodes;
            var encodes = Encodes;
            var sizes = Sizes;
            var samples = GetSamples();
            var sampleMax = (int)Math.Pow(2, BitsPerSample) - 1.0F;
            var n = ranges.Count;
            var m = domains.Count;

            var input = inputs.ToArray();
            Span<int> inputPrev = stackalloc int[m];
            Span<int> inputNext = stackalloc int[m];

            for (int i = 0; i < m; i++)
            {
                var domain = domains[i];
                var encode = encodes[i];
                var size = sizes[i];
                var e = ClipToRange(inputs[i], domain.Low, domain.High);
                e = Linear(e, domain.Low, domain.High, encode.Low, encode.High);
                input[i] = ClipToRange(e, 0, size - 1);
                inputPrev[i] = (int)Math.Floor(e);
                inputNext[i] = (int)Math.Ceiling(e);
            }

            var rinterpol = new Rinterpol(input, inputPrev, inputNext, stackalloc int[m]);
            var result = Rinterpolate(rinterpol);
            for (int j = 0; j < n; j++)
            {
                var range = ranges[j];
                var decode = decodes[j];

                var r = Linear(result[j], 0, sampleMax, decode.Low, decode.High);
                result[j] = ClipToRange(r, range.Low, range.High);
            }

            return result;
        }

        /// <summary>Gets the linear mapping of input values into the domain of the function's sample table.</summary>
        public IList<Interval<int>> Encodes => encodes ??= GetIntervals<int>(
                  PdfName.Encode,
                  delegate (IList<Interval<int>> intervals)
                  {
                      foreach (int sampleCount in Sizes)
                      { intervals.Add(new Interval<int>(0, sampleCount - 1)); }
                      return intervals;
                  });

        /// <summary>Gets the order of interpolation between samples.</summary>
        public InterpolationOrderEnum Order => (InterpolationOrderEnum)GetInt(PdfName.Order, 1);

        /// <summary>Gets the linear mapping of sample values into the ranges of the function's output values.</summary>
        public IList<Interval<float>> Decodes => decodes ??= GetIntervals<float>(PdfName.Decode, (n) => Ranges);

        /// <summary>Gets the number of bits used to represent each sample.</summary>
        public int BitsPerSample => GetInt(PdfName.BitsPerSample);

        /// <summary>Gets the number of samples in each input dimension of the sample table.</summary>
        public int[] Sizes
        {
            get => sizes ??= Get<PdfArray>(PdfName.Size).ToIntArray();
        }

        public int[,] GetSamples()
        {
            if (samples == null)
            {
                var size = Sizes;
                var arraySize = 1;
                var inputCount = InputCount;
                var outputCount = OutputCount;
                for (int i = 0; i < inputCount; i++)
                {
                    arraySize *= size[i];
                }
                samples = new int[arraySize, outputCount];
                var bps = BitsPerSample;
                int index = 0;
                var stream = this;
                var buffer = stream.GetInputStream();
                for (int i = 0; i < arraySize; i++)
                {
                    for (int k = 0; k < outputCount; k++)
                    {
                        samples[index, k] = (int)buffer.ReadBits(bps);
                    }
                    index++;
                }
            }
            return samples;
        }



        /// <summary>calculate the interpolation.</summary>
        /// <param name="rinterpol"></param>
        /// <returns>interpolated result sample</returns>
        float[] Rinterpolate(Rinterpol rinterpol)
        {
            return Rinterpolate(rinterpol, 0);
        }

        /// <summary>Do a linear interpolation if the two coordinates can be known, or
        /// call itself recursively twice.</summary>
        /// <param name="rinterpol">coord partially set coordinate (not set from step
        /// upwards); gets fully filled in the last call("leaf"), where it is
        /// used to get the correct sample</param>
        /// <param name="step">between 0 (first call) and dimension - 1</param>
        /// <returns>interpolated result sample</returns>
        private float[] Rinterpolate(Rinterpol rinterpol, int step)
        {
            var coord = rinterpol.Coord;
            var input = rinterpol.Input;
            var inPrev = rinterpol.Prev;
            var inNext = rinterpol.Next;
            float[] resultSample = new float[OutputCount];
            if (step == coord.Length - 1)
            {
                // leaf
                if (inPrev[step] == inNext[step])
                {
                    coord[step] = inPrev[step];
                    var tmpSample = CalcSampleIndex(coord);
                    for (int i = 0; i < resultSample.Length; ++i)
                    {
                        resultSample[i] = samples[tmpSample, i];
                    }
                    return resultSample;
                }
                coord[step] = inPrev[step];
                var sample1 = CalcSampleIndex(coord);
                coord[step] = inNext[step];
                var sample2 = CalcSampleIndex(coord);
                for (int i = 0; i < resultSample.Length; ++i)
                {
                    resultSample[i] = Linear(input[step], inPrev[step], inNext[step], samples[sample1, i], samples[sample2, i]);
                }
                return resultSample;
            }
            else
            {
                // branch
                if (inPrev[step] == inNext[step])
                {
                    coord[step] = inPrev[step];
                    return Rinterpolate(rinterpol, step + 1);
                }
                coord[step] = inPrev[step];
                float[] sample1 = Rinterpolate(rinterpol, step + 1);
                coord[step] = inNext[step];
                float[] sample2 = Rinterpolate(rinterpol, step + 1);
                for (int i = 0; i < resultSample.Length; ++i)
                {
                    resultSample[i] = Linear(input[step], inPrev[step], inNext[step], sample1[i], sample2[i]);
                }
                return resultSample;
            }
        }

        /// <summary>
        /// calculate array index(structure described in p.171 PDF spec 1.7) in multiple dimensions.
        /// </summary>
        /// <param name="vector">with coordinates</param>
        /// <returns>index in flat array</returns>
        private int CalcSampleIndex(Span<int> vector)
        {
            // inspiration: http://stackoverflow.com/a/12113479/535646
            // but used in reverse
            var sizeValues = Sizes;
            int index = 0;
            int sizeProduct = 1;
            int dimension = vector.Length;
            for (int i = dimension - 2; i >= 0; --i)
            {
                sizeProduct *= sizeValues[i];
            }
            for (int i = dimension - 1; i >= 0; --i)
            {
                index += sizeProduct * vector[i];
                if (i - 1 >= 0)
                {
                    sizeProduct /= sizeValues[i - 1];
                }
            }
            return index;
        }

        private ref struct Rinterpol
        {
            public Span<float> Input;
            public Span<int> Prev;
            public Span<int> Next;
            public Span<int> Coord;

            public Rinterpol(Span<float> input, Span<int> prev, Span<int> next, Span<int> coord)
            {
                Input = input;
                Prev = prev;
                Next = next;
                Coord = coord;
            }
        }
    }
}
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
using System.IO;

namespace PdfGlown.Documents.Functions
{
    /// <summary>Stitching function producing a single new 1-input function from the combination of the
    /// subdomains of <see cref="Functions">several 1-input functions</see> [PDF:1.6:3.9.3].</summary>
    [Pdf(VersionEnum.PDF13)]
    public sealed class Type3Function : Function
    {
        private float[] bounds;
        private IList<Interval<float>> encodes;

        //TODO:implement function creation!

        internal Type3Function(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the <see cref="Domains">domain</see> partition bounds whose resulting intervals
        /// are respectively applied to each <see cref="Functions">function</see>.</summary>
        public float[] Bounds
        {
            get => bounds ??= Get<PdfArray>(PdfName.Bounds).ToFloatArray();
            set => SetDirect(PdfName.Bounds, new PdfArrayImpl(bounds = value));
        }

        /// <summary>Gets the mapping of each <see cref="Bounds">subdomain</see> into the domain
        /// of the corresponding <see cref="Functions">function</see>.</summary>
        public IList<Interval<float>> Encodes => encodes ??= GetIntervals<float>(PdfName.Encode, null);

        /// <summary>Gets the 1-input functions making up this stitching function.</summary>
        /// <remarks>The output dimensionality of all functions must be the same.</remarks>
        public Functions Functions => Get<Functions>(PdfName.Functions);

        public override ReadOnlySpan<float> Calculate(ReadOnlySpan<float> input)
        {
            //This function is known as a "stitching" function. Based on the input, it decides which child function to call.
            // All functions in the array are 1-value-input functions
            //See PDF Reference section 3.9.3.
            Function function = null;
            var domain = Domains[0];
            // clip input value to domain
            var x = ClipToRange(input[0], domain.Low, domain.High);

            if (Functions.Count == 1)
            {
                // This doesn't make sense but it may happen ...
                function = Functions[0];
                var encRange = Encodes[0];
                x = Linear(x, domain.Low, domain.High, encRange.Low, encRange.High);
            }
            else
            {
                var boundsValues = Bounds;
                int boundsSize = boundsValues.Length;
                // create a combined array containing the domain and the bounds values
                // domain.min, bounds[0], bounds[1], ...., bounds[boundsSize-1], domain.max
                float[] partitionValues = new float[boundsSize + 2];
                int partitionValuesSize = partitionValues.Length;
                partitionValues[0] = domain.Low;
                partitionValues[partitionValuesSize - 1] = domain.High;
                boundsValues.CopyTo(partitionValues.AsSpan(1, boundsSize));
                // find the partition 
                for (int i = 0; i < partitionValuesSize - 1; i++)
                {
                    if (x >= partitionValues[i]
                        && (x < partitionValues[i + 1]
                            || (i == partitionValuesSize - 2
                                && x == partitionValues[i + 1])))
                    {
                        function = Functions[i];
                        var encRange = Encodes[i];
                        x = Linear(x, partitionValues[i], partitionValues[i + 1], encRange.Low, encRange.High);
                        break;
                    }
                }
            }
            if (function == null)
            {
                throw new IOException("partition not found in type 3 function");
            }
            float[] functionValues = { x };
            // calculate the output values using the chosen function
            var functionResult = function.Calculate(functionValues).ToArray();
            // clip to range if available
            ClipToRange(functionResult);
            return functionResult;
        }

    }
}

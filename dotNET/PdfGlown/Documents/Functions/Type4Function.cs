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
using PdfGlown.Documents.Functions.Type4;
using PdfGlown.Objects;
using PdfGlown.Tokens;
using PdfGlown.Util.Math;
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Documents.Functions
{
    /// <summary>PostScript calculator function represented as a stream containing code written in a
    /// small subset of the PostScript language [PDF:1.6:3.9.4].</summary>
    [Pdf(VersionEnum.PDF13)]
    public sealed class Type4Function : Function
    {
        private InstructionSequence instructions;

        //TODO:implement function creation!

        internal Type4Function(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override void SetStream(IInputStream value)
        {
            base.SetStream(value);
            if (value != null)
            {
                using var data = (Stream)GetExtractedStream();
                using var input = new StreamReader(data, Charset.ISO88591);
                this.instructions = InstructionSequenceBuilder.Parse(input);
            }
        }

        public override ReadOnlySpan<float> Calculate(ReadOnlySpan<float> input)
        {
            var context = new ExecutionContext();
            for (int i = 0; i < input.Length; i++)
            {
                Interval<float> domain = Domains[i];
                float value = ClipToRange(input[i], domain.Low, domain.High);
                context.Push(value);
            }

            instructions.Execute(context);

            //Extract the output values
            int numberOfOutputValues = OutputCount;
            int numberOfActualOutputValues = context.Stack.Count;
            if (numberOfActualOutputValues < numberOfOutputValues)
            {
                throw new Exception("The type 4 function returned "
                        + numberOfActualOutputValues
                        + " values but the Range entry indicates that "
                        + numberOfOutputValues + " values be returned.");
            }
            float[] outputValues = new float[numberOfOutputValues];
            for (int i = numberOfOutputValues - 1; i >= 0; i--)
            {
                var range = Ranges[i];
                outputValues[i] = context.PopReal();
                outputValues[i] = ClipToRange(outputValues[i], range.Low, range.High);
            }
            return outputValues;
        }
    }
}
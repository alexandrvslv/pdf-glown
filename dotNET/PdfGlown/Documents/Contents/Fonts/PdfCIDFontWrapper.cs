/*
 * https://github.com/apache/pdfbox
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using PdfGlown.Bytes;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PdfGlown.Documents.Contents.Fonts
{
    public abstract class PdfCIDFontWrapper : PdfObjectWrapper<PdfCIDFont>, IPdfFont
    {
        protected readonly PdfType0Font parent;
        protected bool isEmbedded;
        protected bool isDamaged;
        private Dictionary<int, float> widths;
        private int? defaultWidth;
        private float averageWidth;

        private readonly Dictionary<int, float> verticalDisplacementY = new(); // w1y
        private readonly Dictionary<int, SKPoint> positionVectors = new();     // v
        private readonly float[] dw2 = new float[] { 880, -1000 };

        protected PdfCIDFontWrapper(PdfCIDFont dataObject, PdfType0Font parent)
            : base(dataObject.RefOrSelf)
        {
            this.parent = parent;
            ReadWidths();
            ReadVerticalDisplacements();
        }

        public int DefaultWidth
        {
            get => defaultWidth ??= DataObject.GetInt(PdfName.DW, 1000);
            set => DataObject.Set(PdfName.DW, defaultWidth = value);
        }


        public bool IsEmbedded
        {
            get => isEmbedded;
        }

        public bool IsDamaged
        {
            get => isDamaged;
        }

        public abstract BaseFont GenericFont { get; }

        /// <summary>Returns the Type 0 font which is the parent of this font.</summary>
        public PdfType0Font Parent
        {
            get => parent;            
        }

        // todo: this method is highly suspicious, the average glyph width is not usually a good metric
        public virtual float AverageFontWidth
        {
            get
            {
                if (averageWidth == 0)
                {
                    float totalWidths = 0.0f;
                    int characterCount = 0;
                    if (widths != null)
                    {
                        foreach (float width in widths.Values)
                        {
                            if (width > 0)
                            {
                                totalWidths += width;
                                ++characterCount;
                            }
                        }
                    }
                    if (characterCount != 0)
                    {
                        averageWidth = totalWidths / characterCount;
                    }
                    if (averageWidth <= 0 || float.IsNaN(averageWidth))
                    {
                        averageWidth = DefaultWidth;
                    }
                }
                return averageWidth;
            }
        }

        public abstract SKMatrix FontMatrix { get; }

        public abstract SKRect FontBBox { get; }

        /// <summary>Returns the CID for the given character code.If not found then CID 0 is returned.</summary>
        /// <param name="code">character code</param>
        /// <returns>CID</returns>
        public abstract int CodeToCID(int code);

        /// <summary>Returns the GID for the given character code.</summary>
        /// <param name="code">character code</param>
        /// <returns>GID</returns>
        public abstract int CodeToGID(int code);

        public abstract void EncodeGlyphId(Span<byte> bytes, int glyphId);

        public abstract SKPath GetPath(int code);

        public abstract float GetHeight(int code);

        public abstract float GetWidthFromFont(int code);

        public abstract bool HasGlyph(int code);

        public abstract int GetBytesCount(int code);

        public abstract void Encode(Span<byte> bytes, int unicode);

        public abstract int ReadCode(IInputStream input, out ReadOnlySpan<byte> bytes);

        public abstract int ReadCode(ReadOnlySpan<byte> bytes);

        public abstract SKPath GetNormalizedPath(int code);

        internal void ReadWidths()
        {
            widths = new Dictionary<int, float>();
            var wArray = DataObject.Widths;
            if (wArray != null)
            {
                int size = wArray.Count;
                int counter = 0;
                while (counter < size)
                {
                    var startRangeNullable = wArray.GetNInt(counter++);
                    if (startRangeNullable is not int startRange)
                    {
                        Debug.WriteLine($"warn: Expected a number array member, got {wArray.Get(counter - 1)}");
                        continue;
                    }
                    var next = wArray.Get<PdfDirectObject>(counter++);
                    if (next is PdfArray array)
                    {
                        int arraySize = array.Count;
                        for (int i = 0; i < arraySize; i++)
                        {
                            var floatNullable = array.GetNFloat(i);
                            if (floatNullable is not float floatValue)
                            {
                                Debug.WriteLine($"warn: Expected a number array member, got {array.Get(i)}");
                                continue;
                            }
                            widths[startRange + i] = floatValue;
                        }
                    }
                    else if (next is IPdfNumber number
                        && counter < size
                        && wArray.GetNFloat(counter++) is float width)
                    {
                        int endRange = number.IntValue;
                        for (int i = startRange; i <= endRange; i++)
                        {
                            widths[i] = width;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"warn: Expected two numbers, got {next} and {wArray.Get(counter - 1)}");
                    }
                }
            }
        }

        internal void ReadVerticalDisplacements()
        {
            // default position vector and vertical displacement vector
            var dw2Array = DataObject.VerticalDefaultWidth;
            if (dw2Array != null)
            {
                var number0 = dw2Array.GetNumber(0);
                var number1 = dw2Array.GetNumber(1);
                if (number0 != null && number1 != null)
                {
                    dw2[0] = number0.FloatValue;
                    dw2[1] = number1.FloatValue;
                }
            }

            // vertical metrics for individual CIDs.
            var w2Array = DataObject.VerticaltWidths;
            if (w2Array != null)
            {
                for (int i = 0; i < w2Array.Count; i++)
                {
                    var c = w2Array.GetInt(i);
                    var next = w2Array.Get<PdfDirectObject>(++i);
                    if (next is PdfArray array)
                    {
                        for (int j = 0; j < array.Count; j++)
                        {
                            int cid = c + j / 3;
                            var w1y = array.GetFloat(j);
                            var v1x = array.GetFloat(++j);
                            var v1y = array.GetFloat(++j);
                            verticalDisplacementY[cid] = w1y;
                            positionVectors[cid] = new SKPoint(v1x, v1y);
                        }
                    }
                    else
                    {
                        int first = c;
                        int last = ((IPdfNumber)next).IntValue;
                        var w1y = w2Array.GetFloat(++i);
                        var v1x = w2Array.GetFloat(++i);
                        var v1y = w2Array.GetFloat(++i);
                        for (int cid = first; cid <= last; cid++)
                        {
                            verticalDisplacementY[cid] = w1y;
                            positionVectors[cid] = new SKPoint(v1x, v1y);
                        }
                    }
                }
            }
        }

        /// <summary>Returns the default position vector(v).</summary>
        /// <param name="cid">CID</param>
        public virtual SKPoint GetDefaultPositionVector(int cid)
        {
            return new SKPoint(GetWidthForCID(cid) / 2, dw2[0]);
        }

        public float GetWidthForCID(int cid)
        {
            if (widths.TryGetValue(cid, out var width))
                return width;
            return DefaultWidth;
        }

        public virtual bool HasExplicitWidth(int code)
        {
            var cid = CodeToCID(code);
            return widths.TryGetValue(cid, out _);
        }

        public virtual SKPoint GetPositionVector(int code)
        {
            int cid = CodeToCID(code);
            if (positionVectors.TryGetValue(cid, out var position))
                return position;
            return GetDefaultPositionVector(cid);
        }

        /// <summary>Returns the y-component of the vertical displacement vector(w1).</summary>
        /// <param name="code">character code</param>
        /// <returns>w1y</returns>
        public virtual float GetVerticalDisplacementVectorY(int code)
        {
            int cid = CodeToCID(code);
            if (verticalDisplacementY.TryGetValue(cid, out var w1y))
            {
                return w1y;
            }
            return dw2[1];
        }

        public virtual float GetWidth(int code)
        {
            // these widths are supposed to be consistent with the actual widths given in the CIDFont
            // program, but PDFBOX-563 shows that when they are not, Acrobat overrides the embedded
            // font widths with the widths given in the font dictionary
            return GetWidthForCID(CodeToCID(code));
        }


        /// <summary>
        /// Encodes the given Unicode code point for use in a PDF content stream.
        /// Content streams use a multi-byte encoding with 1 to 4 bytes.
        /// <p>This method is called when embedding text in PDFs and when filling in fields.</p>
        /// </summary>
        /// <returns>Array of 1 to 4 PDF content stream bytes.</returns>
        public virtual int[] ReadCIDToGIDMap()
        {
            int[] cid2gid = null;
            if (DataObject.CIDToGIDMap is PdfStream stream)
            {
                var input = stream.GetInputStream();
                var mapAsBytes = input.AsMemory().Span;
                var length = (int)input.Length;
                int numberOfInts = length / 2;
                cid2gid = new int[numberOfInts];
                int offset = 0;
                for (int index = 0; index < numberOfInts; index++)
                {
                    int gid = (mapAsBytes[offset] & 0xff) << 8 | mapAsBytes[offset + 1] & 0xff;
                    cid2gid[index] = gid;
                    offset += 2;
                }
            }
            return cid2gid;
        }
    }
}
/*
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

using System.Diagnostics;
using System;
using System.Collections.Generic;
using PdfGlown.Bytes;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A 'kern' table in a true type font.
    /// @author Glenn Adams
    /// </summary>
    public class KerningSubtable
    {
        // coverage field bit masks and values
        private static readonly int COVERAGE_HORIZONTAL = 0x0001;
        private static readonly int COVERAGE_MINIMUMS = 0x0002;
        private static readonly int COVERAGE_CROSS_STREAM = 0x0004;
        private static readonly int COVERAGE_FORMAT = 0xFF00;

        private static readonly int COVERAGE_HORIZONTAL_SHIFT = 0;
        private static readonly int COVERAGE_MINIMUMS_SHIFT = 1;
        private static readonly int COVERAGE_CROSS_STREAM_SHIFT = 2;
        private static readonly int COVERAGE_FORMAT_SHIFT = 8;

        // true if horizontal kerning
        private bool horizontal;
        // true if minimum adjustment values (versus kerning values)
        private bool minimums;
        // true if cross-stream (block progression) kerning
        private bool crossStream;
        // format specific pair data
        private IPairData pairs;

        public KerningSubtable()
        { }

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="data">The stream to read the data from.</param>
        /// <param name="version">The version of the table to be read</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Read(IInputStream data, int version)
        {
            if (version == 0)
            {
                ReadSubtable0(data);
            }
            else if (version == 1)
            {
                ReadSubtable1(data);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Determine if subtable is designated for use in horizontal writing modes and
        /// contains inline progression kerning pairs(not block progression "cross stream")
        /// kerning pairs.
        /// </summary>
        /// <returns>true if subtable is for horizontal kerning</returns>
        public bool IsHorizontalKerning()
        {
            return IsHorizontalKerning(false);
        }

        /// <summary>
        /// Determine if subtable is designated for use in horizontal writing modes, contains
        /// kerning pairs (as opposed to minimum pairs), and, if CROSS is true, then return
        /// cross stream designator; otherwise, if CROSS is false, return true if cross stream
        /// designator is false.
        /// </summary>
        /// <param name="cross">if true, then return cross stream designator in horizontal modes</param>
        /// <returns>true if subtable is for horizontal kerning in horizontal modes</returns>
        public bool IsHorizontalKerning(bool cross)
        {
            if (!horizontal)
            {
                return false;
            }
            else if (minimums)
            {
                return false;
            }
            else if (cross)
            {
                return crossStream;
            }
            else
            {
                return !crossStream;
            }
        }

        /// <summary>
        /// Obtain kerning adjustments for GLYPHS sequence, where the
        /// Nth returned adjustment is associated with the Nth glyph
        /// and the succeeding non-zero glyph in the GLYPHS sequence.
        /// Kerning adjustments are returned in font design coordinates.
        /// </summary>
        /// <param name="glyphs">a (possibly empty) array of glyph identifiers</param>
        /// <returns>a (possibly empty) array of kerning adjustments</returns>
        public int[] GetKerning(int[] glyphs)
        {
            int[] kerning = null;
            if (pairs != null)
            {
                int ng = glyphs.Length;
                kerning = new int[ng];
                for (int i = 0; i < ng; ++i)
                {
                    int l = glyphs[i];
                    int r = -1;
                    for (int k = i + 1; k < ng; ++k)
                    {
                        int g = glyphs[k];
                        if (g >= 0)
                        {
                            r = g;
                            break;
                        }
                    }
                    kerning[i] = GetKerning(l, r);
                }
            }
            else
            {
                Debug.WriteLine("warn: No kerning subtable data available due to an unsupported kerning subtable version");
            }
            return kerning;
        }

        /// <summary>Obtain kerning adjustment for glyph pair {L,R}.</summary>
        /// <param name="l">left member of glyph pair</param>
        /// <param name="r">right member of glyph pair</param>
        /// <returns>a (possibly zero) kerning adjustment</returns>
        public int GetKerning(int l, int r)
        {
            if (pairs == null)
            {
                Debug.WriteLine("warn: No kerning subtable data available due to an unsupported kerning subtable version");
                return 0;
            }
            return pairs.GetKerning(l, r);
        }

        private void ReadSubtable0(IInputStream data)
        {
            int version = data.ReadUInt16();
            if (version != 0)
            {
                Debug.WriteLine($"info: Unsupported kerning sub-table version: {version}");
                return;
            }
            int length = data.ReadUInt16();
            if (length < 6)
            {
                Debug.WriteLine($"warn: Kerning sub-table too short, got {length} bytes, expect 6 or more.");
                return;
            }
            int coverage = data.ReadUInt16();
            if (IsBitsSet(coverage, COVERAGE_HORIZONTAL, COVERAGE_HORIZONTAL_SHIFT))
            {
                horizontal = true;
            }
            if (IsBitsSet(coverage, COVERAGE_MINIMUMS, COVERAGE_MINIMUMS_SHIFT))
            {
                minimums = true;
            }
            if (IsBitsSet(coverage, COVERAGE_CROSS_STREAM, COVERAGE_CROSS_STREAM_SHIFT))
            {
                crossStream = true;
            }
            int format = GetBits(coverage, COVERAGE_FORMAT, COVERAGE_FORMAT_SHIFT);
            switch (format)
            {
                case 0:
                    ReadSubtable0Format0(data);
                    break;
                case 2:
                    ReadSubtable0Format2(data);
                    break;
                default:
                    Debug.WriteLine($"debug: Skipped kerning subtable due to an unsupported kerning subtable version: {format}");
                    break;
            }
        }

        private void ReadSubtable0Format0(IInputStream data)
        {
            pairs = new PairData0Format0();
            pairs.Read(data);
        }

        private void ReadSubtable0Format2(IInputStream data)
        {
            Debug.WriteLine("info: Kerning subtable format 2 not yet supported.");
        }

        private void ReadSubtable1(IInputStream data)
        {
            Debug.WriteLine("info: Kerning subtable format 1 not yet supported.");
        }

        private static bool IsBitsSet(int bits, int mask, int shift)
        {
            return GetBits(bits, mask, shift) != 0;
        }

        private static int GetBits(int bits, int mask, int shift)
        {
            return (bits & mask) >> shift;
        }

        private interface IPairData
        {
            void Read(IInputStream data);

            int GetKerning(int l, int r);
        }

        private class PairData0Format0 : IComparer<int[]>, IPairData
        {
            private int searchRange;
            private KerningFormat0[] pairs;

            public void Read(IInputStream data)
            {
                int numPairs = data.ReadUInt16();
                searchRange = data.ReadUInt16() / 6;
                int entrySelector = data.ReadUInt16();
                int rangeShift = data.ReadUInt16();
                pairs = new KerningFormat0[numPairs];
                for (int i = 0; i < numPairs; ++i)
                {
                    var left = data.ReadUInt16();
                    var right = data.ReadUInt16();
                    var value = data.ReadInt16();
                    pairs[i] = new KerningFormat0(left, right, value);
                }
            }

            public int GetKerning(int l, int r)
            {
                var key = new KerningFormat0((ushort)l, (ushort)r, 0);
                int index = Array.BinarySearch(pairs, key);
                if (index >= 0)
                {
                    return pairs[index].Value;
                }
                return 0;
            }

            public int Compare(int[] p1, int[] p2)
            {
                Debug.Assert(p1 != null);
                Debug.Assert(p1.Length >= 2);
                Debug.Assert(p2 != null);
                Debug.Assert(p2.Length >= 2);
                int cmp1 = p1[0].CompareTo(p2[0]);
                if (cmp1 != 0)
                {
                    return cmp1;
                }
                return p1[1].CompareTo(p2[1]);
            }

            private struct KerningFormat0 : IComparable<KerningFormat0>
            {
                public readonly ushort Left;
                public readonly ushort Right;
                public readonly short Value;

                public KerningFormat0(ushort left, ushort right, short value)
                {
                    Left = left;
                    Right = right;
                    Value = value;
                }

                public int CompareTo(KerningFormat0 other)
                {
                    var result = Left.CompareTo(other.Left);
                    return result != 0 ? result : Right.CompareTo(other.Right);
                }
            }
        }
    }
}

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
using PdfGlown.Documents.Contents.Fonts.TTF;
using PdfGlown.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>
    /// Embedded PDCIDFontType2 builder. Helper class to populate a PDCIDFontType2 and its parent
    /// PDType0Font from a TTF.
    /// @author Keiji Suzuki
    /// @author John Hewson
    /// </summary>
    internal sealed class FontCIDType2Embedder : TrueTypeEmbedder
    {
        private readonly PdfType0Font parent;
        private readonly PdfCIDFontType2Wrapper cidFont;
        private readonly bool vertical;

        /// <summary>Creates a new TrueType font embedder for the given TTF as a PDCIDFontType2.</summary>
        /// <param name="document">parent document</param>
        /// <param name="parent">Type 0 font dictionary</param>
        /// <param name="ttf">True Type Font</param>
        /// <param name="embedSubset"></param>
        /// <param name="vertical"></param>
        public FontCIDType2Embedder(PdfDocument document, PdfType0Font parent, TrueTypeFont ttf, bool embedSubset, bool vertical)
                : base(document, parent, ttf, embedSubset)
        {
            this.parent = parent;
            this.vertical = vertical;

            // parent Type 0 font
            parent.Set(PdfName.Subtype, PdfName.Type0);
            parent.Set(PdfName.BaseFont, PdfName.Get(FontDescriptor.FontName));
            parent.Set(PdfName.Encoding, vertical ? PdfName.IdentityV : PdfName.IdentityH); // CID = GID

            // descendant CIDFont
            cidFont = CreateCIDFont();
            parent[PdfName.DescendantFonts] = new PdfArrayImpl(1)
            {
                cidFont.RefOrSelf
            };

            if (!embedSubset)
            {
                // build GID -> Unicode map
                BuildToUnicodeCMap(null);
            }
        }

        /// <summary>Returns the descendant CIDFont.</summary>
        public PdfCIDFontWrapper CIDFont
        {
            get => cidFont;
        }

        /// <summary>Rebuild a font subset.</summary>
        /// <param name="ttfSubset"></param>
        /// <param name="tag"></param>
        /// <param name="gidToCid"></param>
        protected override void BuildSubset(IOutputStream ttfSubset, string tag, Dictionary<int, int> gidToCid)
        {
            // build CID2GIDMap, because the content stream has been written with the old GIDs
            var cidToGid = new Dictionary<int, int>(gidToCid.Count);
            foreach (var entry in gidToCid)
            {
                //(newGID, oldGID)->
                cidToGid[entry.Value] = entry.Key;
            }

            // build unicode mapping before subsetting as the subsetted font won't have a cmap
            BuildToUnicodeCMap(gidToCid);
            // build vertical metrics before subsetting as the subsetted font won't have vhea, vmtx
            if (vertical)
            {
                BuildVerticalMetrics(cidToGid);
            }
            // rebuild the relevant part of the font
            BuildFontFile2(ttfSubset);
            AddNameTag(tag);
            BuildWidths(cidToGid);
            BuildCIDToGIDMap(cidToGid);
            BuildCIDSet(cidToGid);
        }

        private void BuildToUnicodeCMap(Dictionary<int, int> newGIDToOldCID)
        {
            var toUniWriter = new ToUnicodeWriter();
            bool hasSurrogates = false;
            for (int gid = 1, max = ttf.MaximumProfile.NumGlyphs; gid <= max; gid++)
            {
                // optional CID2GIDMap for subsetting
                int cid;
                if (newGIDToOldCID != null)
                {
                    if (!newGIDToOldCID.TryGetValue(gid, out cid))
                    {
                        continue;
                    }
                }
                else
                {
                    cid = gid;
                }

                // skip composite glyph components that have no code point
                List<int> codes = cmapLookup.GetCharCodes(cid); // old GID -> Unicode
                if (codes != null)
                {
                    // use the first entry even for ambiguous mappings
                    int codePoint = codes[0];
                    if (codePoint > 0xFFFF)
                    {
                        toUniWriter.Add(cid, char.ConvertFromUtf32(codePoint));
                        hasSurrogates = true;
                    }
                    else
                    {
                        toUniWriter.Add(cid, ((char)codePoint).ToString());
                    }
                }
            }

            var cMapStream = new ByteStream { };
            toUniWriter.WriteTo(cMapStream);

            var stream = new PdfStream(document, cMapStream);

            // surrogate code points, requires PDF 1.5
            if (hasSurrogates)
            {
                var version = document.Version;
                if (version.GetFloat() < 1.5)
                {
                    document.Catalog.Version = new PdfVersion(1, 5);
                }
            }

            parent[PdfName.ToUnicode] = stream.Reference;
        }

        private PdfCIDFontType2Wrapper CreateCIDFont()
        {
            // Vertical metrics
            PdfArray vHeader = null;
            var vMetrix = vertical ? BuildVerticalMetrics(out vHeader) : null;

            return new PdfCIDFontType2Wrapper( new PdfCIDFontType2(document, new Dictionary<PdfName, PdfDirectObject>
            {
                [PdfName.Type] = PdfName.Font,
                [PdfName.Subtype] = PdfName.CIDFontType2,
                [PdfName.BaseFont] = PdfName.Get(fontDescriptor.FontName),
                [PdfName.CIDSystemInfo] = new CIDSystemInfo(null, "Adobe", "Identity", 0),
                [PdfName.FontDescriptor] = fontDescriptor.RefOrSelf,
                [PdfName.CIDToGIDMap] = PdfName.Identity,
                [PdfName.W] = BuildWidths(),
                [PdfName.DW2] = vHeader,
                [PdfName.W2] = vMetrix,
            }), parent, ttf);
        }

        private void AddNameTag(string tag)
        {
            string name = fontDescriptor.FontName;
            string newName = tag + name;

            fontDescriptor.FontName = newName;
            parent[PdfName.BaseFont] =
                cidFont.DataObject[PdfName.BaseFont] = PdfName.Get(newName);
        }

        private void BuildCIDToGIDMap(Dictionary<int, int> cidToGid)
        {
            int cidMax = cidToGid.Keys.Max();
            var output = new ByteStream(cidMax * 2 + 2);
            output.SetLength(0);
            for (int i = 0; i <= cidMax; i++)
            {
                if (!cidToGid.TryGetValue(i, out var gid))
                {
                    gid = 0;
                }
                output.Write((ushort)gid);
            }
            output.Seek(0);
            var stream = new PdfStream(document, output);

            cidFont.DataObject[PdfName.CIDToGIDMap] = stream.Reference;
        }

        /// <summary>Builds the CIDSet entry, required by PDF/A.This lists all CIDs in the font, including those
        /// that don't have a GID.</summary>
        private void BuildCIDSet(Dictionary<int, int> cidToGid)
        {
            int cidMax = cidToGid.Keys.Max();
            byte[] bytes = new byte[cidMax / 8 + 1];
            for (int cid = 0; cid <= cidMax; cid++)
            {
                int mask = 1 << 7 - cid % 8;
                bytes[cid / 8] = (byte)(bytes[cid / 8] | mask);
            }

            var input = new ByteStream(bytes);
            var stream = new PdfStream(document, input);

            fontDescriptor.CIDSet = stream;
        }

        /// <summary>Builds widths with a custom CIDToGIDMap(for embedding font subset).</summary>
        /// <param name="cidToGid"></param>
        private void BuildWidths(Dictionary<int, int> cidToGid)
        {
            float scaling = 1000f / ttf.Header.UnitsPerEm;

            var widths = new PdfArrayImpl();
            var ws = new PdfArrayImpl();
            int prev = int.MinValue;
            // Use a sorted list to get an optimal width array  

            var horizontalMetricsTable = ttf.HorizontalMetrics;
            foreach (var entry in cidToGid)
            {
                var cid = entry.Key;
                int gid = entry.Value;
                long width = (long)Math.Round(horizontalMetricsTable.GetAdvanceWidth(gid) * scaling);
                if (width == 1000)
                {
                    // skip default width
                    continue;
                }
                // c [w1 w2 ... wn]
                if (prev != cid - 1)
                {
                    ws = new PdfArrayImpl();
                    widths.Add(cid); // c
                    widths.Add(ws);
                }
                ws.Add(width); // wi
                prev = cid;
            }
            cidFont.DataObject[PdfName.W] = widths;
        }

        private bool BuildVerticalHeader(out PdfArray array)
        {
            array = null;
            VerticalHeaderTable vhea = ttf.VerticalHeader;
            if (vhea == null)
            {
                Debug.WriteLine("warn: Font to be subset is set to vertical, but has no 'vhea' table");
                return false;
            }

            float scaling = 1000f / ttf.Header.UnitsPerEm;

            long v = (long)Math.Round(vhea.Ascender * scaling);
            long w1 = (long)Math.Round(-vhea.AdvanceHeightMax * scaling);
            if (v != 880 || w1 != -1000)
            {
                array = new PdfArrayImpl { v, w1 };
            }
            return true;
        }

        /// <summary>Builds vertical metrics with a custom CIDToGIDMap(for embedding font subset).</summary>
        /// <param name="cidToGid"></param>
        private void BuildVerticalMetrics(Dictionary<int, int> cidToGid)
        {
            // The "vhea" and "vmtx" tables that specify vertical metrics shall never be used by a conforming
            // reader. The only way to specify vertical metrics in PDF shall be by means of the DW2 and W2
            // entries in a CIDFont dictionary.

            if (!BuildVerticalHeader(out var vArray))
            {
                return;
            }
            cidFont.DataObject[PdfName.DW2] = vArray;

            float scaling = 1000f / ttf.Header.UnitsPerEm;

            VerticalHeaderTable vhea = ttf.VerticalHeader;
            VerticalMetricsTable vmtx = ttf.VerticalMetrics;
            GlyphTable glyf = ttf.Glyph;
            HorizontalMetricsTable hmtx = ttf.HorizontalMetrics;

            long v_y = (long)Math.Round(vhea.Ascender * scaling);
            long w1 = (long)Math.Round(-vhea.AdvanceHeightMax * scaling);

            var heights = new PdfArrayImpl();
            var w2 = new PdfArrayImpl();
            int prev = int.MinValue;
            // Use a sorted list to get an optimal width array
            ISet<int> keys = new HashSet<int>(cidToGid.Keys);
            foreach (int cid in keys)
            {
                // Unlike buildWidths, we look up with cid (not gid) here because this is
                // the original TTF, not the rebuilt one.
                GlyphData glyph = glyf.GetGlyph(cid);
                if (glyph == null)
                {
                    continue;
                }
                long height = (long)Math.Round((glyph.YMaximum + vmtx.GetTopSideBearing(cid)) * scaling);
                long advance = (long)Math.Round(-vmtx.GetAdvanceHeight(cid) * scaling);
                if (height == v_y && advance == w1)
                {
                    // skip default metrics
                    continue;
                }
                // c [w1_1y v_1x v_1y w1_2y v_2x v_2y ... w1_ny v_nx v_ny]
                if (prev != cid - 1)
                {
                    w2 = new PdfArrayImpl();
                    heights.Add(cid); // c
                    heights.Add(w2);
                }
                w2.Add(advance); // w1_iy
                long width = (long)Math.Round(hmtx.GetAdvanceWidth(cid) * scaling);
                w2.Add(width / 2); // v_ix
                w2.Add(height); // v_iy
                prev = cid;
            }
            cidFont.DataObject[PdfName.W2] = heights;
        }

        /// <summary>Build widths with Identity CIDToGIDMap(for embedding full font).</summary>
        private PdfArray BuildWidths()
        {
            int cidMax = ttf.NumberOfGlyphs;
            int[] gidwidths = new int[cidMax * 2];
            var horizontalMetricsTable = ttf.HorizontalMetrics;
            for (int cid = 0; cid < cidMax; cid++)
            {
                gidwidths[cid * 2] = cid;
                gidwidths[cid * 2 + 1] = horizontalMetricsTable.GetAdvanceWidth(cid);
            }

            return GetWidths(gidwidths);
        }

        enum State
        {
            FIRST, BRACKET, SERIAL
        }

        private PdfArray GetWidths(int[] widths)
        {
            if (widths.Length < 2)
            {
                throw new ArgumentException("length of widths must be > 0");
            }

            float scaling = 1000f / ttf.Header.UnitsPerEm;

            long lastCid = widths[0];
            long lastValue = (long)Math.Round(widths[1] * scaling);

            var inner = new PdfArrayImpl();
            var outer = new PdfArrayImpl { lastCid };

            State state = State.FIRST;

            for (int i = 2; i < widths.Length - 1; i += 2)
            {
                long cid = widths[i];
                long value = (long)Math.Round(widths[i + 1] * scaling);

                switch (state)
                {
                    case State.FIRST:
                        if (cid == lastCid + 1 && value == lastValue)
                        {
                            state = State.SERIAL;
                        }
                        else if (cid == lastCid + 1)
                        {
                            state = State.BRACKET;
                            inner = new PdfArrayImpl { lastValue };
                        }
                        else
                        {
                            inner = new PdfArrayImpl { lastValue };
                            outer.Add(inner);
                            outer.Add(cid);
                        }
                        break;
                    case State.BRACKET:
                        if (cid == lastCid + 1 && value == lastValue)
                        {
                            state = State.SERIAL;
                            outer.Add(inner);
                            outer.Add(lastCid);
                        }
                        else if (cid == lastCid + 1)
                        {
                            inner.Add(lastValue);
                        }
                        else
                        {
                            state = State.FIRST;
                            inner.Add(lastValue);
                            outer.Add(inner);
                            outer.Add(cid);
                        }
                        break;
                    case State.SERIAL:
                        if (cid != lastCid + 1 || value != lastValue)
                        {
                            outer.Add(lastCid);
                            outer.Add(lastValue);
                            outer.Add(cid);
                            state = State.FIRST;
                        }
                        break;
                }
                lastValue = value;
                lastCid = cid;
            }

            switch (state)
            {
                case State.FIRST:
                    inner = new PdfArrayImpl { lastValue };
                    outer.Add(inner);
                    break;
                case State.BRACKET:
                    inner.Add(lastValue);
                    outer.Add(inner);
                    break;
                case State.SERIAL:
                    outer.Add(lastCid);
                    outer.Add(lastValue);
                    break;
            }
            return outer;
        }

        /// <summary>Build vertical metrics with Identity CIDToGIDMap(for embedding full font).</summary>
        /// <param name="vHeader">Vertical header</param>
        /// <returns></returns>
        private PdfArray BuildVerticalMetrics(out PdfArray vHeader)
        {
            if (!BuildVerticalHeader(out vHeader))
            {
                return null;
            }

            int cidMax = ttf.NumberOfGlyphs;
            int[] gidMetrics = new int[cidMax * 4];
            var glyphTable = ttf.Glyph;
            var vTable = ttf.VerticalMetrics;
            var hTable = ttf.HorizontalMetrics;

            for (int cid = 0; cid < cidMax; cid++)
            {
                GlyphData glyph = glyphTable.GetGlyph(cid);
                if (glyph == null)
                {
                    gidMetrics[cid * 4] = int.MinValue;
                }
                else
                {
                    gidMetrics[cid * 4] = cid;
                    gidMetrics[cid * 4 + 1] = vTable.GetAdvanceHeight(cid);
                    gidMetrics[cid * 4 + 2] = hTable.GetAdvanceWidth(cid);
                    gidMetrics[cid * 4 + 3] = glyph.YMaximum + vTable.GetTopSideBearing(cid);
                }
            }

            return GetVerticalMetrics(gidMetrics);
        }

        private PdfArray GetVerticalMetrics(int[] values)
        {
            if (values.Length < 4)
            {
                throw new ArgumentException("length of values must be > 0");
            }

            float scaling = 1000f / ttf.Header.UnitsPerEm;

            long lastCid = values[0];
            long lastW1Value = (long)Math.Round(-values[1] * scaling);
            long lastVxValue = (long)Math.Round(values[2] * scaling / 2f);
            long lastVyValue = (long)Math.Round(values[3] * scaling);

            var inner = new PdfArrayImpl();
            var outer = new PdfArrayImpl { lastCid };

            State state = State.FIRST;

            for (int i = 4; i < values.Length - 3; i += 4)
            {
                long cid = values[i];
                if (cid == int.MinValue)
                {
                    // no glyph for this cid
                    continue;
                }
                long w1Value = (long)Math.Round(-values[i + 1] * scaling);
                long vxValue = (long)Math.Round(values[i + 2] * scaling / 2);
                long vyValue = (long)Math.Round(values[i + 3] * scaling);

                switch (state)
                {
                    case State.FIRST:
                        if (cid == lastCid + 1 && w1Value == lastW1Value && vxValue == lastVxValue && vyValue == lastVyValue)
                        {
                            state = State.SERIAL;
                        }
                        else if (cid == lastCid + 1)
                        {
                            state = State.BRACKET;
                            inner = new PdfArrayImpl
                            {
                                lastW1Value,
                                lastVxValue,
                                lastVyValue
                            };
                        }
                        else
                        {
                            inner = new PdfArrayImpl
                            {
                                lastW1Value,
                                lastVxValue,
                                lastVyValue
                            };
                            outer.Add(inner);
                            outer.Add(cid);
                        }
                        break;
                    case State.BRACKET:
                        if (cid == lastCid + 1 && w1Value == lastW1Value && vxValue == lastVxValue && vyValue == lastVyValue)
                        {
                            state = State.SERIAL;
                            outer.Add(inner);
                            outer.Add(lastCid);
                        }
                        else if (cid == lastCid + 1)
                        {
                            inner.Add(lastW1Value);
                            inner.Add(lastVxValue);
                            inner.Add(lastVyValue);
                        }
                        else
                        {
                            state = State.FIRST;
                            inner.Add(lastW1Value);
                            inner.Add(lastVxValue);
                            inner.Add(lastVyValue);
                            outer.Add(inner);
                            outer.Add(cid);
                        }
                        break;
                    case State.SERIAL:
                        if (cid != lastCid + 1 || w1Value != lastW1Value || vxValue != lastVxValue || vyValue != lastVyValue)
                        {
                            outer.Add(lastCid);
                            outer.Add(lastW1Value);
                            outer.Add(lastVxValue);
                            outer.Add(lastVyValue);
                            outer.Add(cid);
                            state = State.FIRST;
                        }
                        break;
                }
                lastW1Value = w1Value;
                lastVxValue = vxValue;
                lastVyValue = vyValue;
                lastCid = cid;
            }

            switch (state)
            {
                case State.FIRST:
                    inner = new PdfArrayImpl
                    {
                        lastW1Value,
                        lastVxValue,
                        lastVyValue
                    };
                    outer.Add(inner);
                    break;
                case State.BRACKET:
                    inner.Add(lastW1Value);
                    inner.Add(lastVxValue);
                    inner.Add(lastVyValue);
                    outer.Add(inner);
                    break;
                case State.SERIAL:
                    outer.Add(lastCid);
                    outer.Add(lastW1Value);
                    outer.Add(lastVxValue);
                    outer.Add(lastVyValue);
                    break;
            }
            return outer;
        }


    }
}

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
using PdfGlown.Bytes;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A table in a true type font.
    /// @author Ben Litchfield
    /// </summary>
    public class GlyphTable : TTFTable
    {
        /// <summary>Tag to identify this table.</summary>
        public const string TAG = "glyf";

        private Dictionary<int, GlyphData> glyphs = new();

        // lazy table reading
        private IInputStream data;
        private IndexToLocationTable loca;
        private int numGlyphs;
        private HorizontalMetricsTable hmt = null;
        private MaximumProfileTable maxp;

        public GlyphTable()
        { }

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="ttf">The font that is being read.</param>
        /// <param name="data">The stream to read the data from.</param>
        public override void Read(TrueTypeFont ttf, IInputStream data)
        {
            loca = ttf.IndexToLocation;
            numGlyphs = ttf.NumberOfGlyphs;

            glyphs = new Dictionary<int, GlyphData>();

            // we don't actually read the complete table here because it can contain tens of thousands of glyphs
            data.Seek(Offset);
            var dataBytes = data.ReadMemory((int)Length);
            this.data = new ByteStream(dataBytes);

            // PDFBOX-5460: read hmtx table early to avoid deadlock if getGlyph() locks "data"
            // and then locks TrueTypeFont to read this table, while another thread
            // locks TrueTypeFont and then tries to lock "data"
            hmt = ttf.HorizontalMetrics;
            maxp = ttf.MaximumProfile;

            initialized = true;
        }

        //public Dictionary<int, GlyphData> Glyphs
        //{
        //    get
        //    {
        //        // PDFBOX-4219: synchronize on data because it is accessed by several threads
        //        // when PDFBox is accessing a standard 14 font for the first time
        //        lock (data)
        //        {
        //            // the glyph offsets
        //            long[] offsets = loca.Offsets;

        //            // the end of the glyph table
        //            // should not be 0, but sometimes is, see PDFBOX-2044
        //            // structure of this table: see
        //            // https://developer.apple.com/fonts/TTRefMan/RM06/Chap6loca.html
        //            long endOfGlyphs = offsets[numGlyphs];
        //            long offset = Offset;
        //            if (glyphs == null)
        //            {
        //                glyphs = new Dictionary<int, GlyphData>();
        //            }

        //            for (int gid = 0; gid < numGlyphs; gid++)
        //            {
        //                // end of glyphs reached?
        //                if (endOfGlyphs != 0 && endOfGlyphs == offsets[gid])
        //                {
        //                    break;
        //                }
        //                // the current glyph isn't defined
        //                // if the next offset is equal or smaller to the current offset
        //                if (offsets[gid + 1] <= offsets[gid])
        //                {
        //                    continue;
        //                }
        //                if (glyphs.TryGetValue(gid, out _))
        //                {
        //                    // already cached
        //                    continue;
        //                }

        //                data.Seek(offset + offsets[gid]);

        //                glyphs[gid] = GetGlyphData(gid);
        //            }
        //            initialized = true;
        //            return glyphs;
        //        }
        //    }
        //    set => glyphs = value;
        //}

        /// <summary>Returns the data for the glyph with the given GID.</summary>
        /// <param name="gid">GID</param>
        public GlyphData GetGlyph(int gid) => GetGlyph(gid, 0);

        public GlyphData GetGlyph(int gid, int level)
        {
            if (gid < 0 || gid >= numGlyphs)
            {
                return null;
            }

            if (glyphs != null && glyphs.TryGetValue(gid, out var gdata))
            {
                return gdata;
            }

            GlyphData glyph;
            // PDFBOX-4219: synchronize on data because it is accessed by several threads
            // when PDFBox is accessing a standard 14 font for the first time
            lock (data)
            {
                // read a single glyph
                long[] offsets = loca.Offsets;

                if (offsets[gid] == offsets[gid + 1])
                {
                    // no outline
                    // PDFBOX-5135: can't return null, must return an empty glyph because
                    // sometimes this is used in a composite glyph.
                    glyph = new GlyphData();
                    glyph.InitEmptyData();
                }
                else
                {
                    // save
                    long currentPosition = data.Position;

                    data.Seek(offsets[gid]);

                    glyph = GetGlyphData(gid, level);

                    // restore
                    data.Seek(currentPosition);
                }
                if (glyphs != null)
                {
                    glyphs[gid] = glyph;
                }

                return glyph;
            }
        }

        private GlyphData GetGlyphData(int gid, int level)
        {
            if (level > maxp.MaxComponentDepth)
            {
                throw new IOException("composite glyph maximum level reached");
            }
            GlyphData glyph = new GlyphData();
            int leftSideBearing = hmt == null ? 0 : hmt.GetLeftSideBearing(gid);
            glyph.InitData(this, data, leftSideBearing, level);
            // resolve composite glyph
            if (glyph.Description.IsComposite)
            {
                glyph.Description.Resolve();
            }
            return glyph;
        }
    }
}

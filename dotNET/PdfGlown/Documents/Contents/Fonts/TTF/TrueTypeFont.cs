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
using PdfGlown.Documents.Contents.Fonts.TTF.Model;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A TrueType font file.
    /// @author Ben Litchfield
    /// </summary>
    public class TrueTypeFont : BaseFont, IDisposable
    {
        private float version;
        private int? numberOfGlyphs;
        private ushort? unitsPerEm;
        protected Dictionary<string, TTFTable> tables = new(StringComparer.Ordinal);
        private readonly IInputStream data;
        private volatile Dictionary<string, int> postScriptNames;
        private List<float> fontMatrix;
        private SKRect? fonBBox;
        private CmapTableLookup cmapTableLookup;
        private readonly object lockReadtable = new object();
        private readonly object lockPSNames = new object();
        private readonly List<string> enabledGsubFeatures = new();

        internal TrueTypeFont(IInputStream fontData)
        {
            data = fontData;
        }

        public void Dispose()
        {
            data.Dispose();
        }

        /// <summary>The version.</summary>
        public virtual float Version
        {
            get => version;
            set => version = value;
        }

        /// <summary>Add a table definition.Package-private, used by TTFParser only.</summary>
        /// <param name="table">The table to add</param>
        public void AddTable(TTFTable table)
        {
            tables[table.Tag] = table;
        }

        /// <summary>Get all of the tables.</summary>
        public ICollection<TTFTable> Tables
        {
            get => tables.Values;
        }

        /// <summary>Get all of the tables.</summary>
        public Dictionary<string, TTFTable> TableMap
        {
            get => tables;
        }

        /// <summary>Returns the raw bytes of the given table.</summary>
        /// <param name="table">the table to read</param>
        /// <returns></returns>
        public Memory<byte> GetTableBytes(TTFTable table)
        {
            lock (lockReadtable)
            {
                // save current position
                long currentPosition = data.Position;
                data.Seek(table.Offset);

                // read all data
                var bytes = data.ReadMemory((int)table.Length);

                // restore current position
                data.Seek(currentPosition);
                return bytes;
            }
        }

        /// <summary>This will get the table for the given tag.</summary>
        /// <param name="tag">the name of the table to be returned</param>
        /// <returns>The table with the given tag</returns>
        protected TTFTable GetTable(string tag)
        {
            if (tables.TryGetValue(tag, out TTFTable table) && !table.Initialized)
            {
                ReadTable(table);
            }
            return table;
        }

        /// <summary>This will get the naming table for the true type font or null if it doesn't exist.</summary>
        public NamingTable Naming
        {
            get => (NamingTable)GetTable(NamingTable.TAG);
        }

        /// <summary>Get the postscript table for this TTF  or null if it doesn't exist.</summary>
        public PostScriptTable PostScript
        {
            get => (PostScriptTable)GetTable(PostScriptTable.TAG);
        }

        /// <summary>Get the OS/2 table for this TTF.</summary>
        public OS2WindowsMetricsTable OS2Windows
        {
            get => (OS2WindowsMetricsTable)GetTable(OS2WindowsMetricsTable.TAG);
        }

        /// <summary>Get the maxp table for this TTF.</summary>
        public MaximumProfileTable MaximumProfile
        {
            get => (MaximumProfileTable)GetTable(MaximumProfileTable.TAG);
        }

        /// <summary>Get the head table for this TTF.</summary>
        public HeaderTable Header
        {
            get => (HeaderTable)GetTable(HeaderTable.TAG);
        }

        /// <summary>Get the hhea table for this TTF.</summary>
        public HorizontalHeaderTable HorizontalHeader
        {
            get => (HorizontalHeaderTable)GetTable(HorizontalHeaderTable.TAG);
        }

        /// <summary>Get the hmtx table for this TTF.</summary>
        public HorizontalMetricsTable HorizontalMetrics
        {
            get => (HorizontalMetricsTable)GetTable(HorizontalMetricsTable.TAG);
        }

        /// <summary>Get the loca table for this TTF.</summary>
        public IndexToLocationTable IndexToLocation
        {
            get => (IndexToLocationTable)GetTable(IndexToLocationTable.TAG);
        }

        /// <summary>Get the glyf table for this TTF.</summary>
        public virtual GlyphTable Glyph
        {
            get => (GlyphTable)GetTable(GlyphTable.TAG);
        }

        /// <summary>Get the "cmap" table for this TTF.</summary>
        public CmapTable Cmap
        {
            get => (CmapTable)GetTable(CmapTable.TAG);
        }

        /// <summary>Get the vhea table for this TTF.</summary>
        public VerticalHeaderTable VerticalHeader
        {
            get => (VerticalHeaderTable)GetTable(VerticalHeaderTable.TAG);
        }

        /// <summary>Get the vmtx table for this TTF.</summary>
        public VerticalMetricsTable VerticalMetrics
        {
            get => (VerticalMetricsTable)GetTable(VerticalMetricsTable.TAG);
        }

        /// <summary>Get the VORG table for this TTF.</summary>
        public VerticalOriginTable VerticalOrigin
        {
            get => (VerticalOriginTable)GetTable(VerticalOriginTable.TAG);
        }

        /// <summary>Get the "kern" table for this TTF.</summary>
        public KerningTable Kerning
        {
            get => (KerningTable)GetTable(KerningTable.TAG);
        }

        /// <summary>Get the "gsub" table for this TTF.</summary>
        public GlyphSubstitutionTable Gsub
        {
            get => (GlyphSubstitutionTable)GetTable(GlyphSubstitutionTable.TAG);
        }

        /// <summary>Get the data of the TrueType Font
        /// program representing the stream used to build this 
        /// object (normally from the TTFParser object).</summary>
        /// <param name="pos"></param>
        /// <returns>IInputStream TrueType font program stream</returns>
        public IInputStream GetOriginalData(out long pos)
        {
            pos = data.Position;
            data.Seek(0);
            return data;
        }

        /// <summary>Get the data size of the TrueType Font program representing the stream used to build this
        /// object (normally from the TTFParser object)</summary>
        public long OriginalDataSize
        {
            get => data.Length;
        }

        /// <summary>Read the given table if necessary. Package-private, used by TTFParser only.</summary>
        /// <param name="table">the table to be initialized</param>
        public void ReadTable(TTFTable table)
        {
            // PDFBOX-4219: synchronize on data because it is accessed by several threads
            // when PDFBox is accessing a standard 14 font for the first time
            lock (data)
            {
                // save current position
                long currentPosition = data.Position;
                data.Seek(table.Offset);
                table.Read(this, data);
                // restore current position
                data.Seek(currentPosition);
            }
        }

        /// <summary>The number of glyphs(MaximumProfile.numGlyphs).</summary>
        public int NumberOfGlyphs
        {
            get => numberOfGlyphs ??= (MaximumProfile?.NumGlyphs ?? 0);
        }

        /// <summary>Returns the units per EM (Header.unitsPerEm).</summary>
        public ushort UnitsPerEm
        {
            get => unitsPerEm ??= (Header?.UnitsPerEm ?? 1000);
        }

        /// <summary>Returns the width for the given GID.</summary>
        /// <param name="gid">the GID</param>
        /// <returns>the width</returns>
        public int GetAdvanceWidth(int gid)
        {
            HorizontalMetricsTable hmtx = HorizontalMetrics;
            if (hmtx != null)
            {
                return hmtx.GetAdvanceWidth(gid);
            }
            else
            {
                // this should never happen
                return 250;
            }
        }

        /// <summary>Returns the height for the given GID.</summary>
        /// <param name="gid">the GID</param>
        /// <returns>the height</returns>
        public int GetAdvanceHeight(int gid)
        {
            VerticalMetricsTable vmtx = VerticalMetrics;
            if (vmtx != null)
            {
                return vmtx.GetAdvanceHeight(gid);
            }
            else
            {
                // this should never happen
                return 250;
            }
        }

        public override string Name
        {
            get => Naming?.PostScriptName;
        }

        private void ReadPostScriptNames()
        {
            var psnames = postScriptNames;
            if (psnames == null)
            {
                // the getter is already synchronized
                PostScriptTable post = PostScript;
                lock (lockPSNames)
                {
                    psnames = postScriptNames;
                    if (psnames == null)
                    {
                        string[] names = post != null ? post.GlyphNames : null;
                        if (names != null)
                        {
                            psnames = new Dictionary<string, int>(names.Length, StringComparer.Ordinal);
                            for (int i = 0; i < names.Length; i++)
                            {
                                psnames[names[i]] = i;
                            }
                        }
                        else
                        {
                            psnames = new Dictionary<string, int>(StringComparer.Ordinal);
                        }
                        postScriptNames = psnames;
                    }
                }
            }
        }

        /// <summary>Returns the best Unicode from the font (the most general). The PDF spec says that "The means
        /// by which this is accomplished are implementation-dependent."
        /// The returned cmap will perform glyph substitution.</summary>
        public ICmapLookup GetUnicodeCmapLookup()
        {
            return GetUnicodeCmapLookup(true);
        }

        /// <summary>
        /// Returns the best Unicode from the font (the most general). The PDF spec says that "The means
        /// by which this is accomplished are implementation-dependent."
        /// The returned cmap will perform glyph substitution.
        /// </summary>
        /// <param name="isStrict">False if we allow falling back to any cmap, even if it's not Unicode.</param>
        /// <returns></returns>
        public ICmapLookup GetUnicodeCmapLookup(bool isStrict)
        {
            CmapSubtable cmap = GetUnicodeCmapImpl(isStrict);
            if (enabledGsubFeatures.Count > 0)
            {
                GlyphSubstitutionTable table = Gsub;
                if (table != null)
                {
                    return new SubstitutingCmapLookup(cmap, table, enabledGsubFeatures);
                }
            }
            return cmap;
        }

        private ICmapLookup GetCmapTableLookup()
        {
            return cmapTableLookup ??= new CmapTableLookup(Cmap, Gsub, enabledGsubFeatures);
        }

        private CmapSubtable GetUnicodeCmapImpl(bool isStrict)
        {
            CmapTable cmapTable = Cmap;
            if (cmapTable == null)
            {
                if (isStrict)
                {
                    throw new IOException("The TrueType font " + Name + " does not contain a 'cmap' table");
                }
                else
                {
                    return null;
                }
            }

            CmapSubtable cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_UNICODE,
                                                      CmapTable.ENCODING_UNICODE_2_0_FULL);
            if (cmap == null)
            {
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS,
                                             CmapTable.ENCODING_WIN_UNICODE_FULL);
            }
            if (cmap == null)
            {
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_UNICODE,
                                             CmapTable.ENCODING_UNICODE_2_0_BMP);
            }
            if (cmap == null)
            {
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS,
                                             CmapTable.ENCODING_WIN_UNICODE_BMP);
            }
            if (cmap == null)
            {
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_MACINTOSH,
                                             CmapTable.ENCODING_UNICODE_1_0);
            }
            if (cmap == null)
            {
                // Microsoft's "Recommendations for OpenType Fonts" says that "Symbol" encoding
                // actually means "Unicode, non-standard character set"
                cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS,
                                             CmapTable.ENCODING_WIN_SYMBOL);
            }
            if (cmap == null)
            {
                if (isStrict)
                {
                    throw new IOException("The TrueType font does not contain a Unicode cmap");
                }
                else if (cmapTable.Cmaps.Length > 0)
                {
                    // fallback to the first cmap (may not be Unicode, so may produce poor results)
                    cmap = cmapTable.Cmaps[0];
                }
            }
            return cmap;
        }

        /// <summary>Returns the GID for the given PostScript name, if the "post" table is present.</summary>
        /// <param name="name">the PostScript name.</param>
        public int NameToGID(string name)
        {
            // look up in 'post' table
            ReadPostScriptNames();
            if (postScriptNames != null)
            {
                if (postScriptNames.TryGetValue(name, out int gid)
                    && gid > 0
                    && gid < MaximumProfile.NumGlyphs)
                {
                    return gid;
                }
            }

            // look up in 'cmap'
            int uni = ParseUniName(name);
            if (uni > -1)
            {
                ICmapLookup cmap = GetCmapTableLookup();
                return cmap.GetGlyphId(uni);
            }

            // PDFBOX-5604: assume gnnnnn is a gid
            //if (name.Length > 1
            //    && int.TryParse(name.AsSpan().Slice(1), out var intValue))
            //{
            //    return intValue;
            //}


            return 0;
        }

        public IGsubData GsubData
        {
            get
            {
                GlyphSubstitutionTable table = Gsub;
                if (table == null)
                {
                    return DefaultGsubData.NO_DATA_FOUND;
                }

                return table.GsubData;
            }
        }

        /// <summary>Parses a Unicode PostScript name in the format uniXXXX.</summary>
        private int ParseUniName(string name)
        {
            if (name.StartsWith("uni", StringComparison.Ordinal)
                && (name.Length - 3) % 4 == 0)
            {
                int nameLength = name.Length;
                try
                {
                    for (int chPos = 3; chPos + 4 <= nameLength; chPos += 4)
                    {
                        if (int.TryParse(name.AsSpan(chPos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint)
                          && (codePoint <= 0xD7FF || codePoint >= 0xE000))// disallowed code area
                        {
                            return codePoint;
                        }
                    }
                    return -1;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"error: ParseUniName {e}");
                    return -1;
                }
            }
            return -1;
        }

        public override SKPath GetPath(string name)
        {
            int gid = NameToGID(name);

            // some glyphs have no outlines (e.g. space, table, newline)
            // must scaled by caller using FontMatrix

            return Glyph.GetGlyph(gid)?.GetPath();
        }

        public override float GetWidth(string name)
        {
            int gid = NameToGID(name);
            return GetAdvanceWidth(gid);
        }

        public override bool HasGlyph(string name)
        {
            return NameToGID(name) != 0;
        }

        public override SKRect FontBBox
        {
            get => fonBBox ??= GenerateBBox();
        }

        private SKRect GenerateBBox()
        {
            var header = Header;
            short xMin = header.XMin;
            short xMax = header.XMax;
            short yMin = header.YMin;
            short yMax = header.YMax;
            float scale = 1000f / UnitsPerEm;
            return new SKRect(xMin * scale, yMin * scale, xMax * scale, yMax * scale);
        }

        public override List<float> FontMatrix
        {
            get => fontMatrix ??= GenerateFontMatrix();
        }

        private List<float> GenerateFontMatrix()
        {
            float scale = 1000f / UnitsPerEm;
            return new List<float>(6) { 0.001f * scale, 0, 0, 0.001f * scale, 0, 0 };
        }

        /// <summary>Enable a particular glyph substitution feature. This feature might not be supported by the
        /// font, or might not be implemented in PDFBox yet.</summary>
        /// <param name="featureTag">The GSUB feature to enable</param>
        public void EnableGsubFeature(string featureTag)
        {
            enabledGsubFeatures.Add(featureTag);
        }

        /// <summary>Disable a particular glyph substitution feature.</summary>
        /// <param name="featureTag">The GSUB feature to disable</param>
        public void DisableGsubFeature(string featureTag)
        {
            enabledGsubFeatures.Remove(featureTag);
        }

        /// <summary>Enable glyph substitutions for vertical writing.</summary>
        public void EnableVerticalSubstitutions()
        {
            EnableGsubFeature("vrt2");
            EnableGsubFeature("vert");
        }

        public override string ToString()
        {
            try
            {
                return Naming?.PostScriptName ?? "(null)";
            }
            catch (IOException e)
            {
                Debug.WriteLine("debug: Error getting the NamingTable for the font " + e);
                return $"(null - {e.Message})";
            }
        }
    }
}

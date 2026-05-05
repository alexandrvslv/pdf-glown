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
using System;
using System.Diagnostics;
using System.IO;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// TrueType font file parser.
    /// @author Ben Litchfield
    /// </summary>
    public class TTFParser
    {
        private bool isEmbedded = false;

        public TTFParser() : this(false)
        {
        }

        /// <summary>Constructor.</summary>
        /// <param name="isEmbedded">true if the font is embedded in PDF</param>
        public TTFParser(bool isEmbedded)
        {
            this.isEmbedded = isEmbedded;
        }

        /// <summary>Parse a file and return a TrueType font.</summary>
        /// <param name="fileName">The TrueType font filename</param>
        /// <param name="fontName"></param>
        /// <returns>A TrueType font</returns>
        public TrueTypeFont Parse(string fileName, string fontName = null)
        {
            using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Parse(fileStream, fontName);
        }

        public TrueTypeFont Parse(Stream fileStream, string fontName = null) => Parse((IInputStream)new ByteStream(fileStream), fontName);

        /// <summary>Parse an input stream and return a TrueType font that is to be embedded.</summary>
        /// <param name="inputStream">The TTF data stream to parse from.It will be closed before returning</param>
        /// <param name="fontName"></param>
        /// <returns>A TrueType font</returns>
        public TrueTypeFont ParseEmbedded(IInputStream inputStream, string fontName = null)
        {
            isEmbedded = true;
            return Parse(inputStream, fontName);
        }

        /// <summary>
        /// Parse a file and get a true type font.
        /// </summary>
        /// <param name="inputStream">The TTF file</param>
        /// <param name="fontName"></param>
        /// <returns>A TrueType font</returns>
        public TrueTypeFont Parse(IInputStream inputStream, string fontName = null)
        {
            if (string.Equals(inputStream.ReadString(4), TrueTypeCollection.TAG, StringComparison.Ordinal))
            {
                inputStream.Seek(inputStream.Position - 4);
                var fontCollection = new TrueTypeCollection(inputStream);

                var nameFont = fontCollection.GetFontByName(fontName);
                if (nameFont == null)
                    nameFont = fontCollection.GetFontAtIndex(0);
                return nameFont;
            }
            inputStream.Seek(inputStream.Position - 4);

            var font = NewFont(inputStream);
            font.Version = inputStream.Read32Fixed();
            int numberOfTables = inputStream.ReadUInt16();
            int searchRange = inputStream.ReadUInt16();
            int entrySelector = inputStream.ReadUInt16();
            int rangeShift = inputStream.ReadUInt16();
            for (int i = 0; i < numberOfTables; i++)
            {
                TTFTable table = ReadTableDirectory(inputStream);

                // skip tables with zero length
                if (table != null)
                {
                    if ((table.Offset + table.Length) > font.OriginalDataSize)
                    {
                        // PDFBOX-5285 if we're lucky, this is an "unimportant" table, e.g. vmtx
                        Debug.WriteLine($"warn: Skip table '{table.Tag}' which is oversize; offset: {table.Offset}, size: {table.Length}, font size: {font.OriginalDataSize}");
                    }
                    else
                    {
                        font.AddTable(table);
                    }
                }
            }
            // parse tables
            ParseTables(font);

            return font;
        }

        public virtual TrueTypeFont NewFont(IInputStream raf)
        {
            return new TrueTypeFont(raf);
        }

        /// <summary>Parse all tables and check if all needed tables are present.</summary>
        /// <param name="font">the TrueTypeFont instance holding the parsed data.</param>
        /// <exception cref="IOException"></exception>
        private void ParseTables(TrueTypeFont font)
        {
            foreach (TTFTable table in font.Tables)
            {
                if (!table.Initialized)
                {
                    font.ReadTable(table);
                }
            }

            var otf = font as OpenTypeFont;
            var hasCFF = font.TableMap.ContainsKey(CFFTable.TAG);
            var isOTF = otf != null;
            var isPostScript = isOTF ? otf.IsPostScript : hasCFF;

            HeaderTable head = font.Header;
            if (head == null)
            {
                throw new IOException("head table is mandatory");
            }

            HorizontalHeaderTable hh = font.HorizontalHeader;
            if (hh == null)
            {
                throw new IOException("hhead table is mandatory");
            }

            MaximumProfileTable maxp = font.MaximumProfile;
            if (maxp == null)
            {
                throw new IOException("maxp table is mandatory");
            }

            PostScriptTable post = font.PostScript;
            if (post == null && !isEmbedded)
            {
                // in an embedded font this table is optional
                throw new IOException("post table is mandatory");
            }

            if (!isPostScript)
            {
                if (font.IndexToLocation == null)
                {
                    throw new IOException("loca table is mandatory");
                }

                if (font.Glyph == null)
                {
                    throw new IOException("glyf table is mandatory");
                }
            }

            if (font.Naming == null && !isEmbedded)
            {
                throw new IOException("name table is mandatory");
            }

            if (font.HorizontalMetrics == null)
            {
                throw new IOException("hmtx table is mandatory");
            }

            if (!isEmbedded && font.Cmap == null)
            {
                throw new IOException("cmap table is mandatory");
            }
        }

        protected virtual bool AllowCFF
        {
            get => false;
        }

        private TTFTable ReadTableDirectory(IInputStream raf)
        {
            TTFTable table;
            string tag = raf.ReadString(4);
            switch (tag)
            {
                case CmapTable.TAG:
                    table = new CmapTable();
                    break;
                case GlyphTable.TAG:
                    table = new GlyphTable();
                    break;
                case HeaderTable.TAG:
                    table = new HeaderTable();
                    break;
                case HorizontalHeaderTable.TAG:
                    table = new HorizontalHeaderTable();
                    break;
                case HorizontalMetricsTable.TAG:
                    table = new HorizontalMetricsTable();
                    break;
                case IndexToLocationTable.TAG:
                    table = new IndexToLocationTable();
                    break;
                case MaximumProfileTable.TAG:
                    table = new MaximumProfileTable();
                    break;
                case NamingTable.TAG:
                    table = new NamingTable();
                    break;
                case OS2WindowsMetricsTable.TAG:
                    table = new OS2WindowsMetricsTable();
                    break;
                case PostScriptTable.TAG:
                    table = new PostScriptTable();
                    break;
                case DigitalSignatureTable.TAG:
                    table = new DigitalSignatureTable();
                    break;
                case KerningTable.TAG:
                    table = new KerningTable();
                    break;
                case VerticalHeaderTable.TAG:
                    table = new VerticalHeaderTable();
                    break;
                case VerticalMetricsTable.TAG:
                    table = new VerticalMetricsTable();
                    break;
                case VerticalOriginTable.TAG:
                    table = new VerticalOriginTable();
                    break;
                case GlyphSubstitutionTable.TAG:
                    table = new GlyphSubstitutionTable();
                    break;
                default:
                    table = ReadTable(tag);
                    break;
            }
            table.Tag = tag;
            table.CheckSum = raf.ReadUInt32();
            table.Offset = raf.ReadUInt32();
            table.Length = raf.ReadUInt32();

            // skip tables with zero length (except glyf)
            if (table.Length == 0 && !tag.Equals(GlyphTable.TAG, StringComparison.Ordinal))
            {
                return null;
            }

            return table;
        }

        protected virtual TTFTable ReadTable(string tag)
        {
            // unknown table type but read it anyway.
            return new TTFTable();
        }
    }
}

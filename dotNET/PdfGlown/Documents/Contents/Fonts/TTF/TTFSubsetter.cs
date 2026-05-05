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
 * distributed under the License input distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using PdfGlown.Bytes;
using PdfGlown.Tokens;
using PdfGlown.Util.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// Subsetter for TrueType (TTF) fonts.
    /// <p>Originally developed by Wolfgang Glas for
    /// <a href="https://clazzes.org/display/SKETCH/Clazzes.org+Sketch+Home">Sketch</a>.
    /// @author Wolfgang Glas
    /// </summary>
    public sealed class TTFSubsetter
    {
        private static readonly byte[] PAD_BUF = new byte[] { 0, 0, 0 };
        //private static readonly TimeZoneInfo TIMEZONE_UTC = TimeZoneInfo.FromSerializedString("UTC");
        private readonly TrueTypeFont ttf;
        private readonly ICmapLookup unicodeCmap;
        private readonly SortedDictionary<int, int> uniToGID;

        private readonly List<string> keepTables;
        private readonly SortedSet<int> glyphIds; // new glyph ids
        private string prefix;
        private bool hasAddedCompoundReferences;

        /// <summary>Creates a subsetter for the given font.</summary>
        /// <param name="ttf">the font to be subset</param>
        public TTFSubsetter(TrueTypeFont ttf)
               : this(ttf, null)
        { }

        /// <summary>Creates a subsetter for the given font.</summary>
        /// <param name="ttf">the font to be subset</param>
        /// <param name="tables">optional tables to keep if present</param>
        public TTFSubsetter(TrueTypeFont ttf, List<string> tables)
        {
            this.ttf = ttf;
            this.keepTables = tables;

            uniToGID = new SortedDictionary<int, int>();
            glyphIds = new SortedSet<int>();

            // find the best Unicode cmap
            this.unicodeCmap = ttf.GetUnicodeCmapLookup();

            // always copy GID 0
            glyphIds.Add(0);
        }

        /// <summary>Sets the prefix to add to the font's PostScript name.</summary>
        /// <param name="prefix"></param>
        public void SetPrefix(string prefix)
        {
            this.prefix = prefix;
        }

        /// <summary>Add the given character code to the subset.</summary>
        /// <param name="unicode">character code</param>
        public void Add(int unicode)
        {
            int gid = unicodeCmap.GetGlyphId(unicode);
            if (gid != 0)
            {
                uniToGID[unicode] = gid;
                glyphIds.Add(gid);
            }
        }

        /// <summary>Add the given character codes to the subset.</summary>
        /// <param name="unicodeSet">character code set</param>
        public void AddAll(ISet<int> unicodeSet)
        {
            foreach (var item in unicodeSet) { Add(item); }
        }

        /// <summary>Returns the map of new -&gt; old GIDs.</summary>
        public Dictionary<int, int> GetGIDMap()
        {
            AddCompoundReferences();

            var newToOld = new Dictionary<int, int>();
            int newGID = 0;
            foreach (int oldGID in glyphIds)
            {
                newToOld[newGID] = oldGID;
                newGID++;
            }
            return newToOld;
        }

        /// <summary>Write File Head</summary>
        /// <param name="output">The data output stream.</param>
        /// <param name="nTables">The number of table.</param>
        /// <returns>The file offset of the first TTF table to write.</returns>
        private long WriteFileHeader(IOutputStream output, int nTables)
        {
            output.Write(0x00010000);
            output.Write((short)nTables);

            short mask = (short)((uint)nTables).HighestOneBit();
            short searchRange = (short)(mask * 16);
            output.Write(searchRange);

            var entrySelector = (short)log2(mask);

            output.Write(entrySelector);

            // numTables * 16 - searchRange
            int last = 16 * nTables - searchRange;
            output.Write((short)last);

            return 0x00010000L + ToUInt32(nTables, searchRange) + ToUInt32(entrySelector, last);
        }

        private long WriteTableHeader(IOutputStream output, string tag, long offset, ByteStream bytes)
        {
            long checksum = 0;
            for (int nup = 0, n = (int)bytes.Length; nup < n; nup++)
            {
                checksum += (bytes.GetByte(nup) & 0xffL) << 24 - nup % 4 * 8;
            }
            checksum &= 0xffffffffL;

            byte[] tagbytes = Charset.ASCII.GetBytes(tag);

            output.Write(tagbytes, 0, 4);
            output.Write((int)checksum);
            output.Write((int)offset);
            output.Write(bytes.Length);

            // account for the checksum twice, once for the header field, once for the content itself
            return (long)StreamExtensions.ReadUInt32(tagbytes) + checksum + checksum + offset + bytes.Length;
        }

        private void WriteTableBody(IOutputStream os, ByteStream bytes)
        {
            int n = (int)bytes.Length;
            os.Write((IInputStream)bytes);
            if (n % 4 != 0)
            {
                os.Write(PAD_BUF, 0, 4 - n % 4);
            }
        }

        private ByteStream buildHeadTable()
        {
            var output = new ByteStream(32);
            {
                HeaderTable h = ttf.Header;
                output.WriteFixed(h.Version);
                output.WriteFixed(h.FontRevision);
                output.Write((uint)0); // h.CheckSumAdjustment()
                output.Write(h.MagicNumber);
                output.Write(h.Flags);
                output.Write(h.UnitsPerEm);
                output.WriteLongDateTime(h.Created);
                output.WriteLongDateTime(h.Modified);
                output.Write(h.XMin);
                output.Write(h.YMin);
                output.Write(h.XMax);
                output.Write(h.YMax);
                output.Write(h.MacStyle);
                output.Write(h.LowestRecPPEM);
                output.Write(h.FontDirectionHint);
                // force long format of 'loca' table
                output.Write((short)1); // h.IndexToLocFormat()
                output.Write(h.GlyphDataFormat);
                output.Flush();

                return output;
            }
        }

        private ByteStream buildHheaTable()
        {
            var output = new ByteStream(32);
            {
                HorizontalHeaderTable h = ttf.HorizontalHeader;
                output.WriteFixed(h.Version);
                output.Write(h.Ascender);
                output.Write(h.Descender);
                output.Write(h.LineGap);
                output.Write(h.AdvanceWidthMax);
                output.Write(h.MinLeftSideBearing);
                output.Write(h.MinRightSideBearing);
                output.Write(h.XMaxExtent);
                output.Write(h.CaretSlopeRise);
                output.Write(h.CaretSlopeRun);
                output.Write(h.Reserved1); // caretOffset
                output.Write(h.Reserved2);
                output.Write(h.Reserved3);
                output.Write(h.Reserved4);
                output.Write(h.Reserved5);
                output.Write(h.MetricDataFormat);

                // input there a GID >= numberOfHMetrics ? Then keep the last entry of original hmtx table,
                // (add if it isn't in our set of GIDs), see also in buildHmtxTable()
                int hmetrics = glyphIds.GetViewBetween(0, h.NumberOfHMetrics).Count();
                if (glyphIds.LastOrDefault() >= h.NumberOfHMetrics && !glyphIds.Contains(h.NumberOfHMetrics - 1))
                {
                    ++hmetrics;
                }
                output.Write((ushort)hmetrics);

                output.Flush();
                return output;
            }
        }

        private bool ShouldCopyNameRecord(NameRecord nr)
        {
            return nr.PlatformId == NameRecord.PLATFORM_WINDOWS
                    && nr.PlatformEncodingId == NameRecord.ENCODING_WIN_UNICODE_BMP
                    && nr.LanguageId == NameRecord.LANGUAGE_WIN_EN_US
                    && nr.NameId >= 0 && nr.NameId < 7;
        }

        private ByteStream BuildNameTable()
        {
            var output = new ByteStream(32);
            NamingTable name = ttf.Naming;
            if (name == null || keepTables != null && !keepTables.Contains(NamingTable.TAG, StringComparer.Ordinal))
            {
                return null;
            }

            List<NameRecord> nameRecords = name.NameRecords;
            int numRecords = nameRecords.Count(p => ShouldCopyNameRecord(p));
            output.Write((ushort)0);
            output.Write((ushort)numRecords);
            output.Write((ushort)(2 * 3 + 2 * 6 * numRecords));

            if (numRecords == 0)
            {
                return null;
            }

            byte[][] names = new byte[numRecords][];
            int j = 0;
            foreach (NameRecord nameRecord in nameRecords)
            {
                if (ShouldCopyNameRecord(nameRecord))
                {
                    int platform = nameRecord.PlatformId;
                    int encoding = nameRecord.PlatformEncodingId;
                    var charset = Charset.ISO88591;

                    if (platform == CmapTable.PLATFORM_WINDOWS &&
                        encoding == CmapTable.ENCODING_WIN_UNICODE_BMP)
                    {
                        charset = Charset.UTF16BE;
                    }
                    else if (platform == 2) // ISO [deprecated]=
                    {
                        if (encoding == 0) // 7-bit ASCII
                        {
                            charset = Charset.ASCII;
                        }
                        else if (encoding == 1) // ISO 10646=
                        {
                            //not sure input this input correct??
                            charset = Charset.UTF16BE;
                        }
                    }
                    string value = nameRecord.ToString();
                    if (nameRecord.NameId == 6 && prefix != null)
                    {
                        value = prefix + value;
                    }
                    names[j] = charset.GetBytes(value);
                    j++;
                }
            }

            int offset = 0;
            j = 0;
            foreach (NameRecord nr in nameRecords)
            {
                if (ShouldCopyNameRecord(nr))
                {
                    output.Write(nr.PlatformId);
                    output.Write(nr.PlatformEncodingId);
                    output.Write(nr.LanguageId);
                    output.Write(nr.NameId);
                    output.Write((ushort)names[j].Length);
                    output.Write((ushort)offset);
                    offset += names[j].Length;
                    j++;
                }
            }

            for (int i = 0; i < numRecords; i++)
            {
                output.Write(names[i]);
            }

            output.Flush();
            return output;
        }

        private ByteStream BuildMaxpTable()
        {
            var output = new ByteStream(32);
            MaximumProfileTable p = ttf.MaximumProfile;
            output.WriteFixed(1.0);
            output.Write((ushort)glyphIds.Count);
            if (p.Version >= 1.0f)
            {
                output.Write(p.MaxPoints);
                output.Write(p.MaxContours);
                output.Write(p.MaxCompositePoints);
                output.Write(p.MaxCompositeContours);
                output.Write(p.MaxZones);
                output.Write(p.MaxTwilightPoints);
                output.Write(p.MaxStorage);
                output.Write(p.MaxFunctionDefs);
                output.Write(p.MaxInstructionDefs);
                output.Write(p.MaxStackElements);
                output.Write(p.MaxSizeOfInstructions);
                output.Write(p.MaxComponentElements);
                output.Write(p.MaxComponentDepth);
            }
            output.Flush();
            return output;
        }

        private ByteStream BuildOS2Table()
        {
            OS2WindowsMetricsTable os2 = ttf.OS2Windows;
            if (os2 == null || uniToGID.Count == 0 || keepTables != null
                && !keepTables.Contains(OS2WindowsMetricsTable.TAG, StringComparer.Ordinal))
            {
                return null;
            }

            var output = new ByteStream(32);
            output.Write(os2.Version);
            output.Write(os2.AverageCharWidth);
            output.Write(os2.WeightClass);
            output.Write(os2.WidthClass);

            output.Write(os2.FsType);

            output.Write(os2.SubscriptXSize);
            output.Write(os2.SubscriptYSize);
            output.Write(os2.SubscriptXOffset);
            output.Write(os2.SubscriptYOffset);

            output.Write(os2.SuperscriptXSize);
            output.Write(os2.SuperscriptYSize);
            output.Write(os2.SuperscriptXOffset);
            output.Write(os2.SuperscriptYOffset);

            output.Write(os2.StrikeoutSize);
            output.Write(os2.StrikeoutPosition);
            output.Write(os2.FamilyClass);
            output.Write(os2.Panose);

            output.Write((uint)0);
            output.Write((uint)0);
            output.Write((uint)0);
            output.Write((uint)0);

            output.Write(Charset.ASCII.GetBytes(os2.AchVendId));

            output.Write(os2.FsSelection);
            output.Write((ushort)uniToGID.Keys.First());
            output.Write((ushort)uniToGID.Keys.Last());
            output.Write(os2.TypoAscender);
            output.Write(os2.TypoDescender);
            output.Write(os2.TypoLineGap);
            output.Write(os2.WinAscent);
            output.Write(os2.WinDescent);

            output.Flush();
            return output;
        }

        // never returns null
        private ByteStream BuildLocaTable(long[] newOffsets)
        {
            var output = new ByteStream(32);
            {
                foreach (long offset in newOffsets)
                {
                    output.Write((uint)offset);
                }

                output.Flush();
                return output;
            }
        }

        /// <summary>Resolve compound glyph references.</summary>
        private void AddCompoundReferences()
        {
            if (hasAddedCompoundReferences)
            {
                return;
            }
            hasAddedCompoundReferences = true;

            bool hasNested;
            GlyphTable g = ttf.Glyph;
            long[] offsets = ttf.IndexToLocation.Offsets;
            do
            {
                var input = ttf.GetOriginalData(out var pos);
                ISet<int> glyphIdsToAdd = null;
                try
                {
                    long isResult = input.Skip(g.Offset);

                    if (isResult.CompareTo(g.Offset) != 0)
                    {
                        Debug.WriteLine($"debug: Tried skipping {g.Offset} bytes but skipped only {isResult} bytes");
                    }

                    long lastOff = 0L;
                    foreach (int glyphId in glyphIds)
                    {
                        long offset = offsets[glyphId];
                        long len = offsets[glyphId + 1] - offset;
                        isResult = input.Skip(offset - lastOff);

                        if (isResult.CompareTo(offset - lastOff) != 0)
                        {
                            Debug.WriteLine($"debug: Tried skipping {(offset - lastOff)} bytes but skipped only {isResult} bytes");
                        }

                        sbyte[] buf = new sbyte[(int)len];
                        isResult = input.Read(buf);

                        if (isResult.CompareTo(len) != 0)
                        {
                            Debug.WriteLine($"debug: Tried reading {len} bytes but only {isResult} bytes read");
                        }

                        // rewrite glyphIds for compound glyphs
                        if (buf.Length >= 2 && buf[0] == -1 && buf[1] == -1)
                        {
                            int off = 2 * 5;
                            int flags;
                            do
                            {
                                flags = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                                off += 2;
                                int ogid = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                                if (!glyphIds.Contains(ogid))
                                {
                                    if (glyphIdsToAdd == null)
                                    {
                                        glyphIdsToAdd = new HashSet<int>();
                                    }
                                    glyphIdsToAdd.Add(ogid);
                                }
                                off += 2;
                                // ARG_1_AND_2_ARE_WORDS
                                if ((flags & 1 << 0) != 0)
                                {
                                    off += 2 * 2;
                                }
                                else
                                {
                                    off += 2;
                                }
                                // WE_HAVE_A_TWO_BY_TWO
                                if ((flags & 1 << 7) != 0)
                                {
                                    off += 2 * 4;
                                }
                                // WE_HAVE_AN_X_AND_Y_SCALE
                                else if ((flags & 1 << 6) != 0)
                                {
                                    off += 2 * 2;
                                }
                                // WE_HAVE_A_SCALE
                                else if ((flags & 1 << 3) != 0)
                                {
                                    off += 2;
                                }
                            }
                            while ((flags & 1 << 5) != 0); // MORE_COMPONENTS

                        }
                        lastOff = offsets[glyphId + 1];
                    }
                }
                finally
                {
                    input.Seek(pos);
                }
                hasNested = glyphIdsToAdd != null;
                if (hasNested)
                {
                    glyphIds.AddRange(glyphIdsToAdd);
                }
            }
            while (hasNested);
        }

        // never returns null
        private ByteStream BuildGlyfTable(long[] newOffsets)
        {

            GlyphTable g = ttf.Glyph;
            long[] offsets = ttf.IndexToLocation.Offsets;

            var input = ttf.GetOriginalData(out var pos);
            long isResult = input.Skip(g.Offset);

            if (isResult.CompareTo(g.Offset) != 0)
            {
                Debug.WriteLine($"debug: Tried skipping {g.Offset} bytes but skipped only {isResult} bytes");
            }

            long prevEnd = 0;    // previously read glyph offset
            long newOffset = 0;  // new offset for the glyph in the subset font
            int newGid = 0;      // new GID in subset font
            var bos = new ByteStream(32);
            // for each glyph in the subset
            foreach (int gid in glyphIds)
            {
                long offset = offsets[gid];
                long length = offsets[gid + 1] - offset;

                newOffsets[newGid++] = newOffset;
                isResult = input.Skip(offset - prevEnd);

                if (isResult.CompareTo(offset - prevEnd) != 0)
                {
                    Debug.WriteLine($"debug: Tried skipping {(offset - prevEnd)} bytes but skipped only {isResult} bytes");
                }

                sbyte[] buf = new sbyte[(int)length];
                isResult = input.Read(buf);

                if (isResult.CompareTo(length) != 0)
                {
                    Debug.WriteLine($"debug: Tried reading {length} bytes but only {isResult} bytes read");
                }

                // detect glyph type
                if (buf.Length >= 2 && buf[0] == -1 && buf[1] == -1)
                {
                    // compound glyph
                    int off = 2 * 5;
                    int flags;
                    do
                    {
                        // flags
                        flags = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                        off += 2;

                        // glyphIndex
                        int componentGid = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                        glyphIds.Add(componentGid);

                        int newComponentGid = GetNewGlyphId(componentGid);
                        buf[off] = (sbyte)((uint)(newComponentGid) >> 8);
                        buf[off + 1] = (sbyte)newComponentGid;
                        off += 2;

                        // ARG_1_AND_2_ARE_WORDS
                        if ((flags & 1 << 0) != 0)
                        {
                            off += 2 * 2;
                        }
                        else
                        {
                            off += 2;
                        }
                        // WE_HAVE_A_TWO_BY_TWO
                        if ((flags & 1 << 7) != 0)
                        {
                            off += 2 * 4;
                        }
                        // WE_HAVE_AN_X_AND_Y_SCALE
                        else if ((flags & 1 << 6) != 0)
                        {
                            off += 2 * 2;
                        }
                        // WE_HAVE_A_SCALE
                        else if ((flags & 1 << 3) != 0)
                        {
                            off += 2;
                        }
                    }
                    while ((flags & 1 << 5) != 0); // MORE_COMPONENTS

                    // WE_HAVE_INSTRUCTIONS
                    if ((flags & 0x0100) == 0x0100)
                    {
                        // USHORT numInstr
                        int numInstr = (buf[off] & 0xff) << 8 | buf[off + 1] & 0xff;
                        off += 2;

                        // BYTE instr[numInstr]
                        off += numInstr;
                    }

                    // write the compound glyph
                    bos.Write((byte[])(Array)buf, 0, off);

                    // offset to start next glyph
                    newOffset += off;
                }
                else if (buf.Length > 0)
                {
                    // copy the entire glyph
                    bos.Write((byte[])(Array)buf, 0, buf.Length);

                    // offset to start next glyph
                    newOffset += buf.Length;
                }

                // 4-byte alignment
                if (newOffset % 4 != 0)
                {
                    int len = 4 - (int)(newOffset % 4);
                    bos.Write(PAD_BUF, 0, len);
                    newOffset += len;
                }

                prevEnd = offset + length;
            }
            newOffsets[newGid++] = newOffset;
            input.Seek(pos);
            return bos;
        }

        private int GetNewGlyphId(int oldGid)
        {
            return glyphIds.GetViewBetween(0, oldGid).Count;
        }

        private ByteStream BuildCmapTable()
        {
            if (ttf.Cmap == null || uniToGID.Count == 0 || keepTables != null
                && !keepTables.Contains(CmapTable.TAG, StringComparer.Ordinal))
            {
                return null;
            }

            var output = new ByteStream(32);
            // cmap header
            output.Write((ushort)0); // version
            output.Write((ushort)1); // numberSubtables

            // encoding record
            output.Write((ushort)CmapTable.PLATFORM_WINDOWS); // platformID
            output.Write((ushort)CmapTable.ENCODING_WIN_UNICODE_BMP); // platformSpecificID
            output.Write((uint)12); // offset 4 * 2 + 4

            // build Format 4 subtable (Unicode BMP)
            var it = uniToGID.GetEnumerator();
            it.MoveNext();
            var lastChar = it.Current;
            var prevChar = lastChar;
            int lastGid = GetNewGlyphId(lastChar.Value);

            // +1 because .notdef input missing in uniToGID
            int[] startCode = new int[uniToGID.Count + 1];
            int[] endCode = new int[startCode.Length];
            int[] idDelta = new int[startCode.Length];
            int segCount = 0;
            while (it.MoveNext())
            {
                var curChar2Gid = it.Current;
                int curGid = GetNewGlyphId(curChar2Gid.Value);

                // todo: need format Format 12 for non-BMP
                if (curChar2Gid.Key > 0xFFFF)
                {
                    throw new NotSupportedException("non-BMP Unicode character");
                }

                if (curChar2Gid.Key != prevChar.Key + 1 ||
                    curGid - lastGid != curChar2Gid.Key - lastChar.Key)
                {
                    if (lastGid != 0)
                    {
                        // don't emit ranges, which map to GID 0, the
                        // undef glyph input emitted a the very last segment
                        startCode[segCount] = lastChar.Key;
                        endCode[segCount] = prevChar.Key;
                        idDelta[segCount] = lastGid - lastChar.Key;
                        segCount++;
                    }
                    else if (!lastChar.Key.Equals(prevChar.Key))
                    {
                        // shorten ranges which start with GID 0 by one
                        startCode[segCount] = lastChar.Key + 1;
                        endCode[segCount] = prevChar.Key;
                        idDelta[segCount] = lastGid - lastChar.Key;
                        segCount++;
                    }
                    lastGid = curGid;
                    lastChar = curChar2Gid;
                }
                prevChar = curChar2Gid;
            }

            // trailing segment
            startCode[segCount] = lastChar.Key;
            endCode[segCount] = prevChar.Key;
            idDelta[segCount] = lastGid - lastChar.Key;
            segCount++;

            // GID 0
            startCode[segCount] = 0xffff;
            endCode[segCount] = 0xffff;
            idDelta[segCount] = 1;
            segCount++;

            // write format 4 subtable
            int searchRange = 2 * (int)Math.Pow(2, log2(segCount));
            output.Write((ushort)4); // format
            output.Write((ushort)(8 * 2 + segCount * 4 * 2)); // length
            output.Write((ushort)0); // language
            output.Write((ushort)(segCount * 2)); // segCountX2
            output.Write((ushort)searchRange); // searchRange
            output.Write((ushort)log2(searchRange / 2)); // entrySelector
            output.Write((ushort)(2 * segCount - searchRange)); // rangeShift

            // endCode[segCount]
            for (int i = 0; i < segCount; i++)
            {
                output.Write((ushort)endCode[i]);
            }

            // reservedPad
            output.Write((ushort)0);

            // startCode[segCount]
            for (int i = 0; i < segCount; i++)
            {
                output.Write((ushort)startCode[i]);
            }

            // idDelta[segCount]
            for (int i = 0; i < segCount; i++)
            {
                output.Write((ushort)idDelta[i]);
            }

            for (int i = 0; i < segCount; i++)
            {
                output.Write((ushort)0);
            }

            return output;
        }

        private ByteStream BuildPostTable()
        {
            PostScriptTable post = ttf.PostScript;
            if (post == null || keepTables != null
                && !keepTables.Contains(PostScriptTable.TAG, StringComparer.Ordinal))
            {
                return null;
            }

            var output = new ByteStream(32);
            output.WriteFixed(2.0); // version
            output.WriteFixed(post.ItalicAngle);
            output.Write(post.UnderlinePosition);
            output.Write(post.UnderlineThickness);
            output.Write(post.IsFixedPitch);
            output.Write(post.MinMemType42);
            output.Write(post.MaxMemType42);
            output.Write(post.MinMemType1);
            output.Write(post.MaxMemType1);

            // version 2.0

            // numberOfGlyphs
            output.Write((ushort)glyphIds.Count);

            // glyphNameIndex[numGlyphs]
            var names = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
            foreach (int gid in glyphIds)
            {
                string name = post.GetName(gid);
                if (WGL4Names.GetGlyphIndex(name, out int macId))
                {
                    // the name input implicit, as it's from MacRoman
                    output.Write((ushort)macId);
                }
                else
                {
                    // the name will be written explicitly
                    int ordinal = names.GetOrAdd(name, (p) => names.Count);
                    output.Write((ushort)(258 + ordinal));
                }
            }

            // names[numberNewGlyphs]
            foreach (string name in names.Keys)
            {
                byte[] buf = Charset.ASCII.GetBytes(name);
                output.WriteByte((byte)buf.Length);
                output.Write(buf);
            }

            output.Flush();
            return output;
        }

        private ByteStream BuildHmtxTable()
        {
            var output = new ByteStream(32);

            HorizontalHeaderTable h = ttf.HorizontalHeader;
            HorizontalMetricsTable hm = ttf.HorizontalMetrics;
            var input = ttf.GetOriginalData(out var pos);

            // more info: https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hmtx.html
            int lastgid = h.NumberOfHMetrics - 1;
            // true if lastgid input not in the set: we'll need its width (but not its left side bearing) later
            bool needLastGidWidth = glyphIds.LastOrDefault() > lastgid && !glyphIds.Contains(lastgid);

            try
            {
                long isResult = input.Skip(hm.Offset);

                if (isResult.CompareTo(hm.Offset) != 0)
                {
                    Debug.WriteLine($"debug: Tried skipping {hm.Offset} bytes but only {isResult} bytes skipped");
                }

                long lastOffset = 0;
                foreach (int glyphId in glyphIds)
                {
                    // offset in original file
                    long offset;
                    if (glyphId <= lastgid)
                    {
                        // copy width and lsb
                        offset = glyphId * 4L;
                        lastOffset = CopyBytes(input, output, offset, lastOffset, 4);
                    }
                    else
                    {
                        if (needLastGidWidth)
                        {
                            // one time only: copy width from lastgid, whose width applies
                            // to all later glyphs
                            needLastGidWidth = false;
                            offset = lastgid * 4L;
                            lastOffset = CopyBytes(input, output, offset, lastOffset, 2);

                            // then go on with lsb from actual glyph (lsb are individual even in monotype fonts)
                        }

                        // copy lsb only, as we are beyond numOfHMetrics
                        offset = h.NumberOfHMetrics * 4L + (glyphId - h.NumberOfHMetrics) * 2L;
                        lastOffset = CopyBytes(input, output, offset, lastOffset, 2);
                    }
                }

                return output;
            }
            finally
            {
                input.Seek(pos);
            }
        }

        private long CopyBytes(IInputStream input, Stream os, long newOffset, long lastOffset, int count)
        {
            // skip over from last original offset
            long nskip = newOffset - lastOffset;
            if (nskip != input.Skip(nskip))
            {
                throw new EndOfStreamException("Unexpected EOF exception parsing glyphId of hmtx table.");
            }
            byte[] buf = new byte[count];
            if (count != input.Read(buf, 0, count))
            {
                throw new EndOfStreamException("Unexpected EOF exception parsing glyphId of hmtx table.");
            }
            os.Write(buf, 0, count);
            return newOffset + count;
        }

        /// <summary>Write the subfont to the given output stream.</summary>
        /// <param name="os">the stream used for writing. It will be closed by this method.</param>
        public void WriteToStream(Stream os) => WriteToStream((IOutputStream)new StreamContainer(os));

        public void WriteToStream(IOutputStream output)
        {
            if (glyphIds.Count == 0 && uniToGID.Count == 0)
            {
                Debug.WriteLine("info: font subset input empty");
            }

            AddCompoundReferences();

            long[] newLoca = new long[glyphIds.Count + 1];

            // generate tables in dependency order
            var head = buildHeadTable();
            var hhea = buildHheaTable();
            var maxp = BuildMaxpTable();
            var name = BuildNameTable();
            var os2 = BuildOS2Table();
            var glyf = BuildGlyfTable(newLoca);
            var loca = BuildLocaTable(newLoca);
            var cmap = BuildCmapTable();
            var hmtx = BuildHmtxTable();
            var post = BuildPostTable();

            // save to TTF in optimized order
            Dictionary<string, ByteStream> tables = new Dictionary<string, ByteStream>(StringComparer.Ordinal);
            if (os2 != null)
            {
                tables[OS2WindowsMetricsTable.TAG] = os2;
            }
            if (cmap != null)
            {
                tables[CmapTable.TAG] = cmap;
            }
            tables[GlyphTable.TAG] = glyf;
            tables[HeaderTable.TAG] = head;
            tables[HorizontalHeaderTable.TAG] = hhea;
            tables[HorizontalMetricsTable.TAG] = hmtx;
            tables[IndexToLocationTable.TAG] = loca;
            tables[MaximumProfileTable.TAG] = maxp;
            if (name != null)
            {
                tables[NamingTable.TAG] = name;
            }
            if (post != null)
            {
                tables[PostScriptTable.TAG] = post;
            }

            // copy all other tables
            foreach (KeyValuePair<string, TTFTable> entry in ttf.TableMap)
            {
                string tag = entry.Key;
                TTFTable table = entry.Value;

                if (!tables.ContainsKey(tag) && (keepTables == null || keepTables.Contains(tag, StringComparer.Ordinal)))
                {
                    tables[tag] = new ByteStream(ttf.GetTableBytes(table));
                }
            }

            // calculate checksum
            long checksum = WriteFileHeader(output, tables.Count);
            long offset = 12L + 16L * tables.Count;
            foreach (var entry in tables)
            {
                checksum += WriteTableHeader(output, entry.Key, offset, entry.Value);
                offset += (entry.Value.Length + 3L) / 4 * 4;
            }
            checksum = 0xB1B0AFBAL - (checksum & 0xffffffffL);

            // update checksumAdjustment in 'head' table
            head.Seek(8);
            head.Write((uint)checksum);
            foreach (var bytes in tables.Values)
            {
                WriteTableBody(output, bytes);
            }
        }

        private long ToUInt32(int high, int low)
        {
            return (high & 0xffffL) << 16 | low & 0xffffL;
        }

        private int log2(int num)
        {
            return (int)Math.Floor(Math.Log(num) / Math.Log(2));
        }

        public void AddGlyphIds(ISet<int> allGlyphIds)
        {
            glyphIds.AddRange(allGlyphIds);
        }
    }
    public static class IntExtension
    {
        public static uint HighestOneBit(this uint i)
        {
            i |= (i >> 1);
            i |= (i >> 2);
            i |= (i >> 4);
            i |= (i >> 8);
            i |= (i >> 16);
            return i - (i >> 1);
        }
    }
}

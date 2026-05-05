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
using PdfGlown.Documents.Contents.Fonts.TTF;
using PdfGlown.Objects;
using PdfGlown.Tokens;
using PdfGlown.Util.Collections;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>
    /// Common functionality for embedding TrueType fonts.
    /// @author Ben Litchfield
    /// @author John Hewson
    /// </summary>
    abstract class TrueTypeEmbedder : ISubsetter
    {
        private static readonly int ITALIC = 1;
        private static readonly int OBLIQUE = 512;
        private static readonly string BASE25 = "BCDEFGHIJKLMNOPQRSTUVWXYZ";

        protected readonly PdfDocument document;
        protected TrueTypeFont ttf;
        protected FontDescriptor fontDescriptor;

        protected readonly ICmapLookup cmapLookup;
        private readonly HashSet<int> subsetCodePoints = new();
        private readonly bool embedSubset;

        private readonly HashSet<int> allGlyphIds = new();

        /// <summary>Creates a new TrueType font for embedding.</summary>
        /// <param name="document"></param>
        /// <param name="dict"></param>
        /// <param name="ttf"></param>
        /// <param name="embedSubset"></param>
        /// <exception cref="IOException"></exception>
        public TrueTypeEmbedder(PdfDocument document, PdfDictionary dict, TrueTypeFont ttf, bool embedSubset)
        {
            this.document = document;
            this.embedSubset = embedSubset;
            this.ttf = ttf;
            fontDescriptor = CreateFontDescriptor(ttf);
#if RELEASE
            if (!IsEmbeddingPermitted(ttf))
            {
                throw new IOException("This font does not permit embedding");
            }
#endif
            if (!embedSubset)
            {
                // full embedding
                // TrueType collections are not supported
                var iStream = ttf.GetOriginalData(out _);
                if (MemoryExtensions.Equals(iStream.ReadROS(4, Charset.ISO88591), "ttcf", StringComparison.Ordinal))
                {
                    throw new IOException("Full embedding of TrueType font collections not supported");
                }
                iStream.Seek(0);
                var stream = new FontFile(document, new ByteStream(iStream));
                fontDescriptor.FontFile2 = stream;
            }
            dict[PdfName.Type] = PdfName.Font;
            dict[PdfName.BaseFont] = PdfName.Get(ttf.Name);

            // choose a Unicode "cmap"
            cmapLookup = ttf.GetUnicodeCmapLookup();
        }

        public void BuildFontFile2(IOutputStream ttfStream)
        {
            var stream = new FontFile(document, ttfStream);

            // as the stream was closed within the PdfStream constructor, we have to recreate it
            var input = stream.GetInputStream();
            ttf = new TTFParser().ParseEmbedded(input);
            if (!IsEmbeddingPermitted(ttf))
            {
                throw new IOException("This font does not permit embedding");
            }
            fontDescriptor ??= CreateFontDescriptor(ttf);
            stream.Set(PdfName.Length1, ttf.OriginalDataSize);
            fontDescriptor.FontFile2 = stream;
        }

        /// <summary>Returns true if the fsType in the OS/2 table permits embedding.</summary>
        /// <param name="ttf"></param>
        /// <returns></returns>
        private static bool IsEmbeddingPermitted(TrueTypeFont ttf)
        {
            if (ttf.OS2Windows != null)
            {
                int fsType = ttf.OS2Windows.FsType;
                if ((fsType & OS2WindowsMetricsTable.FSTYPE_RESTRICTED) ==
                                 OS2WindowsMetricsTable.FSTYPE_RESTRICTED)
                {
                    // restricted License embedding
                    return false;
                }
                else if ((fsType & OS2WindowsMetricsTable.FSTYPE_BITMAP_ONLY) ==
                                     OS2WindowsMetricsTable.FSTYPE_BITMAP_ONLY)
                {
                    // bitmap embedding only
                    return false;
                }
            }
            return true;
        }

        /// <summary>Returns true if the fsType in the OS/2 table permits subsetting.</summary>
        /// <param name="ttf"></param>
        /// <returns></returns>
        private static bool IsSubsettingPermitted(TrueTypeFont ttf)
        {
            if (ttf.OS2Windows != null)

            {
                int fsType = ttf.OS2Windows.FsType;
                if ((fsType & OS2WindowsMetricsTable.FSTYPE_NO_SUBSETTING) ==
                              OS2WindowsMetricsTable.FSTYPE_NO_SUBSETTING)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Creates a new font descriptor dictionary for the given TTF.</summary>
        /// <exception cref="IOException"></exception>
        private FontDescriptor CreateFontDescriptor(TrueTypeFont ttf)
        {
            var ttfName = ttf.Name;
            OS2WindowsMetricsTable os2 = ttf.OS2Windows;
            if (os2 == null)
            {
                throw new IOException("os2 table is missing in font " + ttfName);
            }

            PostScriptTable post = ttf.PostScript;
            if (post == null)
            {
                throw new IOException("post table is missing in font " + ttfName);
            }

            var fd = new FontDescriptor(document)
            {
                FontName = ttfName
            };

            // Flags
            var hHeader = ttf.HorizontalHeader;
            var flags = (FlagsEnum)0;
            flags |= (post.IsFixedPitch > 0 || hHeader.NumberOfHMetrics == 1) ? FlagsEnum.FixedPitch : 0;

            int fsSelection = os2.FsSelection;
            flags |= ((fsSelection & (ITALIC | OBLIQUE)) != 0) ? FlagsEnum.Italic : 0;

            switch (os2.FamilyClass)
            {
                case OS2WindowsMetricsTable.FAMILY_CLASS_CLAREDON_SERIFS:
                case OS2WindowsMetricsTable.FAMILY_CLASS_FREEFORM_SERIFS:
                case OS2WindowsMetricsTable.FAMILY_CLASS_MODERN_SERIFS:
                case OS2WindowsMetricsTable.FAMILY_CLASS_OLDSTYLE_SERIFS:
                case OS2WindowsMetricsTable.FAMILY_CLASS_SLAB_SERIFS:
                    flags |= FlagsEnum.Serif;
                    break;
                case OS2WindowsMetricsTable.FAMILY_CLASS_SCRIPTS:
                    flags |= FlagsEnum.Script;
                    break;
                default:
                    break;
            }

            fd.FontWeight = os2.WeightClass;

            flags |= FlagsEnum.Symbolic;
            flags &= ~FlagsEnum.Nonsymbolic;

            fd.Flags = flags;
            // ItalicAngle
            fd.ItalicAngle = post.ItalicAngle;

            // FontBBox
            HeaderTable header = ttf.Header;
            float scaling = 1000f / header.UnitsPerEm;
            var skRect = new SKRect(
                header.XMin * scaling,
                header.YMin * scaling,
                header.XMax * scaling,
                header.YMax * scaling
                );
            fd.FontBBox = new PdfRectangle(skRect);

            // Ascent, Descent

            fd.Ascent = hHeader.Ascender * scaling;
            fd.Descent = hHeader.Descender * scaling;

            // CapHeight, XHeight
            if (os2.Version >= 1.2)
            {
                fd.CapHeight = os2.CapHeight * scaling;
                fd.XHeight = os2.Height * scaling;
            }
            else
            {
                var capHPath = ttf.GetPath("H");
                if (capHPath != null)
                {
                    fd.CapHeight = (float)Math.Round(capHPath.Bounds.Bottom * scaling);
                }
                else
                {
                    // estimate by summing the typographical +ve ascender and -ve descender
                    fd.CapHeight = (os2.TypoAscender + os2.TypoDescender) * scaling;
                }
                var xPath = ttf.GetPath("x");
                if (xPath != null)
                {
                    fd.XHeight = (float)Math.Round(xPath.Bounds.Bottom * scaling);
                }
                else
                {
                    // estimate by halving the typographical ascender
                    fd.XHeight = (os2.TypoAscender / 2.0f) * scaling;
                }
            }

            // StemV - there's no true TTF equivalent of this, so we estimate it
            fd.StemV = skRect.Width * .13f;

            return fd;
        }

        /// <summary>Returns the font descriptor.</summary>
        public FontDescriptor FontDescriptor
        {
            get => fontDescriptor;
        }

        public virtual void AddToSubset(int codePoint)
        {
            subsetCodePoints.Add(codePoint);
        }

        public void AddGlyphIds(ISet<int> glyphIds)
        {
            allGlyphIds.AddRange(glyphIds);
        }

        public virtual void Subset()
        {
            if (!IsSubsettingPermitted(ttf))
            {
                throw new IOException("This font does not permit subsetting");
            }

            if (!embedSubset)
            {
                throw new InvalidOperationException("Subsetting is disabled");
            }

            // PDF spec required tables (if present), all others will be removed
            var tables = new List<string>
            {
                "head",
                "hhea",
                "loca",
                "maxp",
                "cvt ",
                "prep",
                "glyf",
                "hmtx",
                "fpgm",
                // Windows ClearType
                "gasp"
            };

            // set the GIDs to subset
            var subsetter = new TTFSubsetter(ttf, tables);
            subsetter.AddAll(subsetCodePoints);

            if (allGlyphIds.Count > 0)
            {
                subsetter.AddGlyphIds(allGlyphIds);
            }

            // calculate deterministic tag based on the chosen subset
            Dictionary<int, int> gidToCid = subsetter.GetGIDMap();
            string tag = GetTag(gidToCid);
            subsetter.SetPrefix(tag);

            // save the subset font
            var output = new ByteStream();
            subsetter.WriteToStream((IOutputStream)output);
            output.Seek(0);
            // re-build the embedded font
            BuildSubset(output, tag, gidToCid);
            ttf.Dispose();
        }

        /// <summary>
        /// Returns true if the font needs to be subset.
        /// </summary>
        public bool NeedsSubset
        {
            get => embedSubset;
        }

        /// <summary>Rebuild a font subset.</summary>
        /// <param name="ttfSubset"></param>
        /// <param name="tag"></param>
        /// <param name="gidToCid"></param>
        protected abstract void BuildSubset(IOutputStream ttfSubset, string tag, Dictionary<int, int> gidToCid);

        /// <summary>Returns an uppercase 6-character unique tag for the given subset.</summary>
        /// <param name="gidToCid"></param>
        /// <returns></returns>
        public static string GetTag(Dictionary<int, int> gidToCid)
        {
            // deterministic
            long num = gidToCid.GetHashCode();

            // base25 encode
            var sb = new StringBuilder();
            do
            {
                long div = num / 25;
                int mod = (int)(num % 25);
                sb.Append(BASE25[mod]);
                num = div;
            } while (num != 0 && sb.Length < 6);

            // pad
            while (sb.Length < 6)
            {
                sb.Insert(0, 'A');
            }

            sb.Append('+');
            return sb.ToString();
        }
    }

}
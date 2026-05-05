/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.Fonts.TTF;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PdfGlown.Documents.Contents.Fonts
{
    ///<summary>TrueType font [PDF:1.6:5;OFF:2009].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class PdfTrueTypeFont : PdfSimpleFont
    {
        private static readonly int START_RANGE_F000 = 0xF000;
        private static readonly int START_RANGE_F100 = 0xF100;
        private static readonly int START_RANGE_F200 = 0xF200;

        private static readonly Dictionary<string, int> InvertedMacosRoman = new Dictionary<string, int>(250, StringComparer.Ordinal);
        static PdfTrueTypeFont()
        {
            foreach (var entry in MacOSRomanEncoding.Instance.CodeToNameMap)
            {
                if (!InvertedMacosRoman.ContainsKey(entry.Value))
                {
                    InvertedMacosRoman[entry.Value] = entry.Key;
                }
            }
        }

        private TrueTypeFont ttf;
        private OpenTypeFont otf;
        private bool isEmbedded;
        private bool isDamaged;
        private CmapSubtable cmapWinUnicode = null;
        private CmapSubtable cmapWinSymbol = null;
        private CmapSubtable cmapMacRoman = null;
        private bool cmapInitialized = false;
        private Dictionary<int, int> gidToCode; // for embedding
        private float? scaleFactor;

        internal PdfTrueTypeFont(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        internal override void AfterParse()
        {
            base.AfterParse();
            TrueTypeFont ttfFont = null;
            bool fontIsDamaged = false;
            if (FontDescriptor != null)
            {
                var fd = base.FontDescriptor;
                var ff2Stream = fd.FontFile2;
                if (ff2Stream != null)
                {
                    try
                    {
                        // embedded
                        using var input = ff2Stream.GetExtractedStream();
                        var ttfParser = GetParser(input, true);
                        ttfFont = ttfParser.Parse(input);
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine($"warn: Could not read embedded TTF for font {BaseFont} {e}");
                        fontIsDamaged = true;
                    }
                }
            }
            isEmbedded = ttfFont != null;
            isDamaged = fontIsDamaged;

            // substitute
            if (ttfFont == null)
            {
                FontMapping<TrueTypeFont> mapping = FontMappers.Instance.GetTrueTypeFont(BaseFont, FontDescriptor);
                ttfFont = mapping.Font;

                if (mapping.IsFallback)
                {
                    Debug.WriteLine($"warn: Using fallback font '{ttfFont}' for '{BaseFont}'");
                }
            }
            otf = ttfFont is OpenTypeFont otfFont && otfFont.IsSupportedOTF
                ? otfFont : null;
            ttf = ttfFont;
            ReadEncoding();
        }

        /// <summary>Creates a new TrueType font for embedding.</summary>
        /// <param name="document"></param>
        /// <param name="ttf"></param>
        /// <param name="encoding"></param>
        /// <param name="closeTTF"></param>
        private PdfTrueTypeFont(PdfDocument document, TrueTypeFont ttf, Encoding encoding, bool closeTTF)
            : base(document)
        {
            var embedder = new FontTrueTypeEmbedder(document, this, ttf, encoding);
            this.encoding = encoding;
            this.ttf = ttf;
            FontDescriptor = embedder.FontDescriptor;
            isEmbedded = true;
            isDamaged = false;
            glyphList = GlyphMapping.Default;
            if (closeTTF)
            {
                // the TTF is fully loaded and it is safe to close the underlying data source
                ttf.Dispose();
            }
        }

        /// <summary>
        /// Loads a TTF to be embedded into a document as a simple font.
        /// <p><b>Note:</b> Simple fonts only support 256 characters. For Unicode support, use
        /// FontType0.Load(Document, File)} instead.</p>
        /// </summary>
        /// <param name="doc">The PDF document that will hold the embedded font</param>
        /// <param name="file">A TTF file</param>
        /// <param name="encoding">The PostScript encoding vector to be used for embedding</param>
        /// <returns>a PdfTrueTypeFont instance</returns>
        public static PdfTrueTypeFont Load(PdfDocument doc, string file, Encoding encoding)
        {
            return new PdfTrueTypeFont(doc, new TTFParser().Parse(file), encoding, true);
        }

        /// <summary>
        /// Loads a TTF to be embedded into a document as a simple font.
        /// <p><b>Note:</b> Simple fonts only support 256 characters. For Unicode support, use
        /// <see cref="PdfType0Font.Load(Document, IInputStream)"/> instead.
        /// </summary>
        /// <param name="doc">The PDF document that will hold the embedded font</param>
        /// <param name="input">A TTF file stream</param>
        /// <param name="encoding">The PostScript encoding vector to be used for embedding</param>
        /// <returns>a PdfTrueTypeFont instance</returns>
        public static PdfTrueTypeFont Load(PdfDocument doc, IInputStream input, Encoding encoding)

        {
            return new PdfTrueTypeFont(doc, new TTFParser().Parse(input), encoding, true);
        }

        /// <summary>
        /// Loads a TTF to be embedded into a document as a simple font.
        /// <b>Note:</b> Simple fonts only support 256 characters. For Unicode support, use
        /// <see cref="PdfType0Font.Load(PdfDocument, IInputStream)"/> instead.
        /// </summary>
        /// <param name="doc">The PDF document that will hold the embedded font</param>
        /// <param name="ttf">A true type font</param>
        /// <param name="encoding">The PostScript encoding vector to be used for embedding</param>
        /// <returns>a PdfTrueTypeFont instance</returns>
        public static PdfTrueTypeFont Load(PdfDocument doc, TrueTypeFont ttf, Encoding encoding)
        {
            return new PdfTrueTypeFont(doc, ttf, encoding, false);
        }

        protected override Encoding ReadEncodingFromFont()
        {
            if (!IsEmbedded && Standard14AFM != null)
            {
                // read from AFM
                return new Type1Encoding(Standard14AFM);
            }
            else
            {
                // non-symbolic fonts don't have a built-in encoding per se, but there encoding is
                // assumed to be StandardEncoding by the PDF spec unless an explicit Encoding is present
                // which will override this anyway
                if (!(SymbolicFlag ?? true))
                {
                    return StandardEncoding.Instance;
                }

                // normalise the standard 14 name, e.g "Symbol,Italic" -> "Symbol"
                var standard14Name = Standard14Fonts.GetMappedFontName(Name);

                // likewise, if the font is standard 14 then we know it's Standard Encoding
                if (IsStandard14
                    && standard14Name != FontName.Symbol
                    && standard14Name != FontName.ZapfDingbats)
                {
                    return StandardEncoding.Instance;
                }

                // synthesize an encoding, so that getEncoding() is always usable
                PostScriptTable post = ttf.PostScript;
                var codeToName = new BuiltInEncoding();
                for (int code = 0; code <= 256; code++)
                {
                    int gid = CodeToGID(code);
                    if (gid > 0)
                    {
                        string name = null;
                        if (post != null)
                        {
                            name = post.GetName(gid);
                        }
                        if (name == null)
                        {
                            // GID pseudo-name
                            name = gid.ToString();
                        }
                        codeToName.Put(code, name);
                    }
                }
                return codeToName;
            }
        }

        public override int ReadCode(IInputStream input, out ReadOnlySpan<byte> bytes)
        {
            bytes = input.ReadSpan(1);
            return ReadCode(bytes);
        }

        public override int ReadCode(ReadOnlySpan<byte> bytes)
        {
            return bytes[0];
        }

        public override string Name
        {
            get => BaseFont;
        }

        public override bool IsDamaged
        {
            get => isDamaged;
        }

        /// <summary>Returns the embedded or substituted TrueType font.</summary>
        public TrueTypeFont TrueTypeFont
        {
            get => ttf;
        }

        protected override SKRect GenerateBBox()
        {
            return GetDefaultBBox() ?? ttf.FontBBox;
        }

        public override float ScalingFactor => scaleFactor ??= GenerateScaleFactor();

        private float GenerateScaleFactor()
        {
            //if (ttf is TrueTypeFont)
            //{
            //    return 1000 / ttf.UnitsPerEm;
            //}
            return base.ScalingFactor;
        }

        public override float GetWidthFromFont(int code)
        {
            int gid = CodeToGID(code);
            float width = ttf.GetAdvanceWidth(gid);
            float unitsPerEM = ttf.UnitsPerEm;
            if (unitsPerEM.CompareTo(1000) != 0)
            {
                width *= 1000f / unitsPerEM;
            }
            return width;
        }

        public override float GetHeight(int code)
        {
            int gid = CodeToGID(code);
            GlyphData glyph = ttf.Glyph.GetGlyph(gid);
            if (glyph != null)
            {
                return glyph.BoundingBox.Height;
            }
            return 0;
        }

        public override int GetBytesCount(int code) => 1;

        public override void Encode(Span<byte> bytes, int unicode)
        {
            string name = GlyphList.UnicodeToName(unicode);

            if (encoding != null)
            {
                if (!encoding.Contains(name))
                {
                    throw new ArgumentException($"U+{unicode:x4} is not available in this font's encoding: {encoding.GetPdfObject()}");
                }

                var inverted = encoding.NameToCodeMap;

                if (!ttf.HasGlyph(name))
                {
                    // try unicode name
                    string uniName = UniUtil.GetUniNameOfCodePoint(unicode);
                    if (!ttf.HasGlyph(uniName))
                    {
                        throw new ArgumentException($"No glyph for U+{unicode:x4} in font {Name}");
                    }
                }

                inverted.TryGetValue(name, out int code);
                bytes[0] = (byte)code;
            }
            else
            {
                // use TTF font's built-in encoding
                if (!ttf.HasGlyph(name))
                {
                    throw new ArgumentException($"No glyph for U+{unicode:x4} in font {Name}");
                }

                int gid = ttf.NameToGID(name);
                if (!GIDToCode.TryGetValue(gid, out int code))
                {
                    throw new ArgumentException($"U+{unicode:x4} is not available in this font's Encoding");
                }

                bytes[0] = (byte)code;
            }
        }

        /// <summary>Inverts the font's code -&gt; GID mapping. Any duplicate (GID -&gt; code) mappings will be lost.</summary>
        private Dictionary<int, int> GIDToCode
        {
            get
            {
                if (gidToCode != null)
                {
                    return gidToCode;
                }

                gidToCode = new Dictionary<int, int>();
                for (int code = 0; code <= 255; code++)
                {
                    int gid = CodeToGID(code);
                    if (!gidToCode.ContainsKey(gid))
                    {
                        gidToCode[gid] = code;
                    }
                }
                return gidToCode;
            }
        }

        public override bool IsEmbedded
        {
            get => isEmbedded;
        }

        public override SKPath GetPath(int code)
        {
            if (otf?.IsPostScript ?? false)
            {
                return GetPathFromOutlines(code);
            }
            int gid = CodeToGID(code);
            GlyphData glyph = ttf.Glyph.GetGlyph(gid);

            // some glyphs have no outlines (e.g. space, table, newline)
            return glyph?.GetPath();
        }

        public override SKPath GetPath(string name)
        {
            // handle glyph names and uniXXXX names
            int gid = ttf.NameToGID(name);
            if (gid == 0)
            {
                try
                {
                    // handle GID pseudo-names
                    gid = int.Parse(name);
                    if (gid > ttf.NumberOfGlyphs)
                    {
                        gid = 0;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"error: {e} on GetPath");
                    gid = 0;
                }
            }
            // I'm assuming .notdef paths are not drawn, as it PDFBOX-2421
            if (gid == 0)
            {
                return null;
            }

            GlyphData glyph = ttf.Glyph.GetGlyph(gid);
            return glyph?.GetPath();
        }

        public override SKPath GetNormalizedPath(int code)
        {
            if (!cacheGlyphs.TryGetValue(code, out SKPath path))
            {
                if (otf?.IsPostScript ?? false)
                {
                    path = GetPathFromOutlines(code);
                }
                else
                {
                    int gid = CodeToGID(code);
                    path = GetPath(code);
                    // Acrobat only draws GID 0 for embedded or "Standard 14" fonts, see PDFBOX-2372
                    if (gid == 0 && !IsEmbedded && !IsStandard14)
                    {
                        path = null;
                    }
                }
                //check empty glyph (e.g. space, newline)
                if (path != null && ttf.UnitsPerEm != 1000)
                {
                    float scale = 1000f / ttf.UnitsPerEm;

                    var scaledPath = new SKPath(path);
                    scaledPath.Transform(SKMatrix.CreateScale(scale, scale));
                    path = scaledPath;
                }
                cacheGlyphs[code] = path;
            }
            return path;
        }

        private SKPath GetPathFromOutlines(int code)
        {
            var cffFont = otf.CFF.Font;
            var name = Encoding.GetName(code);
            int sid = cffFont.Charset.GetSID(name);
            int gid = cffFont.Charset.GetGIDForSID(sid);
            var type2CharString = cffFont.GetType2CharString(gid);
            return type2CharString?.Path;
        }

        public override bool HasGlyph(string name)
        {
            int gid = ttf.NameToGID(name);
            return !(gid == 0 || gid >= ttf.MaximumProfile.NumGlyphs);
        }

        public override BaseFont Font
        {
            get => ttf;
        }

        public override bool HasGlyph(int code)
        {
            return CodeToGID(code) != 0;
        }

        /// <summary>Returns the GID for the given character code.</summary>
        /// <param name="code">character code</param>
        /// <returns>GID (glyph index)</returns>
        public int CodeToGID(int code)
        {
            ExtractCmapTable();
            int gid = 0;

            if (!Symbolic) // non-symbolic
            {
                string name = encoding.GetName(code);
                if (name == null || ".notdef".Equals(name, StringComparison.Ordinal))
                {
                    return 0;
                }
                else
                {
                    // (3, 1) - (Windows, Unicode)
                    if (cmapWinUnicode != null)
                    {
                        var unicode = GlyphMapping.Default.ToUnicode(name);
                        if (unicode != null)
                        {
                            //int uni = unicode.codePointAt(0);
                            gid = cmapWinUnicode.GetGlyphId((int)unicode);
                        }
                    }

                    // (1, 0) - (Macintosh, Roman)
                    if (gid == 0 && cmapMacRoman != null)
                    {
                        if (InvertedMacosRoman.TryGetValue(name, out int macCode))
                        {
                            gid = cmapMacRoman.GetGlyphId(macCode);
                        }
                    }

                    // 'post' table
                    if (gid == 0)
                    {
                        gid = ttf.NameToGID(name);
                    }
                }
            }
            else // symbolic
            {
                // PDFBOX-4755 / PDF.js #5501
                // PDFBOX-3965: fallback for font has that the symbol flag but isn't
                if (cmapWinUnicode != null)
                {
                    if (encoding is WinAnsiEncoding || encoding is MacRomanEncoding)
                    {
                        var name = encoding.GetName(code);
                        if (string.Equals(".notdef", name, StringComparison.Ordinal))
                        {
                            return 0;
                        }
                        var unicode = GlyphMapping.Default.ToUnicode(name);
                        if (unicode != null)
                        {
                            gid = cmapWinUnicode.GetGlyphId(unicode.Value);
                        }
                    }
                    else
                    {
                        gid = cmapWinUnicode.GetGlyphId(code);
                    }
                }
                // (3, 0) - (Windows, Symbol)
                if (gid == 0 && cmapWinSymbol != null)
                {
                    gid = cmapWinSymbol.GetGlyphId(code);
                    if (code >= 0 && code <= 0xFF)
                    {
                        // the CMap may use one of the following code ranges,
                        // so that we have to add the high byte to get the
                        // mapped value
                        if (gid == 0)
                        {
                            // F000 - F0FF
                            gid = cmapWinSymbol.GetGlyphId(code + START_RANGE_F000);
                        }
                        if (gid == 0)
                        {
                            // F100 - F1FF
                            gid = cmapWinSymbol.GetGlyphId(code + START_RANGE_F100);
                        }
                        if (gid == 0)
                        {
                            // F200 - F2FF
                            gid = cmapWinSymbol.GetGlyphId(code + START_RANGE_F200);
                        }
                    }
                }

                // (1, 0) - (Mac, Roman)
                if (gid == 0 && cmapMacRoman != null)
                {
                    gid = cmapMacRoman.GetGlyphId(code);
                }

            }

            return gid;
        }

        ///<summary>extract all useful "cmap" subtables.</summary>
        private void ExtractCmapTable()
        {
            if (cmapInitialized)
            {
                return;
            }

            CmapTable cmapTable = ttf.Cmap;
            if (cmapTable != null)
            {
                // get all relevant "cmap" subtables
                CmapSubtable[] cmaps = cmapTable.Cmaps;
                foreach (CmapSubtable cmap in cmaps)
                {
                    if (CmapTable.PLATFORM_WINDOWS == cmap.PlatformId)
                    {
                        if (CmapTable.ENCODING_WIN_UNICODE_BMP == cmap.PlatformEncodingId)
                        {
                            cmapWinUnicode = cmap;
                        }
                        else if (CmapTable.ENCODING_WIN_SYMBOL == cmap.PlatformEncodingId)
                        {
                            cmapWinSymbol = cmap;
                        }
                    }
                    else if (CmapTable.PLATFORM_MACINTOSH == cmap.PlatformId
                            && CmapTable.ENCODING_MAC_ROMAN == cmap.PlatformEncodingId)
                    {
                        cmapMacRoman = cmap;
                    }
                    else if (CmapTable.PLATFORM_UNICODE == cmap.PlatformId
                            && CmapTable.ENCODING_UNICODE_1_0 == cmap.PlatformEncodingId)
                    {
                        // PDFBOX-4755 / PDF.js #5501
                        cmapWinUnicode = cmap;
                    }
                    else if (CmapTable.PLATFORM_UNICODE == cmap.PlatformId
                        && CmapTable.ENCODING_UNICODE_2_0_BMP == cmap.PlatformEncodingId)
                    {
                        // PDFBOX-5484
                        cmapWinUnicode = cmap;
                    }
                }
            }
            cmapInitialized = true;
        }

        private TTFParser GetParser(IInputStream input, bool isEmbedded)
        {
            long startPos = input.Position;
            var testString = input.ReadString(4, System.Text.Encoding.ASCII);
            input.Seek(startPos);
            if (string.Equals("OTTO", testString, StringComparison.Ordinal))
            {
                return new OTFParser(isEmbedded);
            }
            else
            {
                return new TTFParser(isEmbedded);
            }
        }
    }
}
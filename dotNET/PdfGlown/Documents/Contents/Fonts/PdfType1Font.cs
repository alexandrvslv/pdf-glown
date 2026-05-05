/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.Fonts.Type1;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>Type 1 font [PDF:1.6:5.5.1;AFM:4.1].</summary>
    // NOTE: Type 1 fonts encompass several formats:
    // * AFM+PFB;
    // * CFF;
    // * OpenFont/CFF (in case "CFF" table's Top DICT has no CIDFont operators).
    [Pdf(VersionEnum.PDF10)]
    public class PdfType1Font : PdfSimpleFont
    {
        public enum FamilyEnum
        {
            Courier,
            Helvetica,
            Times,
            Symbol,
            ZapfDingbats
        };

        public static PdfType1Font Load(PdfDocument context, FamilyEnum family, bool bold, bool italic)
        {
            string fontName = family.ToString();
            switch (family)
            {
                case (FamilyEnum.Symbol):
                case (FamilyEnum.ZapfDingbats):
                    break;
                case (FamilyEnum.Times):
                    if (bold)
                    {
                        fontName += "-Bold";
                        if (italic)
                        { fontName += "Italic"; }
                    }
                    else if (italic)
                    { fontName += "-Italic"; }
                    else
                    { fontName += "-Roman"; }
                    break;
                default:
                    if (bold)
                    {
                        fontName += "-Bold";
                        if (italic)
                        { fontName += "Oblique"; }
                    }
                    else if (italic)
                    { fontName += "-Oblique"; }
                    break;
            }
            var name = Standard14Fonts.GetMappedFontName(fontName);

            return Load(context, name);
        }

        public static PdfType1Font Load(PdfDocument context, FontName name)
        {
            return context.Type1FontCache.GetOrAdd(name, (n) => new PdfType1Font(context, n));
        }

        // alternative names for glyphs which are commonly encountered
        private static readonly Dictionary<string, string> ALT_NAMES = new(StringComparer.Ordinal)
        {
            { "ff", "f_f" },
            { "ffi", "f_f_i" },
            { "ffl", "f_f_l" },
            { "fi", "f_i" },
            { "fl", "f_l" },
            { "st", "s_t" },
            { "IJ", "I_J" },
            { "ij", "i_j" },
            { "ellipsis", "elipsis" }, // misspelled in ArialMT
        };
        private static readonly int PFB_START_MARKER = 0x80;

        private Type1Font type1font;
        private BaseFont genericFont;
        private bool isEmbedded;
        private bool isDamaged;
        private SKMatrix normMatrix;
        private Dictionary<int, byte[]> codeToBytesMap = new();

        internal PdfType1Font(PdfDocument context)
            : base(context)
        { }

        internal PdfType1Font(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public PdfType1Font(PdfDocument context, FontName baseFont) : base(context, baseFont)
        {
            Set(PdfName.Subtype, PdfName.Type1);
            Set(PdfName.BaseFont, PdfName.Get(Standard14Fonts.FontNames[baseFont]));
            switch (baseFont)
            {
                case FontName.ZapfDingbats:
                    encoding = ZapfDingbatsEncoding.Instance;
                    break;
                case FontName.Symbol:
                    encoding = SymbolEncoding.Instance;
                    break;
                default:
                    encoding = WinAnsiEncoding.Instance;
                    Set(PdfName.Encoding, PdfName.WinAnsiEncoding);
                    break;
            }

            // todo: could load the PFB font here if we wanted to support Standard 14 embedding
            type1font = null;
            var mapping = FontMappers.Instance.GetBaseFont(BaseFont, FontDescriptor);
            genericFont = mapping.Font;

            if (mapping.IsFallback)
            {
                string fontName;
                try
                {
                    fontName = genericFont.Name;
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"debug: Couldn't get font name - setting to '?' {e}");
                    fontName = "?";
                }
                Debug.WriteLine($"warn: Using fallback font {fontName} for base font {BaseFont}");
            }
            isEmbedded = false;
            isDamaged = false;
            normMatrix = SKMatrix.Identity;
        }

        public PdfType1Font(PdfDocument doc, IInputStream pfbIn)
            : this(doc, pfbIn, null)
        { }

        public PdfType1Font(PdfDocument doc, IInputStream pfbIn, Encoding encoding)
            : base(doc)
        {
            var embedder = new FontType1Embedder(doc, this, pfbIn, encoding);
            this.encoding = encoding ?? embedder.FontEncoding;
            glyphList = embedder.GlyphList;
            type1font = embedder.Type1Font;
            genericFont = embedder.Type1Font;
            isEmbedded = true;
            isDamaged = false;
            codeToBytesMap = new Dictionary<int, byte[]>();
            normMatrix = SKMatrix.Identity;
        }

        internal override void AfterParse()
        {
            base.AfterParse();
            var fd = FontDescriptor;
            Type1Font t1 = null;
            bool fontIsDamaged = false;
            if (fd != null)
            {
                // a Type1 font may contain a Type1C font
                var fontFile3 = fd.FontFile3;
                if (fontFile3 != null)
                {
                    Debug.WriteLine("warn: /FontFile3 for Type1 font not supported");
                }

                // or it may contain a PFB
                var fontFile = fd.FontFile;
                if (fontFile != null)
                {
                    try
                    {
                        int length1 = fontFile.Length1;
                        int length2 = fontFile.Length2;

                        // repair Length1 and Length2 if necessary
                        var bytes = fontFile.GetExtractedStream().AsMemory();
                        if (bytes.Length == 0)
                        {
                            throw new IOException("Font data unavailable");
                        }
                        var span = bytes.Span;
                        length1 = RepairLength1(span, length1);
                        length2 = RepairLength2(span, length1, length2);

                        if ((span[0] & 0xff) == PFB_START_MARKER)
                        {
                            // some bad files embed the entire PFB, see PDFBOX-2607
                            t1 = Type1Font.CreateWithPFB(bytes);
                        }
                        else
                        {
                            // the PFB embedded as two segments back-to-back
                            var segment1 = bytes.Slice(0, length1);
                            var segment2 = bytes.Slice(length1, length2);

                            // empty streams are simply ignored
                            if (length1 > 0 && length2 > 0)
                            {
                                t1 = Type1Font.CreateWithSegments(segment1, segment2);
                            }
                        }
                    }
                    catch (DamagedFontException e)
                    {
                        Debug.WriteLine($"warn: Can't read damaged embedded Type1 font {fd.FontName} {e}");
                        fontIsDamaged = true;
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine($"error: Can't read the embedded Type1 font {fd.FontName} {e}");
                        fontIsDamaged = true;
                    }
                }
            }
            isEmbedded = t1 != null;
            isDamaged = fontIsDamaged;
            type1font = t1;

            // find a generic font to use for rendering, could be a .pfb, but might be a .ttf
            if (type1font != null)
            {
                genericFont = type1font;
            }
            else
            {
                var mapping = FontMappers.Instance.GetBaseFont(BaseFont, fd);
                genericFont = mapping.Font;

                if (mapping.IsFallback)
                {
                    Debug.WriteLine($"warn Using fallback font {genericFont.Name} for {BaseFont}");
                }
            }
            ReadEncoding();
            normMatrix = FontMatrix.PreConcat(SKMatrix.CreateScale(1000, 1000));
        }

        /// <summary>
        /// Some Type 1 fonts have an invalid Length1, which causes the binary segment of the font
        /// to be truncated, see PDFBOX-2350, PDFBOX-3677.
        /// </summary>
        /// <param name="bytes">Type 1 stream bytes</param>
        /// <param name="length1">Length1 from the Type 1 stream</param>
        /// <returns>repaired Length1 value</returns>

        internal static int RepairLength1(ReadOnlySpan<byte> bytes, int length1)
        {
            // scan backwards from the end of the first segment to find 'exec'
            int offset = Math.Max(0, length1 - 4);
            if (offset <= 0 || offset > bytes.Length - 4)
            {
                offset = bytes.Length - 4;
            }

            offset = FindBinaryOffsetAfterExec(bytes, offset);
            if (offset == 0 && length1 > 0)
            {
                // 2nd try with brute force
                offset = FindBinaryOffsetAfterExec(bytes, bytes.Length - 4);
            }

            if (length1 - offset != 0 && offset > 0)
            {
                Debug.WriteLine($"warn: Ignored invalid Length1 {length1} for Type 1 font");
                return offset;
            }

            return length1;
        }

        private static int FindBinaryOffsetAfterExec(ReadOnlySpan<byte> bytes, int startOffset)
        {
            int offset = startOffset;
            while (offset > 0)
            {
                if (bytes[offset + 0] == 'e'
                        && bytes[offset + 1] == 'x'
                        && bytes[offset + 2] == 'e'
                        && bytes[offset + 3] == 'c')
                {
                    offset += 4;
                    // skip additional CR LF space characters
                    while (offset < bytes.Length &&
                            (bytes[offset] == '\r' || bytes[offset] == '\n' ||
                             bytes[offset] == ' ' || bytes[offset] == '\t'))
                    {
                        offset++;
                    }
                    break;
                }
                offset--;
            }
            return offset;
        }

        /// <summary>
        /// Some Type 1 fonts have an invalid Length2, see PDFBOX-3475. A negative /Length2 brings an
        /// ArgumentException in Arrays.copyOfRange(), a huge value eats up memory because of
        /// padding.
        /// </summary>
        /// <param name="bytes">Type 1 stream bytes</param>
        /// <param name="length1">Length1 from the Type 1 stream</param>
        /// <param name="length2">Length2 from the Type 1 stream</param>
        /// <returns>repaired Length2 value</returns>
        internal static int RepairLength2(ReadOnlySpan<byte> bytes, int length1, int length2)
        {
            // repair Length2 if necessary
            if (length2 < 0 || length2 > bytes.Length - length1)
            {
                Debug.WriteLine($"warn: Ignored invalid Length2 {length2} for Type 1 font");
                return bytes.Length - length1;
            }
            return length2;
        }

        public override SKMatrix? NormMatrix => normMatrix;

        public override bool IsEmbedded
        {
            get => isEmbedded;
        }

        public override float AverageFontWidth
        {
            get => Standard14AFM?.GetAverageCharacterWidth() ?? base.AverageFontWidth;
        }

        /// <summary>Returns the embedded or substituted Type 1 font, or null if there is none.</summary>
        public Type1Font Type1Font
        {
            get => type1font;
        }

        public override BaseFont Font
        {
            get => genericFont;
        }

        public override string Name
        {
            get => BaseFont;
        }

        public override bool IsDamaged
        {
            get => isDamaged;
        }

        public override float GetHeight(int code)
        {
            if (Standard14AFM != null)
            {
                string afmName = Encoding.GetName(code);
                return Standard14AFM.GetCharacterHeight(afmName); // todo: isn't this the y-advance, not the height?
            }
            else
            {
                string name = CodeToName(code);
                // todo: should be scaled by font matrix
                return (float)genericFont.GetPath(name).Bounds.Height;
            }
        }

        public override int GetBytesCount(int code) => codeToBytesMap.TryGetValue(code, out var bytes) ? bytes.Length : 1;

        public override void Encode(Span<byte> bytes, int unicode)
        {
            if (codeToBytesMap.TryGetValue(unicode, out byte[] exist))
            {
                exist.CopyTo(bytes);
            }

            string name = GlyphList.UnicodeToName(unicode);
            if (IsStandard14)
            {
                // genericFont not needed, thus simplified code
                // this is important on systems with no installed fonts
                if (!encoding.Contains(name))
                {
                    Debug.WriteLine($"U+{unicode:x4} ('{name}') is not available in this font {Name} encoding: {encoding.GetPdfObject()}");
                }
                if (".notdef".Equals(name, StringComparison.Ordinal))
                {
                    Debug.WriteLine($"No glyph for U+{unicode:x4} in font {Name}");
                }
            }
            else
            {
                if (!encoding.Contains(name))
                {
                    Debug.WriteLine($"U+{unicode:x4} ({name}) is not available in this font {Name} (generic: {genericFont.Name}) encoding: {encoding.GetPdfObject()}");
                }

                string nameInFont = GetNameInFont(name);

                if (".notdef".Equals(nameInFont, StringComparison.Ordinal) || !genericFont.HasGlyph(nameInFont))
                {
                    Debug.WriteLine($"No glyph for U+{unicode:x4} in font {Name} (generic: {genericFont.Name})");
                }
            }

            var inverted = encoding.NameToCodeMap;
            int code = inverted.TryGetValue(name, out var nameCode) ? nameCode : 0;
            var newBytes = new byte[] { (byte)code };
            codeToBytesMap[unicode] = newBytes;
            newBytes.CopyTo(bytes);
        }

        public override float GetWidthFromFont(int code)
        {
            string name = CodeToName(code);

            // width of .notdef is ignored for substitutes, see PDFBOX-1900
            if (!isEmbedded && name.Equals(".notdef", StringComparison.Ordinal))
            {
                return 250;
            }
            float width = genericFont.GetWidth(name);

            var p = normMatrix.MapVector(width, 0);
            return p.X;
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

        protected override Encoding ReadEncodingFromFont()
        {
            if (!IsEmbedded && Standard14AFM != null)
            {
                // read from AFM
                return new Type1Encoding(Standard14AFM);
            }
            else
            {
                // extract from Type1 font/substitute
                if (genericFont is IEncodedFont encodedFont)
                {
                    return Type1Encoding.FromFontBox(encodedFont.Encoding);
                }
                else
                {
                    // default (only happens with TTFs)
                    return StandardEncoding.Instance;
                }
            }
        }

        public string CodeToName(int code)
        {
            string name = Encoding?.GetName(code) ?? ".notdef";
            return GetNameInFont(name);
        }

        /// <summary>
        /// Maps a PostScript glyph name to the name in the underlying font, for example when
        /// using a TTF font we might map "W" to "uni0057".
        /// </summary>
        private string GetNameInFont(string name)
        {
            if (IsEmbedded || genericFont.HasGlyph(name))
            {
                return name;
            }

            // try alternative name
            if (ALT_NAMES.TryGetValue(name, out string altName)
                && !name.Equals(".notdef", StringComparison.Ordinal)
                && genericFont.HasGlyph(altName))
            {
                return altName;
            }

            // try unicode name
            var unicodes = GlyphList.ToUnicode(name);
            if (unicodes != null && unicodes.Value <= ushort.MaxValue)
            {
                string uniName = UniUtil.GetUniNameOfCodePoint((int)unicodes);
                if (genericFont.HasGlyph(uniName))
                {
                    return uniName;
                }
                // PDFBOX-4017: no postscript table on Windows 10, and the low uni00NN
                // names are not found in Symbol font. What works is using the PDF code plus 0xF000
                // while disregarding encoding from the PDF (because of file from PDFBOX-1606,
                // makes sense because this segment is about finding the name in a standard font)
                //TODO bring up better solution than this
                if ("SymbolMT".Equals(genericFont.Name, StringComparison.Ordinal))
                {
                    if (SymbolEncoding.Instance.NameToCodeMap.TryGetValue(name, out int code))
                    {
                        uniName = UniUtil.GetUniNameOfCodePoint(code + 0xF000);
                        if (genericFont.HasGlyph(uniName))
                        {
                            return uniName;
                        }
                    }
                }
            }

            return ".notdef";
        }

        public override SKPath GetPath(string name)
        {
            if (name == null)
                return null;
            // Acrobat does not draw .notdef for Type 1 fonts, see PDFBOX-2421
            // I suspect that it does do this for embedded fonts though, but this is untested
            if (name.Equals(".notdef") && !isEmbedded)
            {
                return null;
            }
            else
            {
                return genericFont.GetPath(GetNameInFont(name));
            }
        }

        public override SKPath GetPath(int code)
        {
            string name = Encoding.GetName(code);
            return GetPath(name);
        }

        public override SKPath GetNormalizedPath(int code)
        {
            if (!cacheGlyphs.TryGetValue(code, out SKPath path))
            {
                cacheGlyphs[code] = path = GetPath(code);
            }
            return path;
        }

        public override bool HasGlyph(string name)
        {
            return genericFont.HasGlyph(GetNameInFont(name));
        }

        public override bool HasGlyph(int code)
        {
            return !Encoding.GetName(code).Equals(".notdef", StringComparison.Ordinal);
        }

        protected override SKRect GenerateBBox()
        {
            return GetDefaultBBox() ?? genericFont.FontBBox;
        }

        protected override SKMatrix GenerateFontMatrix()
        {
            // PDF specified that Type 1 fonts use a 1000upem matrix, but some fonts specify
            // their own custom matrix anyway, for example PDFBOX-2298
            List<float> numbers = null;
            try
            {
                numbers = genericFont.FontMatrix;
            }
            catch (IOException e)
            {
                Debug.WriteLine("debug: Couldn't get font matrix box - returning default value", e);
                return DefaultFontMatrix;
            }

            if (numbers != null && numbers.Count > 5)
            {
                return new SKMatrix(
                        numbers[0], numbers[1], numbers[4],
                        numbers[2], numbers[3], numbers[5],
                        0f, 0f, 1f);
            }
            else
            {
                return DefaultFontMatrix;
            }
        }

        protected override float GetAscent() => Standard14AFM?.Ascender is float value && value > 10
            ? value
            : base.GetAscent();

        protected override float GetDescent() => Standard14AFM?.Descender is float value && value < -10
            ? value
            : base.GetDescent();


    }
}
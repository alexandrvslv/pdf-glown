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
using PdfGlown.Documents.Contents.Fonts.CCF;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>Type 1-equivalent CFF font.</summary>
    /// <remarks> 
    /// * @author Villu Ruusmann
    /// * @author John Hewson
    /// </remarks>
    public class PdfType1CFont : PdfSimpleFont
    {
        private readonly Dictionary<string, float> glyphHeights = new(StringComparer.Ordinal);
        private SKMatrix fontMatrixTransform;
        private CFFType1Font cffFont; // embedded font
        private BaseFont genericFont; // embedded or system font for rendering
        private bool isEmbedded;
        private bool isDamaged;
        private float? avgWidth = null;

        internal PdfType1CFont(Dictionary<PdfName, PdfDirectObject> fontDictionary)
            : base(fontDictionary)
        { }

        internal override void AfterParse()
        {
            base.AfterParse();
            var fontIsDamaged = false;
            CFFType1Font cffEmbedded = null;
            FontDescriptor fd = FontDescriptor;
            if (fd != null)
            {
                var ff3Stream = fd.FontFile3;
                if (ff3Stream != null)
                {
                    try
                    {
                        using var bytes = ff3Stream.GetExtractedStream();
                        if (bytes.Length == 0)
                        {
                            Debug.WriteLine($"error: Invalid data for embedded Type1C font {Name}");
                        }
                        else
                        {
                            // note: this could be an OpenType file, fortunately CFFParser can handle that
                            var cffParser = new CFFParser();
                            var parsed = cffParser.Parse(bytes).FirstOrDefault();
                            if (parsed is CFFType1Font parsedCCF)
                            {
                                cffEmbedded = parsedCCF;
                            }
                            else
                            {
                                Debug.WriteLine($"error: Expected CFFType1Font, got {parsed?.GetType().Name ?? "null"}");
                                fontIsDamaged = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"error: Can't read the embedded Type1C font {Name} {e}");
                        fontIsDamaged = true;
                    }
                }
            }

            isDamaged = fontIsDamaged;
            cffFont = cffEmbedded;
            if (cffFont != null)
            {
                genericFont = cffFont;
                isEmbedded = true;
            }
            else
            {
                FontMapping<BaseFont> mapping = FontMappers.Instance.GetBaseFont(BaseFont, fd);
                genericFont = mapping.Font;

                if (mapping.IsFallback)
                {
                    Debug.WriteLine($"warn: Using fallback font {genericFont.Name} for {BaseFont}");
                }
                isEmbedded = false;
            }
            ReadEncoding();
            fontMatrixTransform = FontMatrix;
            fontMatrixTransform = fontMatrixTransform.PreConcat(SKMatrix.CreateScale(1000, 1000));
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

        public override bool IsEmbedded
        {
            get => isEmbedded;
        }

        public override float AverageFontWidth
        {
            get
            {
                if (avgWidth == null)
                {
                    avgWidth = AverageCharacterWidth;
                }
                return (float)avgWidth;
            }
        }

        /// <summary>
        /// Returns the embedded Type 1-equivalent CFF font.
        /// </summary>
        /// <value>the cffFont</value>
        public CFFType1Font CFFType1Font
        {
            get => cffFont;
        }

        // todo: this is a replacement for FontMetrics method
        private float AverageCharacterWidth
        {
            // todo: not implemented, highly suspect
            get => 500;
        }

        protected override SKMatrix GenerateFontMatrix()
        {
            List<float> numbers = null;
            try
            {
                numbers = genericFont.FontMatrix;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"debug: Couldn't get font matrix - returning default value {e}");
                return DefaultFontMatrix;
            }

            if (numbers != null && numbers.Count > 5)
            {
                return new SKMatrix(
                        numbers[0], numbers[1], numbers[4],
                        numbers[2], numbers[3], numbers[5],
                        0, 0, 1);
            }
            else
            {
                return DefaultFontMatrix;
            }
        }

        protected override SKRect GenerateBBox()
        {
            return GetDefaultBBox() ?? genericFont.FontBBox;
        }

        public override SKPath GetPath(string name)
        {
            // Acrobat only draws .notdef for embedded or "Standard 14" fonts, see PDFBOX-2372
            if (name.Equals(".notdef", StringComparison.Ordinal) && !IsEmbedded && !IsStandard14)
            {
                return null;
            }
            if ("nbspace".Equals(name, StringComparison.Ordinal))
            {
                if (!HasGlyph("space"))
                {
                    return null;
                }
                return genericFont.GetPath("space");
            }
            if ("sfthyphen".Equals(name, StringComparison.Ordinal))
            {
                return genericFont.GetPath("hyphen");
            }
            return genericFont.GetPath(name);
        }

        public override bool HasGlyph(int code)
        {
            string name = Encoding.GetName(code);
            name = GetNameInFont(name);
            return "nbspace".Equals(name, StringComparison.Ordinal)
                ? HasGlyph("space")
                : "sfthyphen".Equals(name, StringComparison.Ordinal)
                    ? HasGlyph("hyphen")
                    : HasGlyph(name);
        }

        public override SKPath GetPath(int code)
        {
            string name = Encoding.GetName(code);
            if (name == null)
                return null;
            name = GetNameInFont(name);
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
            return genericFont.HasGlyph(name);
        }

        //override
        public string CodeToName(int code)
        {
            return Encoding.GetName(code);
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


        public override int ReadCode(IInputStream input, out ReadOnlySpan<byte> bytes)
        {
            bytes = input.ReadSpan(1);
            return ReadCode(bytes);
        }

        public override int ReadCode(ReadOnlySpan<byte> bytes)
        {
            return bytes[0];
        }

        public override float GetWidthFromFont(int code)
        {
            string name = CodeToName(code);
            name = GetNameInFont(name);
            float width = genericFont.GetWidth(name);

            var p = fontMatrixTransform.MapVector(width, 0);
            return p.X;
        }


        public override float GetHeight(int code)
        {
            string name = CodeToName(code);
            if (name == null)
            {
                return (float)(Ascent - Descent);
            }
            if (!glyphHeights.TryGetValue(name, out float height))
            {
                if (cffFont == null)
                {
                    Debug.WriteLine("warn: No embedded CFF font, returning 0");
                    return 0;
                }
                glyphHeights[name] = height = cffFont.GetType1CharString(name).Bounds.Height;
            }
            return height;
        }

        public override int GetBytesCount(int code) => 1;

        public override void Encode(Span<byte> bytes, int unicode)
        {
            string name = GlyphList.UnicodeToName(unicode);
            if (name != "space" && !encoding.Contains(name))
            {
                throw new ArgumentException($"U+{unicode:x4} ('{name}') is not available in this font's encoding: {encoding.GetPdfObject()}");
            }

            string nameInFont = GetNameInFont(name);

            var inverted = encoding.NameToCodeMap;

            if (nameInFont.Equals(".notdef", StringComparison.Ordinal) 
                || nameInFont != "space" && !genericFont.HasGlyph(nameInFont))
            {
                throw new ArgumentException($"No glyph for U+{unicode:x4} in font {Name}");
            }

            inverted.TryGetValue(name, out int code);
            bytes[0] = (byte)code;
        }

        public override float GetWidth(string text)
        {
            float width = 0;
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint = text[i];
                string name = GlyphList.UnicodeToName(codePoint);
                width += cffFont.GetType1CharString(name).Width;
            }
            return width;
        }

        /// <summary>
        /// Maps a PostScript glyph name to the name in the underlying font, for example when
        /// using a TTF font we might map "W" to "uni0057"
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetNameInFont(string name)
        {
            if (IsEmbedded || genericFont.HasGlyph(name))
            {
                return name;
            }
            else
            {
                // try unicode name
                var unicode = GlyphList.ToUnicode(name);
                if (unicode != null)
                {
                    string uniName = UniUtil.GetUniNameOfCodePoint((int)unicode);
                    if (genericFont.HasGlyph(uniName))
                    {
                        return uniName;
                    }
                }
            }
            return ".notdef";
        }

    }
}
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

using PdfGlown.Documents.Contents.Fonts.AFM;
using System.Collections.Generic;
using System.IO;
using System;
using PdfGlown.Bytes;
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>
    /// The "Standard 14" PDF fonts, also known as the "base 14" fonts.
    /// There are 14 font files, but Acrobat uses additional names for compatibility, e.g.Arial.
    /// @author John Hewson
    /// </summary>
    public sealed class Standard14Fonts
    {
        public static readonly Dictionary<FontName, string> FontNames = new(14)
        {
            { FontName.TimesRoman, "Times-Roman" },
            { FontName.TimesBold, "Times-Bold"},
            { FontName.TimesItalic, "Times-Italic"},
            { FontName.TimesBoldItalic,"Times-BoldItalic"},
            { FontName.Helvetica,"Helvetica"},
            { FontName.HelveticaBold,"Helvetica-Bold"},
            { FontName.HelveticaOblique,"Helvetica-Oblique"},
            { FontName.HelveticaBoldOblique,"Helvetica-BoldOblique"},
            { FontName.Courier,"Courier"},
            { FontName.CourierBold,"Courier-Bold"},
            { FontName.CourierOblique,"Courier-Oblique"},
            { FontName.CourierBoldOblique,"Courier-BoldOblique"},
            { FontName.Symbol, "Symbol"},
            { FontName.ZapfDingbats,"ZapfDingbats" }
        };

        /// <summary>
        /// Contains all base names and alias names for the known fonts.
        /// For base fonts both the key and the value will be the base name.
        /// For aliases, the key is an alias, and the value is a FontName.
        /// We want a single lookup in the map to find the font both by a base name or an alias.
        /// </summary>
        private static readonly Dictionary<string, FontName> ALIASES = new(38);

        /// <summary>
        /// Contains the font metrics for the standard 14 fonts.
        /// The key is the font name, value is a FontMetrics instance.
        /// Metrics are loaded into this map on demand, only if needed.
        /// @see #getAFM
        /// </summary>
        private static readonly Dictionary<FontName, FontMetrics> FONTS = new(14);

        /// <summary>
        /// Contains the mapped fonts for the standard 14 fonts. 
        /// The key is the font name, value is a FontBoxFont instance.
        /// FontBoxFont are loaded into this map on demand, only if needed.
        /// </summary>
        private static readonly Dictionary<FontName, BaseFont> GENERIC_FONTS = new(14);

        static Standard14Fonts()
        {
            // the 14 standard fonts
            foreach (var entry in FontNames)
            {
                MapName(entry.Value, entry.Key);
            }


            // alternative names from Adobe Supplement to the ISO 32000
            MapName("CourierCourierNew", FontName.Courier);
            MapName("CourierNew", FontName.Courier);
            MapName("CourierNew,Italic", FontName.CourierOblique);
            MapName("CourierNew,Bold", FontName.CourierBold);
            MapName("CourierNew,BoldItalic", FontName.CourierBoldOblique);
            MapName("Arial", FontName.Helvetica);
            MapName("Arial,Italic", FontName.HelveticaOblique);
            MapName("Arial,Bold", FontName.HelveticaBold);
            MapName("Arial,BoldItalic", FontName.HelveticaBoldOblique);
            MapName("TimesNewRoman", FontName.TimesRoman);
            MapName("TimesNewRoman,Italic", FontName.TimesItalic);
            MapName("TimesNewRoman,Bold", FontName.TimesBold);
            MapName("TimesNewRoman,BoldItalic", FontName.TimesBoldItalic);

            // Acrobat treats these fonts as "standard 14" too (at least Acrobat preflight says so)
            MapName("Symbol,Italic", FontName.Symbol);
            MapName("Symbol,Bold", FontName.Symbol);
            MapName("Symbol,BoldItalic", FontName.Symbol);
            MapName("Times", FontName.TimesRoman);
            MapName("Times,Italic", FontName.TimesItalic);
            MapName("Times,Bold", FontName.TimesBold);
            MapName("Times,BoldItalic", FontName.TimesBoldItalic);

            // PDFBOX-3457: PDF.js file bug864847.pdf
            MapName("ArialMT", FontName.Helvetica);
            MapName("Arial-ItalicMT", FontName.HelveticaOblique);
            MapName("Arial-BoldMT", FontName.HelveticaBold);
            MapName("Arial-BoldItalicMT", FontName.HelveticaBoldOblique);
        }

        private Standard14Fonts()
        {
        }

        /// <summary>
        /// Loads the metrics for the base font specified by name.Metric file must exist in the pdfbox jar under
        /// /org/apache/pdfbox/resources/afm/
        /// </summary>
        /// <param name="fontName">one of the standard 14 font names for which to load the metrics.</param>
        /// <returns></returns>
        /// <exception cref="IOException">if no metrics exist for that font.</exception>
        private static FontMetrics LoadMetrics(FontName fontName)
        {
            string resourceName = $"fonts.afm.{FontNames[fontName]}";
            using var resourceAsStream = typeof(Standard14Fonts).Assembly.GetManifestResourceStream(resourceName);
            if (resourceAsStream == null)
            {
                throw new IOException($"resource '{resourceName}' not found");
            }

            using var afmStream = new StreamContainer(resourceAsStream);
            var parser = new AFMParser(afmStream);
            return FONTS[fontName] = parser.Parse(true);
        }

        /// <summary>
        /// Adds an alias name for a standard font to the map of known aliases to the map of aliases (alias as key, standard
        /// name as value). We want a single lookup in tbaseNamehe map to find the font both by a base name or an alias.
        /// </summary>
        /// <param name="alias">an alias for the font</param>
        /// <param name="baseName">the font name of the Standard 14 font</param>
        private static void MapName(string alias, FontName baseName)
        {
            ALIASES[alias] = baseName;
        }

        /// <summary>
        /// Returns the metrics for font specified by fontName. Loads the font metrics if not already
        /// loaded.
        /// </summary>
        /// <param name="fontName">name of font; either a base name or alias</param>
        /// <returns>the font metrics or null if the name is not one of the known names</returns>
        /// <exception cref="ArgumentException">if no metrics exist for that font.</exception>
        public static FontMetrics GetAFM(string fontName)
        {
            if (fontName == null)
                return null;
            if (!ALIASES.TryGetValue(fontName, out FontName baseName))
            {
                return null;
            }

            if (!FONTS.TryGetValue(baseName, out var metrix))
            {
                lock (FONTS)
                {
                    if (!FONTS.TryGetValue(baseName, out metrix))
                    {
                        try
                        {
                            FONTS[baseName] = metrix = LoadMetrics(baseName);
                        }
                        catch (IOException e)
                        {
                            throw new ArgumentException(fontName, e);
                        }
                    }
                }
            }

            return metrix;
        }

        /// <summary>
        /// Returns true if the given font name is one of the known names, including alias.
        /// </summary>
        /// <param name="fontName">the name of font, either a base name or alias</param>
        /// <returns>true if the name is one of the known names</returns>
        public static bool ContainsName(string fontName)
        {
            return ALIASES.ContainsKey(fontName);
        }

        /// <summary>Returns the set of known font names, including aliases.</summary>
        /// <returns>the set of known font names</returns>
        public static IReadOnlyCollection<string> GetNames()
        {
            return ALIASES.Keys;
        }

        /// <summary>
        /// Returns the base name of the font which the given font name maps to.
        /// </summary>
        /// <param name="fontName">name of font, either a base name or an alias</param>
        /// <returns>the base name or null if this is not one of the known names</returns>
        public static FontName GetMappedFontName(string fontName)
        {
            return ALIASES.TryGetValue(fontName, out var font) ? font : (FontName)(-1);
        }

        public static string GetMappedFontString(string fontName)
        {
            return ALIASES.TryGetValue(fontName, out var font) ? FontNames[font] : null;
        }

        /// <summary>
        /// Returns the mapped font for the specified Standard 14 font. The mapped font is cached.
        /// </summary>
        /// <param name="baseName">name of the standard 14 font</param>
        /// <returns>the mapped font</returns>
        private static BaseFont GetMappedFont(FontName baseName)
        {
            if (!GENERIC_FONTS.TryGetValue(baseName, out var box))
            {
                lock (GENERIC_FONTS)
                {
                    if (!GENERIC_FONTS.TryGetValue(baseName, out box))
                    {
                        var type1Font = new PdfType1Font(null, baseName);
                        GENERIC_FONTS[baseName] = box = type1Font.Font;
                    }
                }
            }
            return box;
        }

        /// <summary>Returns the path for the character with the given name for 
        /// the specified Standard 14 font. The mapped font is cached. The path 
        /// may differ in different environments as it depends on the mapped font.</summary>
        /// <param name="baseName">name of the standard 14 font</param>
        /// <param name="glyphName">name of glyph</param>
        /// <returns>the mapped font</returns>
        public static SKPath GetGlyphPath(FontName baseName, string glyphName)
        {
            // copied and adapted from PDType1Font.getNameInFont(string)
            if (!string.Equals(glyphName, ".notdef", StringComparison.Ordinal))
            {
                var mappedFont = GetMappedFont(baseName);
                if (mappedFont != null)
                {
                    if (mappedFont.HasGlyph(glyphName))
                    {
                        return mappedFont.GetPath(glyphName);
                    }
                    var fonName = FontNames[baseName];
                    var unicodes = GetGlyphList(fonName).ToUnicode(glyphName);
                    if (unicodes != null && unicodes > 255)
                    {
                        string uniName = unicodes.Value.GetUniNameOfCodePoint();
                        if (mappedFont.HasGlyph(uniName))
                        {
                            return mappedFont.GetPath(uniName);
                        }
                    }
                    if (string.Equals("SymbolMT", mappedFont.Name))
                    {
                        if (SymbolEncoding.Instance.NameToCodeMap.TryGetValue(glyphName, out var code))
                        {
                            string uniName = code.GetUniNameOfCodePoint();//code + 0xF000
                            if (mappedFont.HasGlyph(uniName))
                            {
                                return mappedFont.GetPath(uniName);
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static GlyphMapping GetGlyphList(string baseName)
        {
            return FontNames[FontName.ZapfDingbats] == baseName
                ? GlyphMapping.ZapfDingbats
                : GlyphMapping.Default;
        }
    }
}

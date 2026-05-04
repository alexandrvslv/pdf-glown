/*
  Copyright 2009-2011 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
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

using PdfClown.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfClown.Documents.Contents.Fonts
{
    /// <summary>Adobe standard glyph mapping (unicode-encoding against glyph-naming)
    /// [PDF:1.6:D;AGL:2.0].</summary>
    public partial class GlyphMapping
    {
        [GeneratedRegex(@"^(\w+);([A-F0-9 ]+)$", RegexOptions.CultureInvariant)]
        private static partial Regex GetLinePattern();

        public static readonly GlyphMapping Default = new GlyphMapping("AGL20", 4300);
        public static readonly GlyphMapping ZapfDingbats = new GlyphMapping("ZapfDingbats", 210);
        public static readonly GlyphMapping DLFONT = new GlyphMapping("G500", 100);
        //public static readonly GlyphMapping Additional = new GlyphMapping("Additional", 128);
        public static bool IsExist(string fontName) => typeof(GlyphMapping).Assembly.GetManifestResourceNames().Contains($"fonts.{fontName}");

        private readonly Dictionary<string, int> nameToCode;
        private readonly Dictionary<int, string> codeToName;
        private readonly Dictionary<string, int> uniNameToUnicodeCache = new Dictionary<string, int>();

        private GlyphMapping(int capacity)
        {
            nameToCode = new Dictionary<string, int>(capacity, StringComparer.Ordinal);
            codeToName = new Dictionary<int, string>(capacity);
        }

        public GlyphMapping(string fontName, int capacity = 100)
            : this(capacity)
        {
            Load($"fonts.{fontName}");
        }

        public GlyphMapping(Stream stream, int capacity = 100)
            : this(capacity)
        {
            Parse(stream);
        }

        public int? ToUnicode(string name)
        {
            if (name == null)
            {
                return null;
            }
            if (nameToCode.TryGetValue(name, out var existing))
            {
                return existing;
            }
            int? unicode = null;
            // separate read/write cache for thread safety
            if (uniNameToUnicodeCache.TryGetValue(name, out existing))
                unicode = existing;
            else
            {
                // test if we have a suffix and if so remove it
                var dotIndex = name.IndexOf('.');
                int nameLength = name.Length;
                if (dotIndex > 0)
                {
                    unicode = ToUnicode(name.Substring(0, dotIndex)) ?? -1;
                }
                else if (name.StartsWith("uni", StringComparison.Ordinal) && name.Length == 7)
                {
                    // test for Unicode name in the format uniXXXX where X is hex
                    try
                    {
                        int index = 0;
                        unicode = 0;
                        for (int chPos = 3; chPos + 4 <= nameLength; chPos += 4, index++)
                        {
                            int codePoint = Convert.ToInt32(name.Substring(chPos, 4), 16);
                            if (codePoint > 0xD7FF && codePoint < 0xE000)
                            {
                                Debug.WriteLine($"warn: Unicode character name with disallowed code area: {name}");
                            }
                            else
                            {
                                unicode <<= 8;
                                unicode |= (codePoint & 0xff);
                                break;
                            }
                        }
                    }
                    catch (Exception nfe)
                    {
                        Debug.WriteLine($"warn: Not a number in Unicode character name: {name} {nfe}");
                    }
                }
                else if (name.StartsWith("u", StringComparison.Ordinal) && name.Length == 5)
                {
                    // test for an alternate Unicode name representation uXXXX
                    try
                    {
                        int codePoint = Convert.ToInt32(name.Substring(1), 16);
                        if (codePoint > 0xD7FF && codePoint < 0xE000)
                        {
                            Debug.WriteLine($"Unicode character name with disallowed code area: {name}");
                        }
                        else
                        {
                            unicode = codePoint;
                        }
                    }
                    catch (Exception nfe)
                    {
                        Debug.WriteLine($"warn: Not a number in Unicode character name: {name} {nfe}");
                    }
                }
                if (unicode != null)
                {
                    // null value not allowed in ConcurrentHashMap
                    uniNameToUnicodeCache[name] = (int)unicode;
                }
            }
            return unicode;
        }

        public string UnicodeToName(int unicode)
        {
            if (codeToName.TryGetValue(unicode, out var name))
            {
                return name;
            }
            return ".notdef";
        }

        /// <summary>Loads the glyph list mapping character names to character codes (unicode
        /// encoding).</summary>
        private void Load(string fontName)
        {
            // Open the glyph list!
            // NOTE: The Adobe Glyph List [AGL:2.0] represents the reference name-to-unicode map
            // for consumer applications.
            using var resourceAsStream = typeof(GlyphMapping).Assembly.GetManifestResourceStream(fontName);
            if (resourceAsStream == null)
            {
                throw new IOException($"GlyphList '{fontName}' not found");
            }
            Parse(resourceAsStream);
        }

        private void Parse(Stream resourceAsStream)
        {
            using var glyphListStream = new StreamReader(resourceAsStream, Charset.ISO88591);
            Parse(glyphListStream);
        }

        private void Parse(StreamReader glyphListStream)
        {
            // Parsing the glyph list...
            string line;
            var linePattern = GetLinePattern();
            while ((line = glyphListStream.ReadLine()) != null)
            {
                MatchCollection lineMatches = linePattern.Matches(line);
                if (lineMatches.Count < 1)
                    continue;

                Match lineMatch = lineMatches[0];

                var name = lineMatch.Groups[1].Value;
                var codes = lineMatch.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                int code = 0;
                for (int i = 0; i < codes.Length; i++)
                {
                    var parsed = ushort.Parse(codes[i], NumberStyles.HexNumber);
                    code <<= 16;
                    code |= parsed & 0xffff;
                }

                // Associate the character name with its corresponding character code!
                nameToCode[name] = code;
                // reverse mapping
                // PDFBOX-3884: take the various standard encodings as canonical, 
                // e.g. tilde over ilde
                bool forceOverride =
                      WinAnsiEncoding.Instance.Contains(name) ||
                      MacRomanEncoding.Instance.Contains(name) ||
                      MacExpertEncoding.Instance.Contains(name) ||
                      SymbolEncoding.Instance.Contains(name) ||
                      ZapfDingbatsEncoding.Instance.Contains(name);
                if (!codeToName.ContainsKey(code) || forceOverride)
                {
                    codeToName[code] = name;
                }
            }
        }
    }
}
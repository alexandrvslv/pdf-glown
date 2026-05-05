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
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>
    /// Embedded PDTrueTypeFont builder. Helper class to populate a PDTrueTypeFont from a TTF.
    /// @author John Hewson
    /// @author Ben Litchfield
    /// </summary>
    internal sealed class FontTrueTypeEmbedder : TrueTypeEmbedder
    {
        private readonly Encoding fontEncoding;

        /// <summary>Creates a new TrueType font embedder for the given TTF as a PDTrueTypeFont.</summary>
        /// <param name="document">The parent document</param>
        /// <param name="dict">Font dictionary</param>
        /// <param name="ttf">TTF stream</param>
        /// <param name="encoding">The PostScript encoding vector to be used for embedding.</param>
        public FontTrueTypeEmbedder(PdfDocument document, PdfFont dict, TrueTypeFont ttf, Encoding encoding)
            : base(document, dict, ttf, false)
        {
            dict[PdfName.Subtype] = PdfName.TrueType;

            var glyphList = GlyphMapping.Default;
            this.fontEncoding = encoding;
            dict[PdfName.Encoding] = encoding.GetPdfObject();
            fontDescriptor.Flags &= ~FlagsEnum.Symbolic;
            fontDescriptor.Flags |= FlagsEnum.Nonsymbolic;

            // add the font descriptor
            dict[PdfName.FontDescriptor] = fontDescriptor.RefOrSelf;

            // set the glyph widths
            SetWidths(dict, glyphList);
        }

        /// <summary>Sets the glyph widths in the font dictionary.</summary>
        /// <param name="font"></param>
        /// <param name="glyphList"></param>
        private void SetWidths(PdfDictionary font, GlyphMapping glyphList)
        {
            float scaling = 1000f / ttf.Header.UnitsPerEm;
            HorizontalMetricsTable hmtx = ttf.HorizontalMetrics;

            Dictionary<int, string> codeToName = FontEncoding.CodeToNameMap;

            int firstChar = codeToName.Keys.Min();
            int lastChar = codeToName.Keys.Max();

            var widths = new List<int>(lastChar - firstChar + 1);
            for (int i = 0; i < lastChar - firstChar + 1; i++)
            {
                widths.Add(0);
            }

            // a character code is mapped to a glyph name via the provided font encoding
            // afterwards, the glyph name is translated to a glyph ID.
            foreach (KeyValuePair<int, string> entry in codeToName)
            {
                int code = entry.Key;
                string name = entry.Value;

                if (code >= firstChar && code <= lastChar)
                {
                    var charCode = glyphList.ToUnicode(name) ?? 0;
                    int gid = cmapLookup.GetGlyphId(charCode);
                    widths[entry.Key - firstChar] = (int)Math.Round(hmtx.GetAdvanceWidth(gid) * scaling);
                }
            }

            font.Set(PdfName.FirstChar, firstChar);
            font.Set(PdfName.LastChar, lastChar);
            font[PdfName.Widths] = new PdfArrayImpl(widths);
        }

        /// <summary>Returns the font's encoding.</summary>
        public Encoding FontEncoding
        {
            get => fontEncoding;
        }

        protected override void BuildSubset(IOutputStream ttfSubset, string tag, Dictionary<int, int> gidToCid)
        {
            // use PDType0Font instead
            throw new NotSupportedException();
        }
    }
}
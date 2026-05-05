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
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{

    /// <summary>An OpenType(OTF/TTF) font.</summary>
    public class OpenTypeFont : TrueTypeFont
    {
        //https://stackoverflow.com/a/26394949/4682355
        public static unsafe uint FloatToUInt32Bits(float f)
        {
            return *((uint*)&f);
        }

        private bool? isPostScript;
        private bool isPostScriptBit;

        /**
         * Constructor. Clients should use the OTFParser to create a new OpenTypeFont object.
         *
         * @param fontData The font data.
         */
        public OpenTypeFont(IInputStream fontData) : base(fontData)
        {

        }

        public override float Version
        {
            get => base.Version;
            set
            {
                isPostScriptBit = FloatToUInt32Bits(value) == 0x469EA8A9; // OTTO
                base.Version = value;
            }
        }

        /// <summary>Get the "CFF" table for this OTF.</summary>
        public CFFTable CFF => (CFFTable)GetTable(CFFTable.TAG);

        public override GlyphTable Glyph => base.Glyph;

        public override SKPath GetPath(string name)
        {
            if (IsPostScript && IsSupportedOTF)
            {
                int gid = NameToGID(name);
                return CFF.Font.GetType2CharString(gid).Path;
            }
            return base.GetPath(name);
        }

        /// <summary>Returns true if this font is a PostScript outline font.</summary>
        public bool IsPostScript
        {
            get => isPostScript ??= (isPostScriptBit || tables.ContainsKey(CFFTable.TAG) || tables.ContainsKey("CFF2"));
        }

        /**
        * Returns true if this font is supported.
        * 
        * There are 3 kind of OpenType fonts, fonts using TrueType outlines, fonts using CFF outlines (version 1 and 2)
        * 
      * Fonts using CFF outlines version 2 aren't supported yet.
      * 
      * @return true if the font is supported
      */
        public bool IsSupportedOTF
        {
            // OTF using CFF2 based outlines aren't yet supported
            get => !(IsPostScript
                    && !tables.ContainsKey(CFFTable.TAG)
                    && tables.ContainsKey("CFF2"));
        }

        /// <summary>Returns true if this font uses OpenType Layout (Advanced Typographic) tables.</summary>
        public bool HasLayoutTables()
        {
            return tables.ContainsKey("BASE") ||
                   tables.ContainsKey("GDEF") ||
                   tables.ContainsKey("GPOS") ||
                   tables.ContainsKey(GlyphSubstitutionTable.TAG) ||
                   tables.ContainsKey(OTLTable.TAG);
        }
    }
}

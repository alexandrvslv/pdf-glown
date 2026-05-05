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

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>OpenType font file parser.</summary>
    public sealed class OTFParser : TTFParser
    {
        public OTFParser() : base()
        { }

        /// <summary>Constructor.</summary>
        /// <param name="isEmbedded">true if the font is embedded in PDF</param>
        public OTFParser(bool isEmbedded) : base(isEmbedded)
        { }

        public override TrueTypeFont NewFont(IInputStream raf)
        {
            return new OpenTypeFont(raf);
        }

        protected override TTFTable ReadTable(string tag)
        {
            // todo: this is a stub, a full implementation is needed
            switch (tag)
            {
                case "BASE":
                case "GDEF":
                case "GPOS":
                case GlyphSubstitutionTable.TAG:
                case OTLTable.TAG:
                    return new OTLTable();
                case CFFTable.TAG:
                    return new CFFTable();
                default:
                    return base.ReadTable(tag);
            }
        }


        protected override bool AllowCFF
        {
            get => true;
        }
    }
}

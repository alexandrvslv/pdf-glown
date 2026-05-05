/*
 * https://github.com/apache/pdfbox
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
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>
    /// An Adobe Compact Font Format(CFF) font.Thread safe.
    /// @author Villu Ruusmann
    /// @author John Hewson
    /// </summary>
    public abstract class CFFFont : BaseFont
    {
        private string fontName;
        private CFFCharset charset;
        private CFFParser.IByteSource source;
        private SKRect? fontBBox;
        protected readonly Dictionary<string, object> topDict = new(StringComparer.Ordinal);
        protected Memory<byte>[] charStrings;
        protected Memory<byte>[] globalSubrIndex;

        /// <summary>The name of the font.</summary>
        public override string Name
        {
            get => fontName;
        }

        public string FontName
        {
            get => fontName;
            set => fontName = value;
        }

        /// <summary>The top dictionary.</summary>
        public Dictionary<string, object> TopDict
        {
            get => topDict;
        }

        /// <summary>Returns the FontBBox.</summary>
        public override SKRect FontBBox
        {
            get => fontBBox ??= GetBBox();
        }

        private SKRect GetBBox()
        {
            var numbers = (List<float>)topDict["FontBBox"];
            return new SKRect(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        public override List<float> FontMatrix
        {
            // our parser guarantees that FontMatrix will be present and correct in the Top DICT
            get => topDict.TryGetValue("FontMatrix", out var array) ? (List<float>)array : null;
        }

        /// <summary>Returns the CFFCharset of the font.</summary>
        public virtual CFFCharset Charset
        {
            get => charset;
            set => charset = value;
        }

        /// <summary>Returns the character strings dictionary. For expert users only.</summary>
        public Memory<byte>[] CharStringBytes
        {
            get => charStrings;
            set => charStrings = value;
        }

        /// <summary>Byte source to re-read the CFF data in the future.</summary>
        public CFFParser.IByteSource Data
        {
            get => source;
            set => source = value;
        }

        /// <summary>Returns the number of charstrings in the font.</summary>
        public int NumCharStrings
        {
            get => charStrings.Length;
        }


        /// <summary>Returns the list containing the global subroutine.</summary>
        public Memory<byte>[] GlobalSubrIndex
        {
            get => globalSubrIndex;
            set => globalSubrIndex = value;
        }

        /// <summary>
        /// Adds the given key/value pair to the top dictionary.
        /// </summary>
        /// <param name="name">the given key</param>
        /// <param name="value">the given value</param>
        public void AddValueToTopDict(string name, object value)
        {
            if (value != null)
            {
                topDict[name] = value;
            }
        }

        /// <summary>Returns the Type 2 charstring for the given CID.</summary>
        /// <param name="cidOrGid">CID for CIFFont, or GID for Type 1 font</param>
        /// <returns></returns>
        public abstract Type2CharString GetType2CharString(int cidOrGid);


        public override string ToString()
        {
            return GetType().Name + "[name=" + fontName + ", topDict=" + topDict
                    + ", charset=" + charset + ", charStrings=" + string.Join(", ", charStrings.Select(p => p.Length))
                    + "]";
        }
    }
}

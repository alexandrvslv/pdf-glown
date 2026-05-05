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

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Fonts.Type1
{
    /// <summary>
    /// Represents an Adobe Type 1 (.pfb) font.Thread safe.
    /// @author John Hewson
    /// </summary>
    public sealed class Type1Font : BaseFont, IType1CharStringReader, IEncodedFont
    {
        /// <summary>Constructs a new Type1Font object from a .pfb stream.</summary>
        /// <param name="pfbStream">.pfb input stream, including headers</param>
        /// <returns>a type1 font</returns>
        public static Type1Font CreateWithPFB(Bytes.ByteStream pfbStream)
        {
            PfbParser pfb = new PfbParser(pfbStream);
            Type1Parser parser = new Type1Parser();
            return parser.Parse(pfb.GetSegment1(), pfb.GetSegment2());
        }

        /// <summary>Constructs a new Type1Font object from a.pfb stream.</summary>
        /// <param name="pfbBytes">.pfb data, including headers</param>
        /// <returns>a type1 font</returns>
        public static Type1Font CreateWithPFB(Memory<byte> pfbBytes) => CreateWithPFB(pfbBytes, new PfbParser(pfbBytes));

        public static Type1Font CreateWithPFB(Memory<byte> pfbBytes, PfbParser pfb)
        {
            Type1Parser parser = new Type1Parser();
            return parser.Parse(pfb.GetSegment1(), pfb.GetSegment2());
        }

        /// <summary>Constructs a new Type1Font object from two header-less.pfb segments.</summary>
        /// <param name="segment1">The first segment, without header</param>
        /// <param name="segment2">The second segment, without header</param>
        /// <returns>A new Type1Font instance</returns>
        public static Type1Font CreateWithSegments(Memory<byte> segment1, Memory<byte> segment2)
        {
            var parser = new Type1Parser();
            return parser.Parse(segment1, segment2);
        }

        // font dictionary
        string fontName = "";
        Encoding encoding = null;
        int paintType;
        int fontType;
        List<float> fontMatrix = new();
        List<float> fontBBox = new();
        private SKRect? rectBBox;
        int uniqueID;
        float strokeWidth;
        string fontID = "";

        // FontInfo dictionary
        string version = "";
        string notice = "";
        string fullName = "";
        string familyName = "";
        string weight = "";
        float italicAngle;
        bool isFixedPitch;
        float underlinePosition;
        float underlineThickness;

        // Private dictionary
        List<float> blueValues = new();
        List<float> otherBlues = new();
        List<float> familyBlues = new();
        List<float> familyOtherBlues = new();
        float blueScale;
        int blueShift, blueFuzz;
        List<float> stdHW = new();
        List<float> stdVW = new();
        List<float> stemSnapH = new();
        List<float> stemSnapV = new();
        bool forceBold;
        int languageGroup;

        // Subrs array, and CharStrings dictionary
        readonly List<Memory<byte>> subrs = new();
        readonly Dictionary<string, Memory<byte>> charstrings = new(StringComparer.Ordinal);

        // private caches
        private readonly Dictionary<string, Type1CharString> charStringCache = new(StringComparer.Ordinal);

        private Type1CharStringParser charStringParser = null;

        // raw data
        private readonly Memory<byte> segment1, segment2;

        /// <summary>Constructs a new Type1Font, called by Type1Parser.</summary>
        public Type1Font(Memory<byte> segment1, Memory<byte> segment2)
        {
            this.segment1 = segment1;
            this.segment2 = segment2;
        }

        /// <summary>
        /// Returns the /Subrs array as raw bytes.
        /// Type 1 char string bytes
        /// </summary>
        public List<Memory<byte>> SubrsArray
        {
            get => subrs;
        }

        /// <summary>The /CharStrings dictionary as raw bytes.
        /// Type 1 char string bytes</summary>
        public Dictionary<string, Memory<byte>> CharStringsDict
        {
            get => charstrings;
        }


        public override string Name => fontName;

        public string FontName
        {
            get => fontName;
            set => fontName = value;
        }

        public Encoding Encoding
        {
            get => encoding;
            set => encoding = value;
        }

        public int PaintType
        {
            get => paintType;
            set => paintType = value;
        }

        public int FontType
        {
            get => fontType;
            set => fontType = value;
        }

        public override List<float> FontMatrix
        {
            get => fontMatrix;
        }

        public List<float> FontMatrixData
        {
            get => fontMatrix;
            set => fontMatrix = value;
        }

        public override SKRect FontBBox
        {
            get => rectBBox ?? (rectBBox = new SKRect(fontBBox[0], fontBBox[1], fontBBox[2], fontBBox[3])).Value;

        }

        public List<float> FontBBoxData
        {
            get => fontBBox;
            set => fontBBox = value;
        }

        public int UniqueID
        {
            get => uniqueID;
            set => uniqueID = value;
        }

        public float StrokeWidth
        {
            get => strokeWidth;
            set => strokeWidth = value;
        }

        public string FontID
        {
            get => fontID;
            set => fontID = value;
        }

        // FontInfo dictionary
        public string Version
        {
            get => version;
            set => version = value;
        }

        public string Notice
        {
            get => notice;
            set => notice = value;
        }

        public string FullName
        {
            get => fullName;
            set => fullName = value;
        }

        public string FamilyName
        {
            get => familyName;
            set => familyName = value;
        }

        public string Weight
        {
            get => weight;
            set => weight = value;
        }

        public float ItalicAngle
        {
            get => italicAngle;
            set => italicAngle = value;
        }

        public bool FixedPitch
        {
            get => isFixedPitch;
            set => isFixedPitch = value;
        }

        public float UnderlinePosition
        {
            get => underlinePosition;
            set => underlinePosition = value;
        }

        public float UnderlineThickness
        {
            get => underlineThickness;
            set => underlineThickness = value;
        }

        // Private dictionary
        public List<float> BlueValues
        {
            get => blueValues;
            set => blueValues = value;
        }

        public List<float> OtherBlues
        {
            get => otherBlues;
            set => otherBlues = value;
        }

        public List<float> FamilyBlues
        {
            get => familyBlues;
            set => familyBlues = value;
        }

        public List<float> FamilyOtherBlues
        {
            get => familyOtherBlues;
            set => familyOtherBlues = value;
        }

        public float BlueScale
        {
            get => blueScale;
            set => blueScale = value;
        }

        public int BlueShift
        {
            get => blueShift;
            set => blueShift = value;
        }

        public int BlueFuzz
        {
            get => blueFuzz;
            set => blueFuzz = value;
        }

        public List<float> StdHW
        {
            get => stdHW;
            set => stdHW = value;
        }

        public List<float> StdVW
        {
            get => stdVW;
            set => stdVW = value;
        }

        public List<float> StemSnapH
        {
            get => stemSnapH;
            set => stemSnapH = value;
        }

        public List<float> StemSnapV
        {
            get => stemSnapV;
            set => stemSnapV = value;
        }

        public bool IsForceBold
        {
            get => forceBold;
            set => forceBold = value;
        }

        public int LanguageGroup
        {
            get => languageGroup;
            set => languageGroup = value;
        }

        public Memory<byte> ASCIISegment
        {
            get => segment1;
        }

        public Memory<byte> BinarySegment
        {
            get => segment2;
        }

        public override SKPath GetPath(string name)
        {
            return GetType1CharString(name).Path;
        }

        public override float GetWidth(string name)
        {
            return GetType1CharString(name).Width;
        }

        public override bool HasGlyph(string name)
        {
            return charstrings.TryGetValue(name, out _);
        }

        //public Type1CharString GetType1CharString(ByteArray key)
        //{
        //}

        public Type1CharString GetType1CharString(string name)
        {
            if (!charStringCache.TryGetValue(name, out Type1CharString type1))
            {
                if (!charstrings.TryGetValue(name, out var bytes))
                {
                    bytes = charstrings[".notdef"];
                }
                List<Object> sequence = Parser.Parse(bytes, subrs, name);
                type1 = new Type1CharString(this, fontName, name, sequence);
                charStringCache.Add(name, type1);
            }
            return type1;
        }

        private Type1CharStringParser Parser
        {
            get => charStringParser ??= new Type1CharStringParser(fontName);
        }

        public override string ToString()
        {
            return $"{GetType().Name}[fontName={fontName}, fullName={fullName}, encoding={encoding}, charStringsDict={charstrings}]";
        }
    }
}
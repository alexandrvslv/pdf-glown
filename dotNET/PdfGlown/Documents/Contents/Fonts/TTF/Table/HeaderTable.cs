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
using System;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A table in a true type font.
    /// @author Ben Litchfield
    /// </summary>
    public class HeaderTable : TTFTable
    {
        /// <summary>Tag to identify this table.</summary>
        public const string TAG = "head";

        /// <summary>Bold macStyle flag.</summary>
        public static readonly int MAC_STYLE_BOLD = 1;

        /// <summary>Italic macStyle flag.</summary>
        public static readonly int MAC_STYLE_ITALIC = 2;

        private float version;
        private float fontRevision;
        private uint checkSumAdjustment;
        private uint magicNumber;
        private ushort flags;
        private ushort unitsPerEm;
        private DateTime created;
        private DateTime modified;
        private short xMin;
        private short yMin;
        private short xMax;
        private short yMax;
        private ushort macStyle;
        private ushort lowestRecPPEM;
        private short fontDirectionHint;
        private short indexToLocFormat;
        private short glyphDataFormat;

        public HeaderTable()
        { }

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="ttf">The font that is being read.</param>
        /// <param name="data">The stream to read the data from.</param>
        public override void Read(TrueTypeFont ttf, IInputStream data)
        {
            version = data.Read32Fixed();
            fontRevision = data.Read32Fixed();
            checkSumAdjustment = data.ReadUInt32();
            magicNumber = data.ReadUInt32();
            flags = data.ReadUInt16();
            unitsPerEm = data.ReadUInt16();
            created = data.ReadInternationalDate();
            modified = data.ReadInternationalDate();
            xMin = data.ReadInt16();
            yMin = data.ReadInt16();
            xMax = data.ReadInt16();
            yMax = data.ReadInt16();
            macStyle = data.ReadUInt16();
            lowestRecPPEM = data.ReadUInt16();
            fontDirectionHint = data.ReadInt16();
            indexToLocFormat = data.ReadInt16();
            glyphDataFormat = data.ReadInt16();
            initialized = true;
        }

        /// <summary>Returns the checkSumAdjustment.</summary>
        public uint CheckSumAdjustment
        {
            get => checkSumAdjustment;
            set => checkSumAdjustment = value;
        }

        public DateTime Created
        {
            get => created;
            set => created = value;
        }

        public ushort Flags
        {
            get => flags;
            set => flags = value;
        }

        public short FontDirectionHint
        {
            get => fontDirectionHint;
            set => fontDirectionHint = value;
        }

        public float FontRevision
        {
            get => fontRevision;
            set => fontRevision = value;
        }

        public short GlyphDataFormat
        {
            get => glyphDataFormat;
            set => glyphDataFormat = value;
        }

        public short IndexToLocFormat
        {
            get => indexToLocFormat;
            set => indexToLocFormat = value;
        }

        public ushort LowestRecPPEM
        {
            get => lowestRecPPEM;
            set => lowestRecPPEM = value;
        }

        public ushort MacStyle
        {
            get => macStyle;
            set => macStyle = value;
        }

        public uint MagicNumber
        {
            get => magicNumber;
            set => magicNumber = value;
        }

        public DateTime Modified
        {
            get => modified;
            set => modified = value;
        }

        public ushort UnitsPerEm
        {
            get => unitsPerEm;
            set => unitsPerEm = value;
        }

        public float Version
        {
            get => version;
            set => version = value;
        }

        public short XMax
        {
            get => xMax;
            set => xMax = value;
        }

        public short XMin
        {
            get => xMin;
            set => xMin = value;
        }

        public short YMax
        {
            get => yMax;
            set => yMax = value;
        }

        public short YMin
        {
            get => yMin;
            set => yMin = value;
        }

    }
}
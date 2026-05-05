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
    /// <summary>
    /// A vertical header 'vhea' table in a TrueType or OpenType font.
    /// Supports versions 1.0 and 1.1, for which the only difference is changing
    /// the specification names and descriptions of the ascender, descender,
    /// and lineGap fields to vertTypoAscender, vertTypoDescender, vertTypeLineGap.
    /// This table is required by the OpenType CJK Font Guidelines for "all
    /// OpenType fonts that are used for vertical writing".
    /// This table is specified in both the TrueType and OpenType specifications.
    /// @author Glenn Adams
    /// </summary>
    public class VerticalHeaderTable : TTFTable
    {
        /// <summary>A tag that identifies this table type.</summary>
        public const string TAG = "vhea";

        private float version;
        private short ascender;
        private short descender;
        private short lineGap;
        private int advanceHeightMax;
        private short minTopSideBearing;
        private short minBottomSideBearing;
        private short yMaxExtent;
        private short caretSlopeRise;
        private short caretSlopeRun;
        private short caretOffset;
        private short reserved1;
        private short reserved2;
        private short reserved3;
        private short reserved4;
        private short metricDataFormat;
        private int numberOfVMetrics;

        public VerticalHeaderTable()
        { }

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="ttf">The font that is being read.</param>
        /// <param name="data">The stream to read the data from.</param>
        public override void Read(TrueTypeFont ttf, IInputStream data)
        {
            version = data.Read32Fixed();
            ascender = data.ReadInt16();
            descender = data.ReadInt16();
            lineGap = data.ReadInt16();
            advanceHeightMax = data.ReadUInt16();
            minTopSideBearing = data.ReadInt16();
            minBottomSideBearing = data.ReadInt16();
            yMaxExtent = data.ReadInt16();
            caretSlopeRise = data.ReadInt16();
            caretSlopeRun = data.ReadInt16();
            caretOffset = data.ReadInt16();
            reserved1 = data.ReadInt16();
            reserved2 = data.ReadInt16();
            reserved3 = data.ReadInt16();
            reserved4 = data.ReadInt16();
            metricDataFormat = data.ReadInt16();
            numberOfVMetrics = data.ReadUInt16();
            initialized = true;
        }

        public int AdvanceHeightMax
        {
            get => advanceHeightMax;
            set => advanceHeightMax = value;
        }

        public short Ascender
        {
            get => ascender;
        }

        public short CaretSlopeRise
        {
            get => caretSlopeRise;
        }

        public short CaretSlopeRun
        {
            get => caretSlopeRun;
        }

        public short CaretOffset
        {
            get => caretOffset;
        }

        public short Descender
        {
            get => descender;
        }

        public short LineGap
        {
            get => lineGap;
        }

        public short MetricDataFormat
        {
            get => metricDataFormat;
        }

        public short MinTopSideBearing
        {
            get => minTopSideBearing;
        }

        public short MinBottomSideBearing
        {
            get => minBottomSideBearing;
        }

        public int NumberOfVMetrics
        {
            get => numberOfVMetrics;
        }

        public short Reserved1
        {
            get => reserved1;
        }

        public short Reserved2
        {
            get => reserved2;
        }

        public short Reserved3
        {
            get => reserved3;
        }

        public short Reserved4
        {
            get => reserved4;
        }

        public float Version
        {
            get => version;
        }
        
        public short YMaxExtent
        {
            get => yMaxExtent;
        }
    }
}
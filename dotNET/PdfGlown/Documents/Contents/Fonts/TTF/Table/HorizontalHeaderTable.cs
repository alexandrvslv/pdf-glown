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
    /// A table in a true type font.
    /// @author Ben Litchfield
    /// </summary>
    public class HorizontalHeaderTable : TTFTable
    {
        /// <summary>A tag that identifies this table type.</summary>
        public const string TAG = "hhea";

        private float version;
        private short ascender;
        private short descender;
        private short lineGap;
        private ushort advanceWidthMax;
        private short minLeftSideBearing;
        private short minRightSideBearing;
        private short xMaxExtent;
        private short caretSlopeRise;
        private short caretSlopeRun;
        private short reserved1;
        private short reserved2;
        private short reserved3;
        private short reserved4;
        private short reserved5;
        private short metricDataFormat;
        private int numberOfHMetrics;

        public HorizontalHeaderTable()
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
            advanceWidthMax = data.ReadUInt16();
            minLeftSideBearing = data.ReadInt16();
            minRightSideBearing = data.ReadInt16();
            xMaxExtent = data.ReadInt16();
            caretSlopeRise = data.ReadInt16();
            caretSlopeRun = data.ReadInt16();
            reserved1 = data.ReadInt16();
            reserved2 = data.ReadInt16();
            reserved3 = data.ReadInt16();
            reserved4 = data.ReadInt16();
            reserved5 = data.ReadInt16();
            metricDataFormat = data.ReadInt16();
            numberOfHMetrics = data.ReadUInt16();
            initialized = true;
        }

        public ushort AdvanceWidthMax
        {
            get => advanceWidthMax;
            set => advanceWidthMax = value;
        }

        public short Ascender
        {
            get => ascender;
            set => ascender = value;
        }

        public short CaretSlopeRise
        {
            get => caretSlopeRise;
            set => caretSlopeRise = value;
        }

        public short CaretSlopeRun
        {
            get => caretSlopeRun;
            set => caretSlopeRun = value;
        }

        public short Descender
        {
            get => descender;
            set => descender = value;
        }

        public short LineGap
        {
            get => lineGap;
            set => lineGap = value;
        }

        public short MetricDataFormat
        {
            get => metricDataFormat;
            set => metricDataFormat = value;
        }

        public short MinLeftSideBearing
        {
            get => minLeftSideBearing;
            set => minLeftSideBearing = value;
        }

        public short MinRightSideBearing
        {
            get => minRightSideBearing;
            set => minRightSideBearing = value;
        }

        public int NumberOfHMetrics
        {
            get => numberOfHMetrics;
            set => numberOfHMetrics = value;
        }

        public short Reserved1
        {
            get => reserved1;
            set => reserved1 = value;
        }

        public short Reserved2
        {
            get => reserved2;
            set => reserved2 = value;
        }

        public short Reserved3
        {
            get => reserved3;
            set => reserved3 = value;
        }

        public short Reserved4
        {
            get => reserved4;
            set => reserved4 = value;
        }

        public short Reserved5
        {
            get => reserved5;
            set => reserved5 = value;
        }

        public float Version
        {
            get => version;
            set => version = value;
        }

        public short XMaxExtent
        {
            get => xMaxExtent;
            set => xMaxExtent = value;
        }

    }
}